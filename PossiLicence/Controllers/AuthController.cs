using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PossiLicence.Context;
using PossiLicence.Dtos;
using System.Security.Claims;

namespace PossiLicence.Controllers
{
    [Authorize]
    public class AuthController(DBContext _dbContext) : Controller
    {
        public IActionResult Index()
        {
            return View(); // Dashboard view
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index");

            return View();
        }


        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (request.NewPassword != request.ConfirmPassword)
                    return BadRequest(new { message = "Yeni şifreler eşleşmiyor." });

                var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out Guid adminId))
                {
                    return Unauthorized(new { message = "Geçersiz kullanıcı." });
                }

                var admin = await _dbContext.Admins.FindAsync(adminId);
                if (admin == null)
                    return NotFound(new { message = "Kullanıcı bulunamadı." });

                // Mevcut şifreyi kontrol et
                if (admin.Password != request.CurrentPassword)
                    return BadRequest(new { message = "Mevcut şifre hatalı." });

                // Yeni şifreyi kaydet
                admin.Password = request.NewPassword;
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Şifre başarıyla değiştirildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şifre değiştirilirken hata oluştu.", error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Kullanıcı adı ve şifre gereklidir.";
                return View();
            }

            var admin = await _dbContext.Admins
                .FirstOrDefaultAsync(x => x.PhoneNumber == username && x.Password == password);

            if (admin == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.FullName),
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}