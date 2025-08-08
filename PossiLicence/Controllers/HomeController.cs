using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PossiLicence.Controllers
{
    public class HomeController : Controller
    {


        public IActionResult Index()
        {
            return View();
        }


    }
}
