using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StudentBatchesController : Controller
    {
        private readonly AppDbContext _context;

        public StudentBatchesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/StudentBatches
        public async Task<IActionResult> Index()
        {
            var batches = await _context.StudentBatches
                .OrderByDescending(b => b.EnrollmentYear)
                .ToListAsync();
            return View(batches);
        }

        // GET: Admin/StudentBatches/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/StudentBatches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentBatch batch)
        {
            // Xoa validation cho navigation properties (khong gui tu form)
            ModelState.Remove("InternshipTerms");
            ModelState.Remove("Majors");

            if (ModelState.IsValid)
            {
                _context.StudentBatches.Add(batch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm khóa học thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(batch);
        }

        // GET: Admin/StudentBatches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var batch = await _context.StudentBatches.FindAsync(id);
            if (batch == null) return NotFound();

            return View(batch);
        }

        // POST: Admin/StudentBatches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentBatch batch)
        {
            if (id != batch.Id) return NotFound();

            // Xoa validation cho navigation properties (khong gui tu form)
            ModelState.Remove("InternshipTerms");
            ModelState.Remove("Majors");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(batch);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật khóa học thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BatchExists(batch.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(batch);
        }

        // POST: Admin/StudentBatches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var batch = await _context.StudentBatches.FindAsync(id);
            if (batch != null)
            {
                _context.StudentBatches.Remove(batch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa khóa học thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BatchExists(int id)
        {
            return _context.StudentBatches.Any(e => e.Id == id);
        }
    }
}
