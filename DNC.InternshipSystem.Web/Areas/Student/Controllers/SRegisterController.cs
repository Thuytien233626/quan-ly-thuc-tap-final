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
      


      // POST: /Student/SRegister/SubmitInternal
       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> SubmitInternal(int companyId, string position)
       {
           try
           {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))

          {
            return Json(new { success = false, message = "Không thể xác định sinh viên." });

           }


            Guid studentUserId = Guid.Parse(userIdClaim);

            var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == studentUserId);
             if (student == null)
        {
            return Json(new { success = false, message = "Sinh viên không tồn tại trong hệ thống." });
        }

        bool existed = await _context.Registrations
            .AnyAsync(r => r.StudentId == studentUserId);
             if (existed)
           {
            return Json(new
            {
                success = false,
                message = "Mỗi sinh viên chỉ được đăng ký 1 doanh nghiệp."
            });
           }
            if (string.IsNullOrWhiteSpace(position))
            {

                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập vị trí thực tập."
                });
            }

        var currentTerm = await _context.InternshipTerms
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.RegistrationStart)
            .FirstOrDefaultAsync();

        if (currentTerm == null)
        {
            return Json(new
            {
                success = false,
                message = "Không có đợt thực tập nào đang mở."
            });
        }

        var registration = new DNC.InternshipSystem.Core.Entities.Registration
        {
            StudentId = studentUserId,
            TermId = currentTerm.Id,
            CompanyId = companyId,
            Position = position,
            Status = 0, // CHỜ DUYỆT
            CreatedDate = DateTime.Now
        };

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = "Đăng ký thực tập thành công. Vui lòng chờ giảng viên duyệt."
        });
    }
    catch (Exception ex)
    {
        return Json(new
        {
            success = false,
            message = "Lỗi hệ thống: " + ex.Message
        });
    }
}
[HttpGet]
public async Task<IActionResult> MyRegistration()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim))
    {
        return Unauthorized();
    }

    Guid studentUserId = Guid.Parse(userIdClaim);

    var registration = await _context.Registrations
        .Include(r => r.Company)
        .Include(r => r.Term)
        .FirstOrDefaultAsync(r => r.StudentId == studentUserId);

    return View(registration);
}
// POST: /Student/SRegister/UpdateRegistrations
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateRegistration(Guid id)
{
    var registration = await _context.Registrations
        .FirstOrDefaultAsync(r => r.Id == id);

    if (registration == null)
        return NotFound();

    // Chỉ cho sửa khi đang CHỜ DUYỆT
    if (registration.Status != 0)
    {
        TempData["Error"] = "Chỉ được sửa khi đơn đang chờ duyệt.";
        return RedirectToAction("MyRegistration");
    }

    // XÓA đơn đăng ký
    _context.Registrations.Remove(registration);

    await _context.SaveChangesAsync();

    // Quay lại trang đăng ký doanh nghiệp
    return RedirectToAction("Index");
}
    }
}