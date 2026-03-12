using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DNC.InternshipSystem.Web.Services;
var builder = WebApplication.CreateBuilder(args);

// Them cac dich vu vao container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// Ban quyen EPPlus
OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("DNC-IMS");

// Them Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DNC.InternshipSystem.Infrastructure.Data.AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.UseCompatibilityLevel(120)));
// Dang ky cac dich vu nghiep vu (Service Layer)
builder.Services.AddHttpClient();
builder.Services.AddScoped<AIMentorService>();
builder.Services.AddScoped<DNC.InternshipSystem.Core.Interfaces.IRegistrationService, DNC.InternshipSystem.Web.Services.RegistrationService>();
builder.Services.AddScoped<DNC.InternshipSystem.Core.Interfaces.IGradingService, DNC.InternshipSystem.Web.Services.GradingService>();
builder.Services.AddScoped<DNC.InternshipSystem.Core.Interfaces.ILogbookService, DNC.InternshipSystem.Web.Services.LogbookService>();
builder.Services.AddScoped<DNC.InternshipSystem.Core.Interfaces.IAllocationService, DNC.InternshipSystem.Web.Services.AllocationService>();
builder.Services.AddScoped<DNC.InternshipSystem.Core.Interfaces.ICompanyService, DNC.InternshipSystem.Web.Services.CompanyService>();
// Them Identity
builder.Services.AddIdentity<DNC.InternshipSystem.Core.Entities.AppUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>>()
    .AddEntityFrameworkStores<DNC.InternshipSystem.Infrastructure.Data.AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Cau hinh HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "admin-area",
    pattern: "{area:exists}/{controller=AdminHome}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "lecturer-area",
    pattern: "{area:exists}/{controller=LecturerHome}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "student-area",
    pattern: "{area:exists}/{controller}/{action}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
// Viec seed du lieu da duoc chuyen sang AppDbContext.cs (OnModelCreating)
using (var scope = app.Services.CreateScope())
{
    // Chay Seed Data (Runtime)
    try 
    {
        await DNC.InternshipSystem.Infrastructure.Data.DbInitializer.Initialize(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Co loi xay ra khi seed du lieu vao database.");
    }
}

app.Run();
