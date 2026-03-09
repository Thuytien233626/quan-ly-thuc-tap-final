using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Infrastructure.Data;

[Area("Student")]
public class TranscriptController : Controller
{
    private readonly AppDbContext _context;

    public TranscriptController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Verify(Guid id)
    {
        try
        {
            // Load Grade 
            var grade = await _context.Grades
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.RegistrationId == id);

            if (grade == null)
            {
                return Content($"Lỗi: Không tìm thấy phiếu điểm. RegistrationId: {id}");
            }

            // Load Registration separately to avoid null reference issues
            var registration = await _context.Registrations
                .AsNoTracking()
                .Include(r => r.Student)
                    .ThenInclude(s => s!.User)
                .Include(r => r.Company)
                .Include(r => r.Term)
                .FirstOrDefaultAsync(r => r.Id == grade.RegistrationId);

            if (registration == null)
            {
                return Content($"Lỗi: Enrollment không tồn tại. RegistrationId: {id}");
            }

            if (registration.Student == null || registration.Student.User == null)
            {
                return Content($"Lỗi: Thông tin sinh viên không đầy đủ hoặc bị xóa.");
            }

            // Assign registration to grade for view
            grade.Registration = registration;

            return View(grade);
        }
        catch (Exception ex)
        {
            return Content($"Lỗi xảy ra: {ex.Message}");
        }
    }
}