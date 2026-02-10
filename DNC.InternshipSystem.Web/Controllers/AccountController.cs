using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DNC.InternshipSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // LAY: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // GUI: /Account/Login
       // POST: /Account/Login
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;

    if (ModelState.IsValid)
    {
        // 1. Kiem tra User/Pass
        var result = await _signInManager.PasswordSignInAsync(
            model.Username,
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
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }
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