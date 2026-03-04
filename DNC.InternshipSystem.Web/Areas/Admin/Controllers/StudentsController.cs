using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Infrastructure.Data;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using OfficeOpenXml;
using System.Text;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public StudentsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Lay cay thu muc sinh vien
            var batches = await _context.StudentBatches
                .Include(b => b.Majors.Where(m => m.IsActive))
                    .ThenInclude(m => m.Classes.Where(c => c.IsActive))
                        .ThenInclude(c => c.Students)
                .Include(b => b.Majors.Where(m => m.IsActive))
                    .ThenInclude(m => m.Classes.Where(c => c.IsActive))
                        .ThenInclude(c => c.Lecturer)
                            .ThenInclude(l => l.User)
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.EnrollmentYear)
                .ToListAsync();
            return View(batches);
        }

        // AJAX: Lay danh sach sinh vien theo lop
        public async Task<IActionResult> GetStudentsByClass(string classId)
        {
            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.OrderNumber)
                .ThenBy(s => s.StudentCode)
                .ToListAsync();
            return PartialView("_StudentListPartial", students);
        }


        // GET: Form nhap du lieu tu Excel
        public IActionResult Import()
        {
            return View();
        }

        // POST: Upload Excel va hien thi XEM TRUOC
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel!";
                return View();
            }

            if (!excelFile.FileName.EndsWith(".xlsx") && !excelFile.FileName.EndsWith(".xls"))
            {
                TempData["Error"] = "File phải có định dạng .xlsx hoặc .xls!";
                return View();
            }

            // Cau hinh license EPPlus 8
            ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");

            var preview = new Models.ImportPreviewViewModel { FileName = excelFile.FileName };

            try
            {
                // Luu file tam de xac nhan sau
                var tempFileName = $"{Guid.NewGuid()}.xlsx";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await excelFile.CopyToAsync(fileStream);
                }
                preview.TempFilePath = tempPath;

                // Doc file Excel de hien thi xem truoc
                using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
                using var package = new ExcelPackage(stream);
                
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    TempData["Error"] = "File Excel không có worksheet!";
                    return View();
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;
                preview.TotalRows = rowCount;
                
                // Tu dong nhan dien cot theo ten tieu de
                int headerRow = -1;
                int colMSSV = -1, colHoDem = -1, colTen = -1, colGioiTinh = -1, colNgaySinh = -1, colLopHoc = -1;
                
                for (int row = 1; row <= Math.Min(15, rowCount); row++)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim()?.ToLower() ?? "";
                        
                        if (cellValue.Contains("mã sinh viên") || cellValue.Contains("ma sinh vien") || cellValue == "mssv")
                        {
                            headerRow = row;
                            colMSSV = col;
                            preview.HasMSSV = true;
                        }
                        else if (cellValue.Contains("họ đệm") || cellValue.Contains("ho dem") || cellValue == "họ")
                        {
                            colHoDem = col;
                            preview.HasHoDem = true;
                        }
                        else if (cellValue == "tên" || cellValue == "ten")
                        {
                            colTen = col;
                            preview.HasTen = true;
                        }
                        else if (cellValue.Contains("giới tính") || cellValue.Contains("gioi tinh"))
                        {
                            colGioiTinh = col;
                            preview.HasGioiTinh = true;
                        }
                        else if (cellValue.Contains("ngày sinh") || cellValue.Contains("ngay sinh"))
                        {
                            colNgaySinh = col;
                            preview.HasNgaySinh = true;
                        }
                        else if (cellValue.Contains("lớp") || cellValue.Contains("lop"))
                        {
                            colLopHoc = col;
                            preview.HasLopHoc = true;
                        }
                    }
                    if (colMSSV > 0) break;
                }
                
                preview.HeaderRow = headerRow;
                
                if (!preview.AllRequiredColumnsFound)
                {
                    TempData["Error"] = "File Excel thiếu các cột bắt buộc: Mã sinh viên, Họ đệm, Tên!";
                    return View("ImportPreview", preview);
                }
                
                // Doc du lieu sinh vien (chi cac dong hop le)
                var existingCodes = await _context.Students.Select(s => s.StudentCode).ToListAsync();
                var existingClasses = await _context.Classes.Select(c => c.Id).ToListAsync();
                
                for (int row = headerRow + 1; row <= rowCount; row++)
                {
                    var studentCode = worksheet.Cells[row, colMSSV].Value?.ToString()?.Trim() ?? "";
                    
                    // Bo qua MSSV trong hoac khong hop le
                    if (string.IsNullOrEmpty(studentCode) || !studentCode.All(char.IsDigit))
                    {
                        preview.SkippedRows++;
                        continue;
                    }
                    
                    var lastName = colHoDem > 0 ? worksheet.Cells[row, colHoDem].Value?.ToString()?.Trim() ?? "" : "";
                    var firstName = colTen > 0 ? worksheet.Cells[row, colTen].Value?.ToString()?.Trim() ?? "" : "";
                    var genderText = colGioiTinh > 0 ? worksheet.Cells[row, colGioiTinh].Value?.ToString()?.Trim() ?? "" : "";
                    var dobValue = colNgaySinh > 0 ? worksheet.Cells[row, colNgaySinh].Value : null;
                    var classCode = colLopHoc > 0 ? worksheet.Cells[row, colLopHoc].Value?.ToString()?.Trim() ?? "" : "";
                    
                    var dobString = "";
                    if (dobValue is DateTime dt) dobString = dt.ToString("dd/MM/yyyy");
                    else if (dobValue != null) dobString = dobValue.ToString() ?? "";
                    
                    var item = new Models.StudentPreviewItem
                    {
                        RowNumber = row,
                        StudentCode = studentCode,
                        LastName = lastName,
                        FirstName = firstName,
                        Gender = genderText,
                        DateOfBirth = dobString,
                        ClassCode = classCode,
                        ClassExists = string.IsNullOrEmpty(classCode) || existingClasses.Contains(classCode),
                        AlreadyExists = existingCodes.Contains(studentCode)
                    };
                    
                    if (item.AlreadyExists)
                    {
                        item.Status = "Đã tồn tại";
                        preview.ExistingStudents++;
                    }
                    else if (!item.ClassExists)
                    {
                        item.Status = "Lớp không tồn tại";
                    }
                    
                    preview.Students.Add(item);
                }
                
                preview.ValidRows = preview.Students.Count;
                
                return View("ImportPreview", preview);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi xử lý file: {ex.Message}";
                return View();
            }
        }

        // POST: Xac nhan va luu vao co so du lieu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmImport(string tempFilePath)
        {
            if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
            {
                TempData["Error"] = "File tạm không tồn tại. Vui lòng upload lại!";
                return RedirectToAction("Import");
            }
            
            ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");
            
            var importResults = new List<ImportResult>();
            int successCount = 0, skipCount = 0, errorCount = 0;
            
            try
            {
                using var stream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null) throw new Exception("Worksheet not found");
                
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;
                
                // Nhan dien lai cac cot
                int headerRow = -1;
                int colMSSV = -1, colHoDem = -1, colTen = -1, colGioiTinh = -1, colNgaySinh = -1, colLopHoc = -1;
                
                for (int row = 1; row <= Math.Min(15, rowCount); row++)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim()?.ToLower() ?? "";
                        
                        if (cellValue.Contains("mã sinh viên") || cellValue.Contains("ma sinh vien") || cellValue == "mssv")
                        { headerRow = row; colMSSV = col; }
                        else if (cellValue.Contains("họ đệm") || cellValue.Contains("ho dem") || cellValue == "họ")
                        { colHoDem = col; }
                        else if (cellValue == "tên" || cellValue == "ten")
                        { colTen = col; }
                        else if (cellValue.Contains("giới tính") || cellValue.Contains("gioi tinh"))
                        { colGioiTinh = col; }
                        else if (cellValue.Contains("ngày sinh") || cellValue.Contains("ngay sinh"))
                        { colNgaySinh = col; }
                        else if (cellValue.Contains("lớp") || cellValue.Contains("lop"))
                        { colLopHoc = col; }
                    }
                    if (colMSSV > 0) break;
                }
                
                // Xu ly tung dong hop le
                for (int row = headerRow + 1; row <= rowCount; row++)
                {
                    var result = new ImportResult { RowNumber = row };
                    
                    try
                    {
                        var studentCode = worksheet.Cells[row, colMSSV].Value?.ToString()?.Trim();
                        
                        if (string.IsNullOrEmpty(studentCode) || !studentCode.All(char.IsDigit))
                            continue; // Am tham bo qua cac dong khong hop le
                        
                        var lastName = colHoDem > 0 ? worksheet.Cells[row, colHoDem].Value?.ToString()?.Trim() ?? "" : "";
                        var firstName = colTen > 0 ? worksheet.Cells[row, colTen].Value?.ToString()?.Trim() ?? "" : "";
                        var genderText = colGioiTinh > 0 ? worksheet.Cells[row, colGioiTinh].Value?.ToString()?.Trim() ?? "" : "";
                        var dobValue = colNgaySinh > 0 ? worksheet.Cells[row, colNgaySinh].Value : null;
                        var classCode = colLopHoc > 0 ? worksheet.Cells[row, colLopHoc].Value?.ToString()?.Trim() ?? "" : "";
                        
                        result.StudentCode = studentCode;
                        result.FullName = $"{lastName} {firstName}".Trim();
                        
                        // Kiem tra ton tai
                        if (await _context.Students.AnyAsync(s => s.StudentCode == studentCode))
                        {
                            result.Status = "Bỏ qua";
                            result.Message = "MSSV đã tồn tại";
                            skipCount++;
                            importResults.Add(result);
                            continue;
                        }
                        
                        // Phan tich du lieu
                        var gender = genderText.ToLower().Contains("nữ") ? Gender.Female : Gender.Male;
                        DateTime? dob = null;
                        if (dobValue is DateTime dt) dob = dt;
                        else if (dobValue != null && DateTime.TryParse(dobValue.ToString(), out DateTime parsed)) dob = parsed;
                        
                        string? validClassId = null;
                        if (!string.IsNullOrEmpty(classCode) && await _context.Classes.AnyAsync(c => c.Id == classCode))
                            validClassId = classCode;
                        
                        // Tao nguoi dung
                        var fullName = $"{lastName} {firstName}".Trim();
                        var user = new AppUser
                        {
                            UserName = studentCode,
                            Email = $"{studentCode}@student.dnc.edu.vn",
                            FullName = fullName,
                            EmailConfirmed = true,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };
                        
                        var createResult = await _userManager.CreateAsync(user, $"Sv@{studentCode}");
                        if (!createResult.Succeeded)
                        {
                            result.Status = "Lỗi";
                            result.Message = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            errorCount++;
                            importResults.Add(result);
                            continue;
                        }
                        
                        await _userManager.AddToRoleAsync(user, "Student");
                        
                        _context.Students.Add(new Core.Entities.Student
                        {
                            UserId = user.Id,
                            StudentCode = studentCode,
                            ClassId = validClassId,
                            Gender = gender,
                            DateOfBirth = dob
                        });
                        await _context.SaveChangesAsync();
                        
                        result.Status = "Thành công";
                        result.Message = $"Tạo tài khoản (Pass: Sv@{studentCode})";
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Status = "Lỗi";
                        result.Message = ex.Message;
                        errorCount++;
                    }
                    
                    importResults.Add(result);
                }
                
                // Don dep file tam
                try { System.IO.File.Delete(tempFilePath); } catch { }
                
                TempData["Success"] = $"Import hoàn tất! Thành công: {successCount}, Bỏ qua: {skipCount}, Lỗi: {errorCount}";
                return View("ImportResult", importResults);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi import: {ex.Message}";
                return RedirectToAction("Import");
            }
        }

        // Tai file mau Excel
        public IActionResult DownloadTemplate()
        {
            ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");
            
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Danh sach sinh vien");

            // Tieu de
            ws.Cells[1, 1].Value = "STT";
            ws.Cells[1, 2].Value = "Mã sinh viên";
            ws.Cells[1, 3].Value = "Họ đệm";
            ws.Cells[1, 4].Value = "Tên";
            ws.Cells[1, 5].Value = "Giới tính";
            ws.Cells[1, 6].Value = "Ngày sinh";
            ws.Cells[1, 7].Value = "Lớp học";

            // Du lieu mau
            ws.Cells[2, 1].Value = 1;
            ws.Cells[2, 2].Value = "233626";
            ws.Cells[2, 3].Value = "Trần Thị Thủy";
            ws.Cells[2, 4].Value = "Tiên";
            ws.Cells[2, 5].Value = "Nữ";
            ws.Cells[2, 6].Value = "04/09/2005";
            ws.Cells[2, 7].Value = "DH23TIN03";

            // Dinh dang tieu de
            using (var range = ws.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            ws.Cells.AutoFitColumns();

            var content = package.GetAsByteArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                "Template_DanhSachSinhVien.xlsx");
        }

        // QUAN LY LOP HOC

        // GET: Tao moi Lop
        public async Task<IActionResult> CreateClass(int majorId)
        {
            var major = await _context.Majors
                .Include(m => m.Batch)
                .FirstOrDefaultAsync(m => m.Id == majorId);
            
            if (major == null) return NotFound();

            ViewBag.Major = major;
            ViewBag.MajorId = majorId;
            return View();
        }

        // POST: Tao moi Lop
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(int majorId, string classId, string className)
        {
            var major = await _context.Majors
                .Include(m => m.Batch)
                .FirstOrDefaultAsync(m => m.Id == majorId);
            
            if (major == null) return NotFound();

            // Kiem tra lop da ton tai chua
            if (await _context.Classes.AnyAsync(c => c.Id == classId))
            {
                TempData["Error"] = $"Mã lớp '{classId}' đã tồn tại!";
                ViewBag.Major = major;
                ViewBag.MajorId = majorId;
                return View();
            }

            var newClass = new Class
            {
                Id = classId,
                Name = className ?? classId,
                MajorId = majorId,
                IsActive = true
            };

            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo lớp '{classId}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // QUAN LY CHUYEN NGANH

        // GET: Tao moi Chuyen nganh
        public async Task<IActionResult> CreateMajor(int batchId)
        {
            var batch = await _context.StudentBatches.FindAsync(batchId);
            if (batch == null) return NotFound();

            ViewBag.Batch = batch;
            ViewBag.BatchId = batchId;
            return View();
        }

        // POST: Tao moi Chuyen nganh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMajor(int batchId, string code, string name)
        {
            var batch = await _context.StudentBatches.FindAsync(batchId);
            if (batch == null) return NotFound();

            // Kiem tra ma nganh da ton tai trong khoa nay chua
            if (await _context.Majors.AnyAsync(m => m.BatchId == batchId && m.Code == code))
            {
                TempData["Error"] = $"Chuyên ngành '{code}' đã tồn tại trong khóa {batch.BatchCode}!";
                ViewBag.Batch = batch;
                ViewBag.BatchId = batchId;
                return View();
            }

            var major = new Major
            {
                Code = code,
                Name = name,
                BatchId = batchId,
                IsActive = true
            };

            _context.Majors.Add(major);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo chuyên ngành '{name}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // CHINH SUA LOP HOC

        // GET: Chinh sua Lop
        public async Task<IActionResult> EditClass(string classId)
        {
            var cls = await _context.Classes
                .Include(c => c.Major)
                    .ThenInclude(m => m!.Batch)
                .FirstOrDefaultAsync(c => c.Id == classId);
            
            if (cls == null) return NotFound();

            ViewBag.Class = cls;
            return View(cls);
        }

        // POST: Chinh sua Lop
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClass(string classId, string name)
        {
            var cls = await _context.Classes.FindAsync(classId);
            if (cls == null) return NotFound();

            cls.Name = name;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật lớp '{classId}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // XOA LOP HOC

        // GET: Xoa Lop (Xac nhan)
        public async Task<IActionResult> DeleteClass(string classId)
        {
            var cls = await _context.Classes
                .Include(c => c.Major)
                    .ThenInclude(m => m!.Batch)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == classId);
            
            if (cls == null) return NotFound();

            return View(cls);
        }

        // POST: Xoa Lop
        [HttpPost, ActionName("DeleteClass")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassConfirmed(string classId)
        {
            var cls = await _context.Classes
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == classId);
            
            if (cls == null) return NotFound();

            // Kiem tra lop co sinh vien khong
            if (cls.Students?.Any() == true)
            {
                TempData["Error"] = $"Không thể xóa lớp '{classId}' vì đang có {cls.Students.Count} sinh viên!";
                return RedirectToAction(nameof(Index));
            }

            _context.Classes.Remove(cls);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa lớp '{classId}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // AJAX ENDPOINTS

        [HttpPost]
        public async Task<IActionResult> CreateClassAjax(int majorId, string classId, string className)
        {
            try
            {
                if (await _context.Classes.AnyAsync(c => c.Id == classId))
                    return Json(new { success = false, message = $"Mã lớp '{classId}' đã tồn tại!" });

                _context.Classes.Add(new Class
                {
                    Id = classId,
                    Name = className ?? classId,
                    MajorId = majorId,
                    IsActive = true
                });
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditClassAjax(string classId, string name)
        {
            try
            {
                var cls = await _context.Classes.FindAsync(classId);
                if (cls == null) return Json(new { success = false, message = "Không tìm thấy lớp!" });

                cls.Name = name;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClassAjax(string classId)
        {
            try
            {
                var cls = await _context.Classes.Include(c => c.Students).FirstOrDefaultAsync(c => c.Id == classId);
                if (cls == null) return Json(new { success = false, message = "Không tìm thấy lớp!" });
                if (cls.Students?.Any() == true)
                    return Json(new { success = false, message = $"Không thể xóa! Lớp có {cls.Students.Count} sinh viên." });

                _context.Classes.Remove(cls);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMajorAjax(int batchId, string code, string name)
        {
            try
            {
                if (await _context.Majors.AnyAsync(m => m.BatchId == batchId && m.Code == code))
                    return Json(new { success = false, message = $"Chuyên ngành '{code}' đã tồn tại!" });

                _context.Majors.Add(new Major
                {
                    Code = code,
                    Name = name,
                    BatchId = batchId,
                    IsActive = true
                });
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // IMPORT API

        [HttpPost]
        public async Task<IActionResult> ImportPreviewAjax(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return Content("<div class='alert alert-danger'>Vui lòng chọn file Excel!</div>");

            ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");
            var preview = new Models.ImportPreviewViewModel { FileName = excelFile.FileName };

            try
            {
                var tempFileName = $"{Guid.NewGuid()}.xlsx";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
                using (var fs = new FileStream(tempPath, FileMode.Create))
                    await excelFile.CopyToAsync(fs);
                preview.TempFilePath = tempPath;

                using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    return Content("<div class='alert alert-danger'>File Excel không có worksheet!</div>");

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;
                preview.TotalRows = rowCount;

                int headerRow = -1;
                int colMSSV = -1, colHoDem = -1, colTen = -1, colGioiTinh = -1, colNgaySinh = -1, colLopHoc = -1;

                for (int row = 1; row <= Math.Min(15, rowCount); row++)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        var val = worksheet.Cells[row, col].Value?.ToString()?.Trim()?.ToLower() ?? "";
                        if (val.Contains("mã sinh viên") || val.Contains("ma sinh vien") || val == "mssv")
                        { headerRow = row; colMSSV = col; preview.HasMSSV = true; }
                        else if (val.Contains("họ đệm") || val.Contains("ho dem") || val == "họ")
                        { colHoDem = col; preview.HasHoDem = true; }
                        else if (val == "tên" || val == "ten")
                        { colTen = col; preview.HasTen = true; }
                        else if (val.Contains("giới tính") || val.Contains("gioi tinh"))
                        { colGioiTinh = col; preview.HasGioiTinh = true; }
                        else if (val.Contains("ngày sinh") || val.Contains("ngay sinh"))
                        { colNgaySinh = col; preview.HasNgaySinh = true; }
                        else if (val.Contains("lớp") || val.Contains("lop"))
                        { colLopHoc = col; preview.HasLopHoc = true; }
                    }
                    if (colMSSV > 0) break;
                }
                preview.HeaderRow = headerRow;

                if (!preview.AllRequiredColumnsFound)
                    return PartialView("_ImportPreviewPartial", preview);

                var existingCodes = await _context.Students.Select(s => s.StudentCode).ToListAsync();
                var existingClasses = await _context.Classes.Select(c => c.Id).ToListAsync();

                for (int row = headerRow + 1; row <= rowCount; row++)
                {
                    var code = worksheet.Cells[row, colMSSV].Value?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(code) || !code.All(char.IsDigit)) { preview.SkippedRows++; continue; }

                    var lastName = colHoDem > 0 ? worksheet.Cells[row, colHoDem].Value?.ToString()?.Trim() ?? "" : "";
                    var firstName = colTen > 0 ? worksheet.Cells[row, colTen].Value?.ToString()?.Trim() ?? "" : "";
                    var gender = colGioiTinh > 0 ? worksheet.Cells[row, colGioiTinh].Value?.ToString()?.Trim() ?? "" : "";
                    var dob = colNgaySinh > 0 ? worksheet.Cells[row, colNgaySinh].Value : null;
                    var classCode = colLopHoc > 0 ? worksheet.Cells[row, colLopHoc].Value?.ToString()?.Trim() ?? "" : "";

                    var dobStr = dob is DateTime dt ? dt.ToString("dd/MM/yyyy") : dob?.ToString() ?? "";

                    var item = new Models.StudentPreviewItem
                    {
                        RowNumber = row,
                        StudentCode = code,
                        LastName = lastName,
                        FirstName = firstName,
                        Gender = gender,
                        DateOfBirth = dobStr,
                        ClassCode = classCode,
                        ClassExists = string.IsNullOrEmpty(classCode) || existingClasses.Contains(classCode),
                        AlreadyExists = existingCodes.Contains(code)
                    };
                    if (item.AlreadyExists) { item.Status = "Đã tồn tại"; preview.ExistingStudents++; }
                    else if (!item.ClassExists) item.Status = "Lớp không tồn tại";
                    preview.Students.Add(item);
                }
                preview.ValidRows = preview.Students.Count;

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
            if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                return Content("<div class='alert alert-danger'>File tạm không tồn tại!</div>");

            ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");
            var results = new List<ImportResult>();
            int success = 0, skip = 0, error = 0;

            try
            {
                using var stream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
                using var package = new ExcelPackage(stream);
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null) throw new Exception("Worksheet not found");

                var rowCount = ws.Dimension?.Rows ?? 0;
                var colCount = ws.Dimension?.Columns ?? 0;

                int headerRow = -1;
                int colMSSV = -1, colHoDem = -1, colTen = -1, colGioiTinh = -1, colNgaySinh = -1, colLopHoc = -1;

                for (int row = 1; row <= Math.Min(15, rowCount); row++)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        var val = ws.Cells[row, col].Value?.ToString()?.Trim()?.ToLower() ?? "";
                        if (val.Contains("mã sinh viên") || val.Contains("ma sinh vien") || val == "mssv")
                        { headerRow = row; colMSSV = col; }
                        else if (val.Contains("họ đệm") || val.Contains("ho dem") || val == "họ") colHoDem = col;
                        else if (val == "tên" || val == "ten") colTen = col;
                        else if (val.Contains("giới tính") || val.Contains("gioi tinh")) colGioiTinh = col;
                        else if (val.Contains("ngày sinh") || val.Contains("ngay sinh")) colNgaySinh = col;
                        else if (val.Contains("lớp") || val.Contains("lop")) colLopHoc = col;
                    }
                    if (colMSSV > 0) break;
                }

                int orderNumber = 0; // Track order number for students in same class
                for (int row = headerRow + 1; row <= rowCount; row++)
                {
                    var r = new ImportResult { RowNumber = row };
                    try
                    {
                        var code = ws.Cells[row, colMSSV].Value?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(code) || !code.All(char.IsDigit)) continue;

                        var lastName = colHoDem > 0 ? ws.Cells[row, colHoDem].Value?.ToString()?.Trim() ?? "" : "";
                        var firstName = colTen > 0 ? ws.Cells[row, colTen].Value?.ToString()?.Trim() ?? "" : "";
                        var genderText = colGioiTinh > 0 ? ws.Cells[row, colGioiTinh].Value?.ToString()?.Trim() ?? "" : "";
                        var dobVal = colNgaySinh > 0 ? ws.Cells[row, colNgaySinh].Value : null;
                        var classCode = colLopHoc > 0 ? ws.Cells[row, colLopHoc].Value?.ToString()?.Trim() ?? "" : "";

                        r.StudentCode = code;
                        r.FullName = $"{lastName} {firstName}".Trim();

                        if (await _context.Students.AnyAsync(s => s.StudentCode == code))
                        { r.Status = "Bỏ qua"; r.Message = "MSSV đã tồn tại"; skip++; results.Add(r); continue; }

                        var gender = genderText.ToLower().Contains("nữ") ? Gender.Female : Gender.Male;
                        DateTime? dob = dobVal is DateTime dt ? dt : (DateTime.TryParse(dobVal?.ToString(), out var p) ? p : null);
                        string? validClass = !string.IsNullOrEmpty(classCode) && await _context.Classes.AnyAsync(c => c.Id == classCode) ? classCode : null;

                        var user = new AppUser
                        {
                            UserName = code,
                            Email = $"{code}@student.dnc.edu.vn",
                            FullName = r.FullName,
                            EmailConfirmed = true,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };

                        var createRes = await _userManager.CreateAsync(user, $"Sv@{code}");
                        if (!createRes.Succeeded)
                        { r.Status = "Lỗi"; r.Message = string.Join(", ", createRes.Errors.Select(e => e.Description)); error++; results.Add(r); continue; }

                        orderNumber++; // Increment order number for each successful import
                        await _userManager.AddToRoleAsync(user, "Student");
                        _context.Students.Add(new Core.Entities.Student
                        {
                            UserId = user.Id,
                            StudentCode = code,
                            ClassId = validClass,
                            OrderNumber = orderNumber, // Save the order from Excel file
                            Gender = gender,
                            DateOfBirth = dob
                        });
                        await _context.SaveChangesAsync();

                        r.Status = "Thành công";
                        r.Message = $"STT: {orderNumber}";
                        success++;
                    }
                    catch (Exception ex) { r.Status = "Lỗi"; r.Message = ex.Message; error++; }
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

        // ========================================
        // DELETE ALL STUDENTS IN CLASS
        // ========================================

        [HttpPost]
        public async Task<IActionResult> DeleteAllStudentsInClassAjax(string classId)
        {
            try
            {
                var students = await _context.Students
                    .Include(s => s.User)
                    .Where(s => s.ClassId == classId)
                    .ToListAsync();

                if (!students.Any())
                    return Json(new { success = false, message = "Không có sinh viên nào trong lớp này!" });

                int count = students.Count;
                
                // Delete students and their user accounts
                foreach (var student in students)
                {
                    _context.Students.Remove(student);
                    if (student.User != null)
                    {
                        await _userManager.DeleteAsync(student.User);
                    }
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========================================
        // ASSIGN LECTURER
        // ========================================

        [HttpGet]
        public async Task<IActionResult> GetAvailableLecturersAjax()
        {
            var lecturers = await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Classes)
                .Select(l => new 
                {
                    id = l.UserId,
                    name = l.User != null ? l.User.FullName : "",
                    code = l.LecturerCode,
                    count = l.Classes.Count, // Số lớp đang phụ trách
                    spec = l.Specialization
                })
                .OrderBy(l => l.name)
                .ToListAsync();
            return Json(lecturers);
        }

        [HttpPost]
        public async Task<IActionResult> AssignLecturerAjax(string classId, Guid? lecturerId)
        {
            try
            {
                var cls = await _context.Classes.FindAsync(classId);
                if (cls == null) return Json(new { success = false, message = "Không tìm thấy lớp học!" });

                cls.LecturerId = lecturerId;
                await _context.SaveChangesAsync();

                // Get updated info to return
                string lecturerName = "Chưa phân công";
                string lecturerCode = "";
                if (lecturerId.HasValue)
                {
                    var lec = await _context.Lecturers
                        .Include(l => l.User)
                        .FirstOrDefaultAsync(l => l.UserId == lecturerId.Value);
                    if (lec != null)
                    {
                        lecturerName = lec.User?.FullName ?? "";
                        lecturerCode = lec.LecturerCode;
                    }
                }

                return Json(new { success = true, lecturerName, lecturerCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class ImportResult
    {
        public int RowNumber { get; set; }
        public string StudentCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
    }

}
