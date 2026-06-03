using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

    builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleSeeder.AdminOnlyPolicy, policy =>
        policy.RequireRole(RoleSeeder.Admin)); // Chỉ Admin mới có quyền truy cập

    options.AddPolicy(RoleSeeder.EmployeeOnlyPolicy, policy =>
        policy.RequireRole(RoleSeeder.Employee)); // Chỉ Employee mới có quyền truy cập

    options.AddPolicy(RoleSeeder.StaffPolicy, policy => // Admin và Employee đều có quyền truy cập
        policy.RequireRole(RoleSeeder.Admin, RoleSeeder.Employee));
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
//Nếu có cấu hình SeedAdmin:Password thì tự tạo tài khoản admin mặc định.
//Nếu không có thì chỉ chuẩn hóa role sai/rỗng.
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var adminPassword = builder.Configuration["SeedAdmin:Password"];

        string? adminPasswordHash = null;

        if (!string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminUser = new AppUser
            {
                Username = "admin",
                Role = RoleSeeder.Admin
            };

            var passwordHasher = new PasswordHasher<AppUser>();
            adminPasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);
        }

        await RoleSeeder.SeedAsync(context, adminPasswordHash);
    }
}
catch (Exception ex)
{
    Console.WriteLine("Không thể seed Roles & Policy do chưa kết nối được database.");
    Console.WriteLine(ex.Message);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

app.Run();
