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
        public DbSet<Class> Classes { get; set; }
        public DbSet<InternshipTerm> InternshipTerms { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Logbook> Logbooks { get; set; }
        public DbSet<Grade> Grades { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Entity Relationships (Fluent API)
            // Example configuration if needed, otherwise convention works for most.
            
            // Student Key is UserId (1-1 with AppUser)
            builder.Entity<Student>()
                .HasKey(s => s.UserId);
                
            builder.Entity<Lecturer>()
                .HasKey(l => l.UserId);

            builder.Entity<Grade>()
                .HasKey(g => g.RegistrationId);
                
            builder.Entity<Grade>()
                 .Property(g => g.FinalScore)
                 .HasComputedColumnSql("(IsNull(CompanyScore,0)*0.4 + IsNull(InstructorScore,0)*0.3 + IsNull(ReportScore,0)*0.3)");
        }
    }
}
