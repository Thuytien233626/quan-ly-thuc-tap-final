using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DNC.InternshipSystem.Web.Areas.Lecturer.Controllers
{
    [Area("Lecturer")] 
    [Authorize(Roles = "Lecturer")] 
    public class LecturerHomeController : Controller
    {
        public IActionResult Index()
        {
            return View(); 
        }
    }
}