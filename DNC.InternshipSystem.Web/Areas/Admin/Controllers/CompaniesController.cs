using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CompaniesController : Controller
    {
        private readonly AppDbContext _context;

        public CompaniesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? province, string? search, string tab = "partner")
        {
            ViewData["Title"] = "Quản lý Doanh nghiệp";
            ViewBag.CurrentProvince = province;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentTab = tab;

            bool isExternal = tab == "external";

            // Lay danh sach tinh thanh de loc (tu tab hien tai)
            ViewBag.Provinces = await _context.Companies
                .Where(c => c.IsExternal == isExternal)
                .Select(c => c.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            var query = _context.Companies
                .Include(c => c.Registrations)
                .Where(c => c.IsExternal == isExternal);

            if (!string.IsNullOrEmpty(province))
                query = query.Where(c => c.Province == province);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Name.Contains(search) || c.Address.Contains(search));

            var companies = await query.OrderBy(c => c.Province).ThenBy(c => c.Name).ToListAsync();
            
            // Thong ke so luong
            ViewBag.PartnerCount = await _context.Companies.CountAsync(c => !c.IsExternal);
            ViewBag.ExternalCount = await _context.Companies.CountAsync(c => c.IsExternal);
            ViewBag.PendingCount = await _context.Companies.CountAsync(c => c.IsExternal && c.Status == 0);
            
            return View(companies);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax(string name, string address, string province, string? phone, string? email, string? contactPerson, bool isExternal = false)
        {
            try
            {
                var company = new Company
                {
                    Name = name,
                    Address = address,
                    Province = province,
                    PhoneNumber = phone,
                    ContactEmail = email,
                    ContactPerson = contactPerson,
                    IsExternal = isExternal,
                    Status = 1 // Da xac nhan boi Admin
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                return Json(new { success = true, id = company.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAjax(int id, string name, string address, string province, string? phone, string? email, string? contactPerson)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null) return Json(new { success = false, message = "Không tìm thấy doanh nghiệp!" });

                company.Name = name;
                company.Address = address;
                company.Province = province;
                company.PhoneNumber = phone;
                company.ContactEmail = email;
                company.ContactPerson = contactPerson;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveAjax(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null) return Json(new { success = false, message = "Không tìm thấy doanh nghiệp!" });

                company.Status = 1; // Duyet
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null) return Json(new { success = false, message = "Không tìm thấy doanh nghiệp!" });

                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanyAjax(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return Json(new { success = false });
            return Json(new { 
                success = true, 
                id = company.Id,
                name = company.Name, 
                address = company.Address, 
                province = company.Province, 
                phone = company.PhoneNumber,
                email = company.ContactEmail,
                contactPerson = company.ContactPerson,
                isExternal = company.IsExternal,
                status = company.Status
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentsAjax(int id)
        {
            var registrations = await _context.Registrations
                .Include(r => r.Student)
                    .ThenInclude(s => s!.User)
                .Where(r => r.CompanyId == id)
                .Select(r => new {
                    studentCode = r.Student != null ? r.Student.StudentCode : "",
                    studentName = r.Student != null && r.Student.User != null ? r.Student.User.FullName : "",
                    position = r.Position,
                    status = r.Status
                })
                .ToListAsync();
            return Json(registrations);
        }
    }
}
