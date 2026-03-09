using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Infrastructure.Data;
using System.Security.Claims; 
namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class SRegisterController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 6; // So DN moi trang

        public SRegisterController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Student/SRegister
        public async Task<IActionResult> Index(int page = 1, string? search = null, string? province = null)
        {
            // Query DN doi tac (Status = 1, IsExternal = false)
            var query = _context.Companies
                .Where(c => c.Status == 1 && !c.IsExternal)
                .AsQueryable();

            // Loc theo ten
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search));
                ViewBag.Search = search;
            }

            // Loc theo tinh/thanh pho
            if (!string.IsNullOrWhiteSpace(province))
            {
                query = query.Where(c => c.Province == province);
                ViewBag.SelectedProvince = province;
            }

            // Dem tong so ban ghi (truoc phan trang)
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            // Dam bao page hop le
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Lay du lieu phan trang
            var companies = await query
                .OrderBy(c => c.Province)
                .ThenBy(c => c.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Lay danh sach tinh/thanh pho (tu tat ca DN, khong phan trang)
            var provinces = await _context.Companies
                .Where(c => c.Status == 1 && !c.IsExternal && !string.IsNullOrEmpty(c.Province))
                .Select(c => c.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            ViewBag.Companies = companies;
            ViewBag.Provinces = provinces;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View();
        }
    }
}
