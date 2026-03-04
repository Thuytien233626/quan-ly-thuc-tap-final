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

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager,
            DNC.InternshipSystem.Infrastructure.Data.AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        // LAY: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Nhan dang nhap theo username/email/mssv tuong ung voi vai tro
                var credential = model.Email?.Trim() ?? string.Empty;

                // tim user bang UserName (admin, giang vien email, sinh vien mssv)
                AppUser? user = await _userManager.FindByNameAsync(credential);

                // neu khong tim thay va credential co @ thi thu tim theo email
                if (user == null && credential.Contains("@"))
                {
                    var normalizedEmail = credential.ToUpper();
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
                }

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return View(model);
                }

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return View(model);
                }

                // Kiểm tra password:
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
            // 2. Logic phan quyen va dieu huong
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // 3. Neu khong co returnUrl -> Kiem tra Role de chuyen huong
            // Sử dụng biến user đã xác thực
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("Index", "AdminHome", new { area = "Admin" });
            }
            else if (roles.Contains("Student"))
            {
                return RedirectToAction("Index", "StudentHome", new { area = "Student" });
            }
            else if (roles.Contains("Lecturer"))
            {
                
                return RedirectToAction("Index", "LecturerHome", new { area = "Lecturer" });
            }

            // Mac dinh: ve trang chu chung (neu khong thuoc role nao dac biet)
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
    }

    return View(model);
}
        // GUI: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}