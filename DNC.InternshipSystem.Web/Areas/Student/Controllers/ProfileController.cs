using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Infrastructure.Data;
using System.Security.Claims;

namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Student/Profile
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            Guid userId = Guid.Parse(userIdClaim);

            var student = await _context.Students
                .Include(s => s.Class)
                    .ThenInclude(c => c.Major)
                        .ThenInclude(m => m.Batch)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            student.User = user;

            return View(student);
        }


        // UPDATE PHONE
        [HttpPost]
        public async Task<IActionResult> UpdatePhone(string phone)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            Guid userId = Guid.Parse(userIdClaim);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound();

            student.Phone = phone;

            await _context.SaveChangesAsync();

            return Ok();
        }


        // UPLOAD AVATAR
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            Guid userId = Guid.Parse(userIdClaim);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound();

            // Chỉ cho upload 1 lần
            if (!string.IsNullOrEmpty(student.ProfileImage))
            {
                TempData["Error"] = "Bạn chỉ được upload avatar một lần.";
                return RedirectToAction("Index");
            }

            if (image != null && image.Length > 0)
            {
                var folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/students"
                );

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);

                var path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                student.ProfileImage = "/uploads/students/" + fileName;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}