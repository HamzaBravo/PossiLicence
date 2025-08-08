using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PossiLicence.Context;

namespace PossiLicence.Controllers;

[Authorize]
public class AuthController(DBContext _dBContext) : Controller
{
    public IActionResult Index()
    {
        return View();
    }


    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> LoginAsync(string username, string password)
    {
        await Task.CompletedTask;
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }
}
