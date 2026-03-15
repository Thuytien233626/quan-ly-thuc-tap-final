using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public SettingsController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.AdminUser = user;

            // Thong ke DB
            var dbStats = new
            {
                StudentCount = await _context.Students.CountAsync(),
                LecturerCount = await _context.Lecturers.CountAsync(),
                CompanyCount = await _context.Companies.CountAsync(),
                RegistrationCount = await _context.Registrations.CountAsync(),
                LogbookCount = await _context.Logbooks.CountAsync()
            };
            ViewBag.DbStats = dbStats;

            // Kiem tra file backup
            var backupDir = Path.Combine(_env.WebRootPath, "..", "Backups");
            var backupFiles = new List<dynamic>();
            if (Directory.Exists(backupDir))
            {
                backupFiles = Directory.GetFiles(backupDir, "*.bak")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => (dynamic)new
                    {
                        Name = f.Name,
                        Size = (f.Length / 1024.0 / 1024.0).ToString("F2") + " MB",
                        Date = f.CreationTime.ToString("dd/MM/yyyy HH:mm")
                    })
                    .ToList();
            }
            ViewBag.BackupFiles = backupFiles;

            return View();
        }

        // POST: Doi mat khau
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới không khớp.";
                return RedirectToAction("Index");
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Đổi mật khẩu thành công!";
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Đổi mật khẩu thất bại: {errors}";
            }

            return RedirectToAction("Index");
        }

        // POST: Sao luu DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackupDatabase()
        {
            try
            {
                var backupDir = Path.Combine(_env.WebRootPath, "..", "Backups");
                Directory.CreateDirectory(backupDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"DNC_Backup_{timestamp}.bak";
                var filePath = Path.Combine(backupDir, fileName);

                var connStr = _configuration.GetConnectionString("DefaultConnection");
                var builder = new SqlConnectionStringBuilder(connStr);
                var dbName = builder.InitialCatalog;

                var sql = $"BACKUP DATABASE [{dbName}] TO DISK = '{filePath}' WITH FORMAT, INIT, NAME = 'DNC Backup {timestamp}'";

                using var connection = new SqlConnection(connStr);
                await connection.OpenAsync();
                using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = 120;
                await command.ExecuteNonQueryAsync();

                TempData["Success"] = $"Sao lưu thành công: {fileName}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Sao lưu thất bại: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: Khoi phuc DB tu file upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreDatabase(IFormFile backupFile)
        {
            if (backupFile == null || backupFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file backup (.bak).";
                return RedirectToAction("Index");
            }

            if (!backupFile.FileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Chỉ chấp nhận file .bak.";
                return RedirectToAction("Index");
            }

            try
            {
                var backupDir = Path.Combine(_env.WebRootPath, "..", "Backups");
                Directory.CreateDirectory(backupDir);
                var filePath = Path.Combine(backupDir, backupFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await backupFile.CopyToAsync(stream);
                }

                var connStr = _configuration.GetConnectionString("DefaultConnection");
                var builder = new SqlConnectionStringBuilder(connStr);
                var dbName = builder.InitialCatalog;

                // Chuyen sang master de restore
                builder.InitialCatalog = "master";
                var masterConnStr = builder.ConnectionString;

                var sql = $@"
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    RESTORE DATABASE [{dbName}] FROM DISK = '{filePath}' WITH REPLACE;
                    ALTER DATABASE [{dbName}] SET MULTI_USER;";

                using var connection = new SqlConnection(masterConnStr);
                await connection.OpenAsync();
                using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = 300;
                await command.ExecuteNonQueryAsync();

                TempData["Success"] = "Khôi phục database thành công! Vui lòng đăng nhập lại.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Khôi phục thất bại: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // GET: Tai file backup
        public IActionResult DownloadBackup(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return NotFound();

            var backupDir = Path.Combine(_env.WebRootPath, "..", "Backups");
            var filePath = Path.Combine(backupDir, fileName);

            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/octet-stream", fileName);
        }

        // POST: Xoa file backup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBackup(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["Error"] = "Tên file không hợp lệ.";
                return RedirectToAction("Index");
            }

            var backupDir = Path.Combine(_env.WebRootPath, "..", "Backups");
            var filePath = Path.Combine(backupDir, fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                TempData["Success"] = $"Đã xóa file {fileName}.";
            }
            else
            {
                TempData["Error"] = "File không tồn tại.";
            }

            return RedirectToAction("Index");
        }
    }
}
