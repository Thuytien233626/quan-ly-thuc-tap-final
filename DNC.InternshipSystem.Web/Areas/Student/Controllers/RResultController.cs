using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using QRCoder;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using iText.Layout.Properties;
using System.IO;

namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class RResultController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public RResultController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private static PdfFont GetUnicodeFont()
        {
            try
            {
                
                var fontPaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/DejaVuSans.ttf"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/arial.ttf"),
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", // Linux
                    "/System/Library/Fonts/Arial.ttf", // macOS
                    "C:\\Windows\\Fonts\\arial.ttf" // Windows system font
                };

                foreach (var fontPath in fontPaths)
                {
                    if (System.IO.File.Exists(fontPath))
                    {
                        try
                        {
                            return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                        }
                        catch
                        {
                            // Continue to next font if this one fails
                            continue;
                        }
                    }
                }
            }
            catch
            {
                // If all font loading attempts fail, fall back to standard font
            }

       
            return PdfFontFactory.CreateFont("Courier");
        }

        // GET: /Student/Result
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Tim registration cua sinh vien (moi nhat)
            var registration = await _context.Registrations
                .Include(r => r.Company)
                .Include(r => r.Lecturer)
                    .ThenInclude(l => l!.User)
                .Include(r => r.Term)
                    .ThenInclude(t => t!.Batch)
                .Where(r => r.StudentId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            // Tim diem (Grade)
            Grade? grade = null;
            if (registration != null)
            {
                grade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.RegistrationId == registration.Id);
            }

            // Dem so logbook
            int logbookCount = 0;
            int totalWeeks = 0;
            if (registration != null)
            {
                logbookCount = await _context.Logbooks
                    .CountAsync(l => l.RegistrationId == registration.Id);
                totalWeeks = registration.Term?.DurationInWeeks ?? 0;
            }
            

            ViewBag.Registration = registration;
            ViewBag.Grade = grade;
            ViewBag.LogbookCount = logbookCount;
            ViewBag.TotalWeeks = totalWeeks;
            ViewBag.HasRegistration = registration != null;

            return View();
        }
        // GET: /Student/Result/ExportTranscript
        public async Task<IActionResult> ExportTranscript()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var registration = await _context.Registrations
                .Include(r => r.Company)
                .Include(r => r.Lecturer)
                    .ThenInclude(l => l!.User)
                .Include(r => r.Term)
                .FirstOrDefaultAsync(r => r.StudentId == user.Id);

            if (registration == null) return NotFound();

            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.RegistrationId == registration.Id);

            if (grade == null) return BadRequest("Chưa có điểm.");

            double finalScore = grade.FinalScore ?? 0;

            // LINK VERIFY
            string verifyUrl = $"{Request.Scheme}://{Request.Host}/Student/Transcript/Verify/{registration.Id}";

            // QR CODE
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(verifyUrl, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);

            // LOAD FONT 
            PdfFont font = GetUnicodeFont();

            using var stream = new MemoryStream();

            PdfWriter writer = new PdfWriter(stream);
            PdfDocument pdf = new PdfDocument(writer);
            Document doc = new Document(pdf);

            doc.SetFont(font);

            doc.Add(new Paragraph("PHIẾU ĐIỂM THỰC TẬP")
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"Sinh viên: {user.FullName}"));
            doc.Add(new Paragraph($"MSSV: {user.UserName}"));

            string companyName = registration.Company?.Name ?? registration.ExternalCompanyName ?? "N/A";

            doc.Add(new Paragraph($"Doanh nghiệp: {companyName}"));

            doc.Add(new Paragraph($"Điểm doanh nghiệp: {grade.CompanyScore:F1}"));
            doc.Add(new Paragraph($"Điểm giảng viên: {grade.InstructorScore:F1}"));
            doc.Add(new Paragraph($"Điểm tổng kết: {finalScore:F1}"));

            Image qrImage = new Image(ImageDataFactory.Create(qrBytes))
                .SetWidth(120);

            doc.Add(new Paragraph("Quét QR để xác thực phiếu điểm"));
            doc.Add(qrImage);

            doc.Close();

            return File(stream.ToArray(),
                "application/pdf",
                $"PhieuDiem_{user.UserName}.pdf");
        }
    }
}
