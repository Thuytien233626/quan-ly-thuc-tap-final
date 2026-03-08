using DNC.InternshipSystem.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace DNC.InternshipSystem.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            base.OnConfiguring(optionsBuilder);
        }
        public DbSet<Class> Classes { get; set; }
        public DbSet<InternshipTerm> InternshipTerms { get; set; }
        public DbSet<StudentBatch> StudentBatches { get; set; }
        public DbSet<Major> Majors { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Logbook> Logbooks { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Submission> Submissions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Cau hinh Quan he (Fluent API)
            
            // Sinh vien: PK = UserId, FK den AppUser (1-1)
            builder.Entity<Student>()
                .HasKey(s => s.UserId);
            
            builder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .IsRequired();

            // Giang vien: PK = UserId, FK den AppUser (1-1)
            builder.Entity<Lecturer>()
                .HasKey(l => l.UserId);

            builder.Entity<Lecturer>()
                .HasOne(l => l.User)
                .WithOne(u => u.Lecturer)
                .HasForeignKey<Lecturer>(l => l.UserId)
                .IsRequired();

            builder.Entity<Grade>(entity =>
            {
                entity.HasKey(g => g.RegistrationId);

                entity.HasOne(g => g.Registration)
                    .WithOne()
                    .HasForeignKey<Grade>(g => g.RegistrationId)
                    .IsRequired();

                entity.Property(g => g.FinalScore)
                    .HasComputedColumnSql("(IsNull(CompanyScore,0)*0.4 + IsNull(InstructorScore,0)*0.6)");
            });

            // ============================================================
            // 2. SEED DATA (Du lieu mau)
            // ============================================================
            
            // a. Tao cac Roles (Vai tro) mac dinh
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var lecturerRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var studentRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            builder.Entity<IdentityRole<Guid>>().HasData(
                new IdentityRole<Guid> { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "fixed-stamp" },
                new IdentityRole<Guid> { Id = lecturerRoleId, Name = "Lecturer", NormalizedName = "LECTURER", ConcurrencyStamp = "fixed-stamp" },
                new IdentityRole<Guid> { Id = studentRoleId, Name = "Student", NormalizedName = "STUDENT", ConcurrencyStamp = "fixed-stamp" }
            );

            // b. Tao Khoa Cong nghe thong tin
            builder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Khoa Công Nghệ Thông Tin", Code = "CNTT", EstablishedYear = 2013 }
            );

            // c. Seed Khoa hoc (StudentBatch)
            builder.Entity<StudentBatch>().HasData(
                new StudentBatch { Id = 1, BatchCode = "K9", ClassCode = "DH21", EnrollmentYear = 2021, GraduationYear = 2025, IsActive = true },
                new StudentBatch { Id = 2, BatchCode = "K10", ClassCode = "DH22", EnrollmentYear = 2022, GraduationYear = 2026, IsActive = true },
                new StudentBatch { Id = 3, BatchCode = "K11", ClassCode = "DH23", EnrollmentYear = 2023, GraduationYear = 2027, IsActive = true }
            );

            // d. Cau hinh quan he InternshipTerm -> StudentBatch
            builder.Entity<InternshipTerm>()
                .HasOne(t => t.Batch)
                .WithMany(b => b.InternshipTerms)
                .HasForeignKey(t => t.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
            // e. Cau hinh quan he Major -> StudentBatch
            builder.Entity<Major>()
                .HasOne(m => m.Batch)
                .WithMany(b => b.Majors)
                .HasForeignKey(m => m.BatchId)
                .OnDelete(DeleteBehavior.Restrict);

            // f. Cau hinh quan he Class -> Major
            builder.Entity<Class>()
                .HasOne(c => c.Major)
                .WithMany(m => m.Classes)
                .HasForeignKey(c => c.MajorId)
                .OnDelete(DeleteBehavior.Restrict);

            // g. Cau hinh quan he Student -> Class
            builder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // h. Seed Major (Chuyen nganh) cho tung Khoa
            builder.Entity<Major>().HasData(
                // K9 Majors
                new Major { Id = 1, Code = "KTPM", Name = "Kỹ thuật Phần mềm", BatchId = 1, IsActive = true },
                new Major { Id = 2, Code = "CNTT", Name = "Công nghệ Thông tin", BatchId = 1, IsActive = true },
                new Major { Id = 3, Code = "KHMT", Name = "Khoa học Máy tính", BatchId = 1, IsActive = true },
                // K10 Majors
                new Major { Id = 4, Code = "KTPM", Name = "Kỹ thuật Phần mềm", BatchId = 2, IsActive = true },
                new Major { Id = 5, Code = "CNTT", Name = "Công nghệ Thông tin", BatchId = 2, IsActive = true },
                // K11 Majors
                new Major { Id = 6, Code = "KTPM", Name = "Kỹ thuật Phần mềm", BatchId = 3, IsActive = true },
                new Major { Id = 7, Code = "CNTT", Name = "Công nghệ Thông tin", BatchId = 3, IsActive = true },
                new Major { Id = 8, Code = "KHMT", Name = "Khoa học Máy tính", BatchId = 3, IsActive = true }
            );

            // i. User & Lecturer Seeding chuyen sang DbInitializer (Runtime) de tranh loi PasswordHash khi Migration.
        }
    }
}
