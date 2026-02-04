using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] 
    [Authorize(Roles = "Admin")] 
    public class AdminHomeController : Controller
    {
        public IActionResult Index()
        {
            return View(); 
        }
    }
}