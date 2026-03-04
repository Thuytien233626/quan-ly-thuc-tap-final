using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DNC.InternshipSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly DNC.InternshipSystem.Infrastructure.Data.AppDbContext _context;

        public AccountController(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            DNC.InternshipSystem.Infrastructure.Data.AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        // =========================
        // GET: /Account/Login
        // =========================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // =========================
        // POST: /Account/Login
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            string credential = model.Email?.Trim() ?? string.Empty;

            
            AppUser? user = await _userManager.FindByNameAsync(credential);

            if (user == null && credential.Contains("@"))
            {
                string normalizedEmail = credential.ToUpper();
                user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            }

            if (user == null || user.UserName == null)
{
    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
    return View(model);
}

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return RedirectToAction("Index", "AdminHome", new { area = "Admin" });

            if (roles.Contains("Student"))
                return RedirectToAction("Index", "StudentHome", new { area = "Student" });

            if (roles.Contains("Lecturer"))
                return RedirectToAction("Index", "LecturerHome", new { area = "Lecturer" });

            // Mặc định
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // POST: /Account/Logout
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}