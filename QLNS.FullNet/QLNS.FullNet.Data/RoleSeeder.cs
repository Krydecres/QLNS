using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Data;

public static class RoleSeeder
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";

    public const string AdminOnlyPolicy = "AdminOnly";
    public const string EmployeeOnlyPolicy = "EmployeeOnly";
    public const string StaffPolicy = "Staff";

    public static async Task SeedAsync(AppDbContext context, string? adminPasswordHash = null)
    {
        var hasChange = false;

        // Nếu cấu hình mật khẩu admin thì tự tạo tài khoản admin mặc định
        if (!string.IsNullOrWhiteSpace(adminPasswordHash))
        {
            var hasAdmin = await context.AppUsers.AnyAsync(u => u.Role == Admin);

            if (!hasAdmin)
            {
                context.AppUsers.Add(new AppUser
                {
                    Username = "admin",
                    FullName = "Quản trị hệ thống",
                    Email = "admin@qlns.local",
                    Role = Admin,
                    PasswordHash = adminPasswordHash
                });

                hasChange = true;
            }
        }

        // Chuẩn hóa role bị rỗng hoặc sai
        var invalidUsers = await context.AppUsers
            .Where(u => u.Role != Admin && u.Role != Employee)
            .ToListAsync();

        foreach (var user in invalidUsers)
        {
            user.Role = Employee;
            hasChange = true;
        }

        if (hasChange)
        {
            await context.SaveChangesAsync();
        }
    }
}