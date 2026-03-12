using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InternshipTermsController : Controller
    {
        private readonly AppDbContext _context;

        public InternshipTermsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/InternshipTerms
        public async Task<IActionResult> Index()
        {
            var terms = await _context.InternshipTerms
                .Include(t => t.Batch)
                .OrderByDescending(t => t.InternshipStart)
                .ToListAsync();
            return View(terms);
        }

        // GET: Admin/InternshipTerms/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View();
        }

        // POST: Admin/InternshipTerms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InternshipTerm term)
        {
            // Xoa validation cho navigation property (khong gui tu form)
            ModelState.Remove("Batch");

            if (ModelState.IsValid)
            {
                // Kiem tra logic ngay thang
                if (!ValidateDates(term)) 
                {
                    await LoadDropdownsAsync();
                    return View(term);
                }

                // Tu dong tinh ngay ket thuc = ngay bat dau + so tuan
                term.EndDate = term.InternshipStart.AddDays(term.DurationInWeeks * 7);

                _context.Add(term);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo đợt thực tập thành công!";
                return RedirectToAction(nameof(Index));
            }
            await LoadDropdownsAsync();
            return View(term);
        }

        // GET: Admin/InternshipTerms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var term = await _context.InternshipTerms.FindAsync(id);
            if (term == null) return NotFound();

            await LoadDropdownsAsync();
            return View(term);
        }

        // POST: Admin/InternshipTerms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InternshipTerm term)
        {
            if (id != term.Id) return NotFound();

            // Xoa validation cho navigation property (khong gui tu form)
            ModelState.Remove("Batch");

            if (ModelState.IsValid)
            {
                if (!ValidateDates(term))
                {
                    await LoadDropdownsAsync();
                    return View(term);
                }

                try
                {
                    // Tu dong tinh ngay ket thuc = ngay bat dau + so tuan
                    term.EndDate = term.InternshipStart.AddDays(term.DurationInWeeks * 7);
                    _context.Update(term);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật đợt thực tập thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TermExists(term.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Hien thi loi validation de user thay
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                TempData["Error"] = error.ErrorMessage;
            }

            await LoadDropdownsAsync();
            return View(term);
        }

        // GET: Admin/InternshipTerms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var term = await _context.InternshipTerms
                .Include(t => t.Batch)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (term == null) return NotFound();
            return View(term);
        }

        // POST: Admin/InternshipTerms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var term = await _context.InternshipTerms.FindAsync(id);
            if (term != null)
            {
                _context.InternshipTerms.Remove(term);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa đợt thực tập thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper: Load dropdowns
        private async Task LoadDropdownsAsync()
        {
            // Khóa học
            ViewBag.Batches = new SelectList(
                await _context.StudentBatches.Where(b => b.IsActive).OrderByDescending(b => b.EnrollmentYear).ToListAsync(),
                "Id", "BatchCode"
            );

            // Năm học
            var currentYear = DateTime.Now.Year;
            ViewBag.AcademicYears = new SelectList(new[]
            {
                $"{currentYear-1}-{currentYear}",
                $"{currentYear}-{currentYear+1}",
                $"{currentYear+1}-{currentYear+2}"
            });

            // Học kỳ
            ViewBag.Semesters = new SelectList(new[]
            {
                new { Value = 1, Text = "Học kỳ 1" },
                new { Value = 2, Text = "Học kỳ 2" },
                new { Value = 3, Text = "Học kỳ 3" }
            }, "Value", "Text", 3);

            // Thời lượng
            ViewBag.Durations = new SelectList(new[]
            {
                new { Value = 4, Text = "1 tháng (4 tuần)" },
                new { Value = 6, Text = "1.5 tháng (6 tuần)" },
                new { Value = 8, Text = "2 tháng (8 tuần)" },
                new { Value = 12, Text = "3 tháng (12 tuần)" }
            }, "Value", "Text", 8);

            // Trạng thái
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(TermStatus))
                .Cast<TermStatus>()
                .Select(s => new { Value = (int)s, Text = GetStatusText(s) }),
                "Value", "Text");
        }

        private string GetStatusText(TermStatus status) => status switch
        {
            TermStatus.Draft => "Bản nháp",
            TermStatus.Open => "Đang mở đăng ký",
            TermStatus.InProgress => "Đang thực tập",
            TermStatus.Grading => "Đang chấm điểm",
            TermStatus.Completed => "Hoàn thành",
            _ => status.ToString()
        };

        private bool ValidateDates(InternshipTerm term)
        {
            if (term.RegistrationStart >= term.RegistrationEnd)
            {
                ModelState.AddModelError("RegistrationEnd", "Hạn đăng ký phải sau ngày mở đăng ký.");
                return false;
            }
            if (term.RegistrationEnd > term.InternshipStart)
            {
                ModelState.AddModelError("InternshipStart", "Ngày bắt đầu TT phải sau hạn đăng ký.");
                return false;
            }
            return true;
        }

        private bool TermExists(int id) => _context.InternshipTerms.Any(e => e.Id == id);
    }
}
