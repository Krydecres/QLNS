using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QLNS.FullNet.Web.Models;

namespace QLNS.FullNet.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

}
