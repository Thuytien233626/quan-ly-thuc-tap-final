using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DNC.InternshipSystem.Core.Interfaces;
using System.Security.Claims;

namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class SRegisterController : Controller
    {
        private readonly IRegistrationService _registrationService;
        private readonly ICompanyService _companyService;

        public SRegisterController(
            IRegistrationService registrationService,
            ICompanyService companyService)
        {
            _registrationService = registrationService;
            _companyService = companyService;
        }

        // Lay ID nguoi dung hien tai tu Cookie xac thuc
        private Guid GetCurrentUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // GET: /Student/SRegister — Danh sach doanh nghiep doi tac
        public async Task<IActionResult> Index(int page = 1, string? search = null, string? province = null)
        {
            const int pageSize = 6;

            // Goi Service lay danh sach doanh nghiep co phan trang
            var result = await _companyService.GetApprovedCompanies(search, province, page, pageSize);
            var provinces = await _companyService.GetProvinces();

            ViewBag.Companies = result.Items;
            ViewBag.Provinces = provinces;
            ViewBag.CurrentPage = result.CurrentPage;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.TotalItems = result.TotalItems;
            ViewBag.Search = search;
            ViewBag.SelectedProvince = province;

            return View();
        }

        // GET: /Student/SRegister/MyRegistration — Xem don dang ky cua toi
        public async Task<IActionResult> MyRegistration()
        {
            var userId = GetCurrentUserId();
            var registration = await _registrationService.GetMyRegistration(userId);
            return View(registration);
        }

        // POST: /Student/SRegister/SubmitInternal — Dang ky tai doanh nghiep doi tac
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitInternal(int companyId, string position)
        {
            var userId = GetCurrentUserId();
            var result = await _registrationService.RegisterInternal(userId, companyId, position);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Student/SRegister/SubmitExternal — Dang ky tai doanh nghiep tu tim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitExternal(
            string CompanyName, string Address, string? TaxCode,
            string ContactPerson, string? PositionTitle, string Phone, string Email,
            string Position, string? Note)
        {
            var userId = GetCurrentUserId();
            var result = await _registrationService.RegisterExternal(
                userId, CompanyName, Address, TaxCode,
                ContactPerson, PositionTitle, Phone, Email, Position, Note);

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Student/SRegister/UpdateRegistration — Huy don dang ky
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRegistration(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _registrationService.CancelRegistration(userId, id);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(MyRegistration));
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
