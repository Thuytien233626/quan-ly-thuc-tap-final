using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DNC.InternshipSystem.Infrastructure.Data.AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Identity
builder.Services.AddIdentity<DNC.InternshipSystem.Core.Entities.AppUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>>()
    .AddEntityFrameworkStores<DNC.InternshipSystem.Infrastructure.Data.AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DNC.InternshipSystem.Core.Entities.AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    // Create roles
    string[] roles = { "Admin", "Lecturer", "Student" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    // Create Admin user
    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser == null)
    {
        adminUser = new DNC.InternshipSystem.Core.Entities.AppUser
        {
            UserName = "admin",
            Email = "admin@nctu.edu.vn",
            FullName = "Quản trị viên",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
    //Create Lecturer user
    var lecturerUser = await userManager.FindByNameAsync("lecturer");
    if (lecturerUser == null)
    {
        lecturerUser = new DNC.InternshipSystem.Core.Entities.AppUser
        {
            UserName = "lecturer",
            Email = "lecturer@nctu.edu.vn",
            FullName = "Giảng viên",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(lecturerUser, "Lecturer@123");
        await userManager.AddToRoleAsync(lecturerUser, "Lecturer");
    }
    //Create Student user
    var studentUser = await userManager.FindByNameAsync("student");
    if(studentUser == null)
    {
        studentUser = new DNC.InternshipSystem.Core.Entities.AppUser
        {
            UserName = "student",
            Email = "student@nctu.edu.vn",
            FullName = "Sinh viên",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(studentUser, "Student@123");
        await userManager.AddToRoleAsync(studentUser, "Student");
    }
}

app.Run();
