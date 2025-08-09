// PossiLicence/Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PossiLicence.Context;
using PossiLicence.Dtos;
using PossiLicence.Entityies;
using System.Security.Claims;

namespace PossiLicence.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AdminController(DBContext _dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAdmins()
    {
        try
        {
            // Sadece SuperAdmin kullanıcıları görebilir
            if (!await IsSuperAdmin())
                return Forbid("Bu işlem için yetkiniz yok.");

            // Önce temel verileri çek
            var adminsData = await _dbContext.Admins
                .AsNoTracking()
                .OrderByDescending(x => x.CreateAt)
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.PhoneNumber,
                    x.IsSuperAdmin,
                    x.CreateAt,
                    x.Permissions,
                    CompanyCount = _dbContext.Companies.Count(c => c.AdminId == x.Id)
                })
                .ToListAsync();

            // JSON deserialize işlemini memory'de yap
            var admins = adminsData.Select(x => new
            {
                x.Id,
                x.FullName,
                x.PhoneNumber,
                x.IsSuperAdmin,
                x.CreateAt,
                x.CompanyCount,
                PermissionsList = string.IsNullOrEmpty(x.Permissions) ?
                    new List<string>() :
                    System.Text.Json.JsonSerializer.Deserialize<List<string>>(x.Permissions)
            }).ToList();

            return Ok(admins);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcılar yüklenirken hata oluştu.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        try
        {
            if (!await IsSuperAdmin())
                return Forbid("Bu işlem için yetkiniz yok.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var admin = new AdminDbEntity
            {
                FullName = request.FullName.ToUpper(),
                PhoneNumber = request.PhoneNumber,
                Password = request.Password,
                IsSuperAdmin = request.IsSuperAdmin,
                Permissions = System.Text.Json.JsonSerializer.Serialize(request.Permissions)
            };

            _dbContext.Admins.Add(admin);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Kullanıcı başarıyla eklendi.",
                admin = new
                {
                    admin.Id,
                    admin.FullName,
                    admin.PhoneNumber,
                    admin.IsSuperAdmin,
                    admin.CreateAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcı eklenirken hata oluştu.", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAdmin(Guid id, [FromBody] UpdateAdminRequest request)
    {
        try
        {
            if (!await IsSuperAdmin())
                return Forbid("Bu işlem için yetkiniz yok.");

            var admin = await _dbContext.Admins.FindAsync(id);
            if (admin == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            admin.FullName = request.FullName.ToUpper();
            admin.PhoneNumber = request.PhoneNumber;
            admin.IsSuperAdmin = request.IsSuperAdmin;
            admin.Permissions = System.Text.Json.JsonSerializer.Serialize(request.Permissions);

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Kullanıcı başarıyla güncellendi.", admin });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcı güncellenirken hata oluştu.", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAdmin(Guid id)
    {
        try
        {
            if (!await IsSuperAdmin())
                return Forbid("Bu işlem için yetkiniz yok.");

            var admin = await _dbContext.Admins.FindAsync(id);
            if (admin == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            // Kendi kendini silemesin
            var currentAdminId = GetCurrentAdminId();
            if (admin.Id == currentAdminId)
                return BadRequest(new { message = "Kendi hesabınızı silemezsiniz." });

            _dbContext.Admins.Remove(admin);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Kullanıcı başarıyla silindi." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcı silinirken hata oluştu.", error = ex.Message });
        }
    }

    // AdminController.cs'ye eklenecek method
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAdmin(Guid id)
    {
        try
        {
            if (!await IsSuperAdmin())
                return Forbid("Bu işlem için yetkiniz yok.");

            var admin = await _dbContext.Admins
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.PhoneNumber,
                    x.IsSuperAdmin,
                    x.CreateAt,
                    x.Permissions
                })
                .FirstOrDefaultAsync();

            if (admin == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            // JSON deserialize işlemini memory'de yap
            List<string> permissionsList = new List<string>();
            if (!string.IsNullOrEmpty(admin.Permissions))
            {
                try
                {
                    permissionsList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(admin.Permissions);
                }
                catch
                {
                    // JSON parse hatası durumunda boş liste
                }
            }

            return Ok(new
            {
                admin.Id,
                admin.FullName,
                admin.PhoneNumber,
                admin.IsSuperAdmin,
                admin.CreateAt,
                PermissionsList = permissionsList
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcı bilgileri alınırken hata oluştu.", error = ex.Message });
        }
    }

    // AdminController.cs'ye eklenecek method
    [HttpGet("current-user-info")]
    public async Task<IActionResult> GetCurrentUserInfo()
    {
        try
        {
            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out Guid adminId))
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı." });
            }

            var admin = await _dbContext.Admins.FindAsync(adminId);
            if (admin == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            List<string> permissions = new List<string>();
            if (!string.IsNullOrEmpty(admin.Permissions))
            {
                try
                {
                    permissions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(admin.Permissions);
                }
                catch
                {
                    // JSON parse hatası durumunda boş liste
                }
            }

            return Ok(new
            {
                isSuperAdmin = admin.IsSuperAdmin,
                permissions = permissions,
                fullName = admin.FullName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcı bilgileri alınırken hata oluştu.", error = ex.Message });
        }
    }

    // Helper Methods
    private async Task<bool> IsSuperAdmin()
    {
        var adminId = GetCurrentAdminId();
        if (adminId == null) return false;

        var admin = await _dbContext.Admins.FindAsync(adminId);
        return admin?.IsSuperAdmin == true;
    }

    private Guid? GetCurrentAdminId()
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (adminIdClaim != null && Guid.TryParse(adminIdClaim.Value, out Guid adminId))
            return adminId;
        return null;
    }
}