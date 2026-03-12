using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using System.Security.Claims;

namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProfileController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            if (image != null && image.Length > 0)
            {
                var folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/students"
                );

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Xoa anh cu neu co
                if (!string.IsNullOrEmpty(student.ProfileImage))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.ProfileImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

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

        // DOI MAT KHAU
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });

            var checkCurrent = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!checkCurrent)
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng." });

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Lỗi: {errors}" });
        }
    }
}