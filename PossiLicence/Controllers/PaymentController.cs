using Microsoft.AspNetCore.Mvc;
using PossiLicence.Context;

namespace PossiLicence.Controllers;

public class PaymentController(DBContext _dBContext): Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
