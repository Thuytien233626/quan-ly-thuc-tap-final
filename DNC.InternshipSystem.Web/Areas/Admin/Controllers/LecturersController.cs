using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Infrastructure.Data;
using DNC.InternshipSystem.Core.Entities;
using OfficeOpenXml;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LecturersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LecturersController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var lecturers = await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Classes)
                .OrderBy(l => l.LecturerCode)
                .ToListAsync();
            return View(lecturers);
        }

        // Chạy cái này 1 lần để fix data cũ: /Admin/Lecturers/FixLecturerCodes
        [HttpGet]
        public async Task<IActionResult> FixLecturerCodes()
        {
            var lecturers = await _context.Lecturers.ToListAsync();
            int count = 0;
            foreach (var l in lecturers)
            {
                bool changed = false;
                if (string.IsNullOrEmpty(l.LecturerCode)) 
                {
                    l.LecturerCode = await GenerateUniqueLecturerCode();
                    changed = true;
                }
                if (string.IsNullOrEmpty(l.Specialization)) 
                {
                    l.Specialization = "Công nghệ Thông tin";
                    changed = true;
                }
                if (changed) count++;
            }
            if (count > 0) await _context.SaveChangesAsync();
            return Content($"Đã cập nhật {count} giảng viên. Quay lại trang Index để xem kết quả.");
        }

        // Chay 1 lan de fix tat ca GV cu: /Admin/Lecturers/FixLecturerAccounts
        // Reset mat khau ve Giangvien@123 + doi UserName = Email
        [HttpGet]
        public async Task<IActionResult> FixLecturerAccounts()
        {
            var lecturers = await _context.Lecturers.Include(l => l.User).ToListAsync();
            int updated = 0;
            var errors = new List<string>();

            foreach (var l in lecturers)
            {
                if (l.User == null) continue;

                // 1. Doi UserName = Email (de login bang email)
                if (l.User.UserName != l.User.Email && !string.IsNullOrEmpty(l.User.Email))
                {
                    l.User.UserName = l.User.Email;
                    l.User.NormalizedUserName = l.User.Email.ToUpper();
                    await _userManager.UpdateAsync(l.User);
                }

                // 2. Reset mat khau ve Giangvien@123
                var token = await _userManager.GeneratePasswordResetTokenAsync(l.User);
                var result = await _userManager.ResetPasswordAsync(l.User, token, "Giangvien@123");

                if (result.Succeeded)
                    updated++;
                else
                    errors.Add($"{l.User.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            var msg = $"✅ Đã cập nhật {updated}/{lecturers.Count} giảng viên. Mật khẩu mới: Giangvien@123";
            if (errors.Any())
                msg += $"\n❌ Lỗi: {string.Join("; ", errors)}";

            return Content(msg);
        }

        // ========================================
        // AJAX ENDPOINTS
        // ========================================

        [HttpPost]
        public async Task<IActionResult> CreateAjax(string name, string email, string? phone, string rank, string? spec)
        {
            try
            {
                if (await _userManager.FindByEmailAsync(email) != null)
                    return Json(new { success = false, message = $"Email '{email}' đã được sử dụng!" });

                string code = await GenerateUniqueLecturerCode();

                var user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    PhoneNumber = phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, "Giangvien@123");
                if (!result.Succeeded)
                    return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });

                await _userManager.AddToRoleAsync(user, "Lecturer");

                _context.Lecturers.Add(new Core.Entities.Lecturer
                {
                    UserId = user.Id,
                    LecturerCode = code,
                    DepartmentId = 1,
                    AcademicRank = rank,
                    Specialization = spec
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, code = code, password = "Giangvien@123" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateUniqueLecturerCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Bỏ I, O, 1, 0 để tránh nhầm lẫn
            var random = new Random();
            string code;
            do
            {
                var codeChars = new char[5];
                for (int i = 0; i < 5; i++) codeChars[i] = chars[random.Next(chars.Length)];
                code = "L-" + new string(codeChars);
            } while (await _context.Lecturers.AnyAsync(l => l.LecturerCode == code));
            
            return code;
        }

        [HttpPost]
        public async Task<IActionResult> EditAjax(Guid userId, string rank, string? specialization)
        {
            try
            {
                var lecturer = await _context.Lecturers.FindAsync(userId);
                if (lecturer == null)
                    return Json(new { success = false, message = "Không tìm thấy giảng viên!" });

                lecturer.AcademicRank = rank;
                lecturer.Specialization = specialization;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(Guid userId)
        {
            try
            {
                var lecturer = await _context.Lecturers
                    .Include(l => l.Classes)
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (lecturer == null)
                    return Json(new { success = false, message = "Không tìm thấy giảng viên!" });

                if (lecturer.Classes?.Any() == true)
                    return Json(new { success = false, message = $"Giảng viên đang phụ trách {lecturer.Classes.Count} lớp!" });

                _context.Lecturers.Remove(lecturer);
                if (lecturer.User != null)
                    await _userManager.DeleteAsync(lecturer.User);

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========================================
        // IMPORT EXCEL
        // ========================================

        public IActionResult DownloadTemplate()
        {
            ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("GiangVien");

            // Headers
            string[] headers = { "Mã GV", "Họ tên", "Email", "SĐT", "Học vị", "Chuyên môn" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Sample data
            ws.Cells[2, 1].Value = "GV001";
            ws.Cells[2, 2].Value = "Nguyễn Văn Anh";
            ws.Cells[2, 3].Value = "nva@dnc.edu.vn";
            ws.Cells[2, 4].Value = "0901234567";
            ws.Cells[2, 5].Value = "ThS";
            ws.Cells[2, 6].Value = "Công nghệ Thông tin";

            ws.Cells.AutoFitColumns();

            return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "GiangVien_Mau.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> ImportPreviewAjax(IFormFile excelFile)
        {
            try
            {
                if (excelFile == null || excelFile.Length == 0)
                    return Content("<div class='alert alert-danger'>Vui lòng chọn file!</div>");

                var tempPath = Path.Combine(Path.GetTempPath(), $"lec_import_{Guid.NewGuid()}.xlsx");
                using (var stream = new FileStream(tempPath, FileMode.Create))
                    await excelFile.CopyToAsync(stream);

                ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");
                using var package = new ExcelPackage(new FileInfo(tempPath));
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                    return Content("<div class='alert alert-danger'>File không có worksheet!</div>");

                var preview = new List<LecturerPreviewItem>();
                int rowCount = ws.Dimension?.Rows ?? 0;

                // Find columns
                int colCode = -1, colName = -1, colEmail = -1, colPhone = -1, colRank = -1, colSpec = -1;
                for (int col = 1; col <= (ws.Dimension?.Columns ?? 10); col++)
                {
                    var h = ws.Cells[1, col].Value?.ToString()?.ToLower()?.Trim() ?? "";
                    if (h.Contains("mã") && h.Contains("gv")) colCode = col;
                    else if (h.Contains("họ") && h.Contains("tên") || h == "họ tên") colName = col;
                    else if (h.Contains("email")) colEmail = col;
                    else if (h.Contains("sđt") || h.Contains("điện thoại")) colPhone = col;
                    else if (h.Contains("học vị")) colRank = col;
                    else if (h.Contains("chuyên môn")) colSpec = col;
                }

                if (colCode < 0 || colName < 0 || colEmail < 0)
                    return Content("<div class='alert alert-danger'>File thiếu cột bắt buộc: Mã GV, Họ tên, Email!</div>");

                for (int row = 2; row <= rowCount; row++)
                {
                    var code = ws.Cells[row, colCode].Value?.ToString()?.Trim();
                    var name = ws.Cells[row, colName].Value?.ToString()?.Trim();
                    var email = ws.Cells[row, colEmail].Value?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name)) continue;

                    var existsCode = await _context.Lecturers.AnyAsync(l => l.LecturerCode == code);
                    var existsEmail = await _userManager.FindByEmailAsync(email ?? "") != null;

                    preview.Add(new LecturerPreviewItem
                    {
                        Code = code,
                        Name = name,
                        Email = email ?? "",
                        Phone = colPhone > 0 ? ws.Cells[row, colPhone].Value?.ToString()?.Trim() : null,
                        Rank = colRank > 0 ? ws.Cells[row, colRank].Value?.ToString()?.Trim() : "ThS",
                        Spec = colSpec > 0 ? ws.Cells[row, colSpec].Value?.ToString()?.Trim() : null,
                        AlreadyExists = existsCode || existsEmail,
                        Status = existsCode ? "Mã GV đã tồn tại" : (existsEmail ? "Email đã tồn tại" : "OK")
                    });
                }

                ViewBag.TempFilePath = tempPath;
                ViewBag.ValidCount = preview.Count(p => !p.AlreadyExists);
                ViewBag.SkipCount = preview.Count(p => p.AlreadyExists);
                return PartialView("_ImportPreviewPartial", preview);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Lỗi: {ex.Message}</div>");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmImportAjax(string tempFilePath)
        {
            var results = new List<LecturerImportResult>();
            int success = 0, skip = 0, error = 0;

            try
            {
                if (!System.IO.File.Exists(tempFilePath))
                    return Content("<div class='alert alert-danger'>File tạm không tồn tại!</div>");

                ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");
                using var package = new ExcelPackage(new FileInfo(tempFilePath));
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null) return Content("<div class='alert alert-danger'>Lỗi đọc file!</div>");

                int rowCount = ws.Dimension?.Rows ?? 0;

                // Find columns
                int colCode = -1, colName = -1, colEmail = -1, colPhone = -1, colRank = -1, colSpec = -1;
                for (int col = 1; col <= (ws.Dimension?.Columns ?? 10); col++)
                {
                    var h = ws.Cells[1, col].Value?.ToString()?.ToLower()?.Trim() ?? "";
                    if (h.Contains("mã") && h.Contains("gv")) colCode = col;
                    else if (h.Contains("họ") && h.Contains("tên") || h == "họ tên") colName = col;
                    else if (h.Contains("email")) colEmail = col;
                    else if (h.Contains("sđt") || h.Contains("điện thoại")) colPhone = col;
                    else if (h.Contains("học vị")) colRank = col;
                    else if (h.Contains("chuyên môn")) colSpec = col;
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    var r = new LecturerImportResult();
                    try
                    {
                        var code = ws.Cells[row, colCode].Value?.ToString()?.Trim();
                        var name = ws.Cells[row, colName].Value?.ToString()?.Trim();
                        var email = ws.Cells[row, colEmail].Value?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name)) continue;

                        r.Code = code;
                        r.Name = name;

                        if (await _context.Lecturers.AnyAsync(l => l.LecturerCode == code))
                        { r.Status = "Bỏ qua"; r.Message = "Mã GV đã tồn tại"; skip++; results.Add(r); continue; }

                        if (await _userManager.FindByEmailAsync(email ?? "") != null)
                        { r.Status = "Bỏ qua"; r.Message = "Email đã tồn tại"; skip++; results.Add(r); continue; }

                        var phone = colPhone > 0 ? ws.Cells[row, colPhone].Value?.ToString()?.Trim() : null;
                        var rank = colRank > 0 ? ws.Cells[row, colRank].Value?.ToString()?.Trim() : "ThS";
                        var spec = colSpec > 0 ? ws.Cells[row, colSpec].Value?.ToString()?.Trim() : null;

                        var user = new AppUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = name,
                            PhoneNumber = phone,
                            EmailConfirmed = true,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };

                        var createRes = await _userManager.CreateAsync(user, $"Gv@{code}");
                        if (!createRes.Succeeded)
                        {
                            r.Status = "Lỗi";
                            r.Message = string.Join(", ", createRes.Errors.Select(e => e.Description));
                            error++;
                            results.Add(r);
                            continue;
                        }

                        await _userManager.AddToRoleAsync(user, "Lecturer");
                        _context.Lecturers.Add(new Core.Entities.Lecturer
                        {
                            UserId = user.Id,
                            LecturerCode = code,
                            DepartmentId = 1,
                            AcademicRank = rank,
                            Specialization = spec
                        });
                        await _context.SaveChangesAsync();

                        r.Status = "Thành công";
                        r.Message = $"Pass: Gv@{code}";
                        success++;
                    }
                    catch (Exception ex)
                    {
                        r.Status = "Lỗi";
                        r.Message = ex.Message;
                        error++;
                    }
                    results.Add(r);
                }

                try { System.IO.File.Delete(tempFilePath); } catch { }

                ViewBag.Success = success;
                ViewBag.Skip = skip;
                ViewBag.Error = error;
                return PartialView("_ImportResultPartial", results);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Lỗi: {ex.Message}</div>");
            }
        }
    }

    public class LecturerPreviewItem
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Rank { get; set; }
        public string? Spec { get; set; }
        public bool AlreadyExists { get; set; }
        public string Status { get; set; } = "";
    }

    public class LecturerImportResult
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
