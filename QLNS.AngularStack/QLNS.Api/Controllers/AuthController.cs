using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Api.Data;
using QLNS.Api.DTOs.Auth;
using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == loginDto.Username);
            if (user == null)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });
            }

            var passwordHasher = new PasswordHasher<AppUser>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);

            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return Ok(new AuthResponseDto
                {
                    Username = user.Username,
                    Role = user.Role,
                    FullName = user.FullName
                });
            }

            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto userDto)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Username == userDto.Username);
            if (userExists)
            {
                return BadRequest(new { message = "Tài khoản đã tồn tại." });
            }

            var passwordHasher = new PasswordHasher<AppUser>();
            var user = new AppUser
            {
                Username = userDto.Username,
                FullName = userDto.FullName,
                Email = userDto.Email,
                Role = string.IsNullOrWhiteSpace(userDto.Role) ? "Employee" : userDto.Role
            };
            user.PasswordHash = passwordHasher.HashPassword(user, userDto.Password);

            _context.AppUsers.Add(user);

            if (user.Role == "Employee")
            {
                var employee = new Employee
                {
                    FullName = user.FullName,
                    Email = user.Email
                };
                _context.Employees.Add(employee);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công" });
        }
    }
}
