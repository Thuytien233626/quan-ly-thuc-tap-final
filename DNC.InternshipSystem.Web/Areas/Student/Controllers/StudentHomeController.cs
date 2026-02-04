using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")] 
    [Authorize(Roles = "Student")] 
    public class StudentHomeController : Controller
    {
        public IActionResult Index()
        {
            return View(); 
        }
    }
}