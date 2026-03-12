using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DNC.InternshipSystem.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // Tu dong tao va cap nhat database theo Migrations
            context.Database.Migrate();

            // 1. Seed Users (Admin & Giang vien)
            // Quan tri vien
            if (await userManager.FindByNameAsync("admin") == null)
            {
                var admin = new AppUser
                {
                    UserName = "admin",
                    Email = "admin@nctu.edu.vn",
                    FullName = "Quản trị viên",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Danh sach Giang vien (UserName = Email de login bang email)
            var lecturers = new[]
            {
                new { Name = "Ts. Ngô Hồ Anh Khôi", Email = "nhakhoi@nctu.edu.vn", Rank = "Tiến sĩ" },
                new { Name = "Ths. Bùi Thị Diễm Trinh", Email = "btdtrinh@nctu.edu.vn", Rank = "Thạc sĩ" },
                new { Name = "Ths. Đặng Mạnh Huy", Email = "dmhuy@nctu.edu.vn", Rank = "Thạc sĩ" },
                new { Name = "Ths. Đoàn Hòa Minh", Email = "dhminh@nctu.edu.vn", Rank = "Thạc sĩ" }
            };

            foreach (var l in lecturers)
            {
// Kiểm tra theo email (vì login dùng email)
var normalizedEmail = l.Email.Trim().ToUpper();
var existingUser = await userManager.Users
    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

if (existingUser == null)
{
    var user = new AppUser
    {
        UserName = l.Email,  // Dùng email làm username
        Email = l.Email,
        FullName = l.Name,
        EmailConfirmed = true
    };
                    var result = await userManager.CreateAsync(user, "Giangvien@123");
                    
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"✓ Tạo Giảng viên: {l.Email} (UserName={user.UserName})");
                        await userManager.AddToRoleAsync(user, "Lecturer");

                        // Tao profile Giang vien
                        if (!await context.Lecturers.AnyAsync(lec => lec.UserId == user.Id))
                        {
                            var lecturerCount = await context.Lecturers.CountAsync();
                            context.Lecturers.Add(new Lecturer
                            {
                                UserId = user.Id,
                                LecturerCode = $"GV{(lecturerCount + 1):D3}",
                                DepartmentId = 1, // Khoa mac dinh (CNTT)
                                AcademicRank = l.Rank,
                                Specialization = "Công nghệ Thông tin"
                            });
                            await context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"✗ Lỗi tạo GV {l.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"~ GV {l.Email} đã tồn tại (UserName={existingUser.UserName})");
                }
            }

            // 2. Seed Companies (Danh sach goi y doanh nghiep)
            if (!await context.Companies.AnyAsync())
            {
                var companies = new List<Company>
                {
                    new Company { Name = "Mobifone An Giang", Address = "93 Trần Hưng Đạo, phường Mỹ Quý, TP Long Xuyên", Province = "An Giang", PhoneNumber = "0779249999", Status = CompanyStatus.Approved },
                    new Company { Name = "Ngân hàng Nông nghiệp và Phát triển Nông thôn Agribank", Address = "51B Tôn Đức Thắng, phường Mỹ Bình, TP Long Xuyên", Province = "An Giang", PhoneNumber = "02963954845", Status = CompanyStatus.Approved },
                    new Company { Name = "Viễn thông An Giang (VNPT An Giang)", Address = "02 Lê Lợi, phường Mỹ Bình, TP Long Xuyên", Province = "An Giang", PhoneNumber = "18001166", Status = CompanyStatus.Approved },
                    new Company { Name = "Viễn Thông Bạc Liêu", Address = "99 Nguyễn Văn Linh, phường 1, TP Bạc Liêu", Province = "Bạc Liêu", PhoneNumber = "02913827561", Status = CompanyStatus.Approved },
                    new Company { Name = "Viettel Cà Mau", Address = "Số 298, đường Trần Hưng Đạo, phường 5, TP Cà Mau", Province = "Cà Mau", PhoneNumber = "02906276088", Status = CompanyStatus.Approved },
                    new Company { Name = "Viễn Thông Cà Mau (Trung tâm CNTT)", Address = "Số 3 Lưu Tấn Tài, phường 5, TP Cà Mau", Province = "Cà Mau", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH MTV Công nghệ Kỹ thuật Tiên Phong", Address = "Quận Cái Răng", Province = "Cần Thơ", ContactPerson = "Ngô Hồ Anh Khôi (Giám đốc)", PhoneNumber = "0916416409", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty Silicon Stack", Address = "Cần Thơ", Province = "Cần Thơ", ContactEmail = "jobs.vn@siliconstack.com.au", Status = CompanyStatus.Approved },
                    new Company { Name = "Trung Tâm CNTT-VNPT Cần Thơ", Address = "11 Phan Đình Phùng, phường Tân An, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Thương mại Dịch vụ Appcore", Address = "Số 86 Nguyễn Trãi, phường Cái Khế, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Công nghệ phần mềm May", Address = "I12 Nguyễn Ngọc Trai, phường Xuân Khánh, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty cổ phần Trí Tuệ Đồng Bằng", Address = "515/86, đường 30/4, phường Hưng Lợi, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty Điện lực Thành phố Cần Thơ", Address = "6 Nguyễn Trãi, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Trường Cao đẳng FPT Cần Thơ", Address = "288 Nguyễn Văn Linh, phường An Khánh, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Trung tâm Công nghệ thông tin và Truyền Thông", Address = "Số 29, Cách Mạng Tháng 8, phường Thới Bình, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Viettel Cái Răng", Address = "Phú An, phường Phú Thứ, quận Cái Răng", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Tin học Á Châu", Address = "41 Lý Tự Trọng, phường An Phú, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Phần mềm & Dịch vụ CNTT LIINK", Address = "A9-6 đường số 2, KDC Nam Long, phường Thường Thạnh, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công Ty TNHH TMDV Thời Số", Address = "Số 190/13, hẻm 534, đường 30/4, phường Hưng Lợi, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Công nghệ NHONHO", Address = "K2-17, đường Võ Nguyên Giáp, phường Phú Thứ, quận Cái Răng", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Dịch vụ xử lý số FPT - CN Cần Thơ", Address = "48 Cách Mạng Tháng 8, phường An Thới, quận Bình Thủy", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "FSoft Cần Thơ", Address = "600 Nguyễn Văn Cừ nối dài, phường An Bình, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Phần mềm FPT (Chi nhánh Cần Thơ)", Address = "Tầng 4, Đại học FPT, số 600 Nguyễn Văn Cừ, phường An Bình, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty Dịch vụ Mobifone Khu vực 9", Address = "Toà nhà Mobifone, đường số 22, khu Công ty Xây dựng 8, phường Hưng Thạnh, quận Cái Răng", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Trung tâm Công nghệ thông tin và Truyền thông TPCT", Address = "29 Cách Mạng Tháng 8, phường Thới Bình, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công Ty TNHH Thương Mại Dịch Vụ Nhất Ngôn Phát", Address = "342/15 KTT Thủy Lợi, đường Tầm Vu, phường Hưng Lợi, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Trung tâm Thông tin Khoa học và Công nghệ Cần Thơ", Address = "118/3 Trần Phú, phường Cái Khế, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Trung tâm Công nghệ Phần mềm - ĐH Duy Tân", Address = "24 Trần Văn Trà, Hưng Phú 1, phường Hưng Phú, quận Cái Răng", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Mobifone thành phố Cần Thơ", Address = "199 Võ Văn Kiệt, phường An Thới, quận Bình Thuỷ", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Axon Active Viet Nam", Address = "Tầng 3 Tòa nhà Toyota Ninh Kiều, 57-59A Cách Mạng Tháng 8, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Giải pháp Công nghệ và Truyền thông BIGWALK", Address = "68 đường B2, KDC 91B, phường An Khánh, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Đài Phát thanh và Truyền hình thành phố Cần Thơ", Address = "409 đường 30/4, phường Hưng Lợi, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH IVS", Address = "Tầng 6, phòng 605, Trung Tâm Dịch vụ Việc làm TP.Cần Thơ, số 160 Đường 30/04, phường An Phú, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Trung tâm CNTT - VNPT Cần Thơ", Address = "Số 11 Phan Đình Phùng, phường Tân An, quận Ninh Kiều", Province = "Cần Thơ", Status = CompanyStatus.Approved },
                    new Company { Name = "Trung tâm Công nghệ phần mềm - ĐH Cần Thơ", Address = "01 Lý Tự Trọng, quận Ninh Kiều", Province = "Cần Thơ", ContactPerson = "Huỳnh Xuân Trúc", ContactEmail = "hxtruc@ctu.edu.vn", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty Cổ phần MKS", Address = "187AA Nguyễn Văn Cừ, phường An Bình, quận Ninh Kiều", Province = "Cần Thơ", ContactPerson = "Trần Văn Thiện (Chủ tịch HĐQT)", PhoneNumber = "0919755866", Status = CompanyStatus.Approved },
                    new Company { Name = "VNPT Hậu Giang", Address = "số 61, đường Võ Văn Kiệt, khu vực 4, phường 5, TP Vị Thanh", Province = "Hậu Giang", Status = CompanyStatus.Approved },
                    new Company { Name = "Văn phòng HĐND và UBND Thị xã Long Mỹ", Address = "KV Bình Thạnh B, phường Bình Thạnh, thị xã Long Mỹ", Province = "Hậu Giang", ContactPerson = "Trần Út Nhã", PhoneNumber = "0912758324", Status = CompanyStatus.Approved },
                    new Company { Name = "Văn phòng HĐND và UBND huyện Long Mỹ", Address = "Ấp 1, thị trấn Vĩnh Viễn, huyện Long Mỹ", Province = "Hậu Giang", ContactPerson = "Trần Hồng Hoa (Chánh Văn phòng)", PhoneNumber = "0949010199", Status = CompanyStatus.Approved },
                    new Company { Name = "Phòng Giáo dục và Đào tạo huyện Long Mỹ", Address = "Ấp 1, thị trấn Vĩnh Viễn, huyện Long Mỹ", Province = "Hậu Giang", ContactPerson = "Đặng Thanh Ty (Phó Trưởng phòng)", PhoneNumber = "0985323336", Status = CompanyStatus.Approved },
                    new Company { Name = "Đài Truyền thanh huyện Long Mỹ", Address = "Ấp 1, thị trấn Vĩnh Viễn, huyện Long Mỹ", Province = "Hậu Giang", ContactPerson = "Nguyễn Minh Cường (Phó Trưởng đài)", PhoneNumber = "0919438535", Status = CompanyStatus.Approved },
                    new Company { Name = "Phòng Văn hóa và Thông tin huyện Long Mỹ", Address = "Ấp 1, thị trấn Vĩnh Viễn, huyện Long Mỹ", Province = "Hậu Giang", ContactPerson = "Nguyễn Tâm (Phó Trưởng phòng)", PhoneNumber = "0939282881", Status = CompanyStatus.Approved },
                    new Company { Name = "Phòng Kinh tế và Hạ tầng huyện Long Mỹ", Address = "Ấp 1, thị trấn Vĩnh Viễn, huyện Long Mỹ", Province = "Hậu Giang", ContactPerson = "Lê Phú Hải (Phó Trưởng phòng)", PhoneNumber = "0901278678", Status = CompanyStatus.Approved },
                    new Company { Name = "BHXH huyện Châu Thành", Address = "Ấp Thị Trấn, thị trấn Ngã Sáu, huyện Châu Thành", Province = "Hậu Giang", ContactPerson = "Nguyễn Thành Nam", PhoneNumber = "0949256955", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH IVS (HCM)", Address = "180-182 Lý Chính Thắng, Phường 9, Quận 3", Province = "Hồ Chí Minh", Status = CompanyStatus.Approved },
                    new Company { Name = "Tập đoàn Công nghệ TMA", Address = "84A/5 Trần Hữu Trang, quận Phú Nhuận (#Office 4)", Province = "Hồ Chí Minh", Status = CompanyStatus.Approved },
                    new Company { Name = "KMS Technology Việt Nam", Address = "2 Tản Viên, phường 2, quận Tân Bình", Province = "Hồ Chí Minh", Status = CompanyStatus.Approved },
                    new Company { Name = "Viễn Thông Kiên Giang", Address = "25 Điện Biên Phủ, phường Vĩnh Quang, TP Rạch Giá", Province = "Kiên Giang", Status = CompanyStatus.Approved },
                    new Company { Name = "Viễn Thông Sóc Trăng", Address = "02 Trần Hưng Đạo, phường 2, TP Sóc Trăng", Province = "Sóc Trăng", Status = CompanyStatus.Approved },
                    new Company { Name = "Trung tâm VNPT - IT Khu vực 5", Address = "ấp Phong Thuận, xã Tân Mỹ Chánh, TP Mỹ Tho", Province = "Tiền Giang", Status = CompanyStatus.Approved },
                    new Company { Name = "Viễn Thông Tiền Giang - Công viên Phần mềm Mekong", Address = "Số 1 Lê Lợi - Phường 1 - TP Mỹ Tho", Province = "Tiền Giang", ContactPerson = "Võ Thị Ý (Trưởng Phòng Nhân sự)", PhoneNumber = "0914709595", Status = CompanyStatus.Approved },
                    new Company { Name = "Công ty TNHH Công nghệ phần mềm Phúc Lam Phương", Address = "M66 Đinh Tiên Hoàng, phường 8, TP Vĩnh Long", Province = "Vĩnh Long", ContactPerson = "Võ Văn Phúc (Giám đốc)", PhoneNumber = "0909141661", Status = CompanyStatus.Approved },
                };
                context.Companies.AddRange(companies);
                await context.SaveChangesAsync();
            }

        }
    }
}
