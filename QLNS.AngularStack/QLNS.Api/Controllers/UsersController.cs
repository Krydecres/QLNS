using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.AppUsers
            .OrderBy(u => u.Username)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(int id, UpdateRoleDto dto)
    {
        if (dto.Role != "Admin" && dto.Role != "Employee")
        {
            return BadRequest(new { message = "Role không hợp lệ. Chỉ được chọn Admin hoặc Employee." });
        }

        var user = await _context.AppUsers.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản." });
        }

        user.Role = dto.Role;
        await _context.SaveChangesAsync();

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });
        }

        var user = await _context.AppUsers.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản." });
        }

        var passwordHasher = new PasswordHasher<AppUser>();
        user.PasswordHash = passwordHasher.HashPassword(user, dto.NewPassword);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Reset mật khẩu thành công." });
    }
}

public class UserResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateRoleDto
{
    public string Role { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}