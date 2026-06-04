using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace QLNS.FullNet.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "Employee")]
    public IActionResult EmployeeDashboard()
    {
        return View();
    }
}
