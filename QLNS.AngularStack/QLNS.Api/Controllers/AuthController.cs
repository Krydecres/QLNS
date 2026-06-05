using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace QLNS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _dataFile = "Data/users.json";

        public class LoginDto
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class UserDto
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
            public string FullName { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            if (!System.IO.File.Exists(_dataFile))
                return BadRequest("Database not found.");

            var json = System.IO.File.ReadAllText(_dataFile);
            var users = JsonSerializer.Deserialize<List<UserDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var user = users?.FirstOrDefault(u => u.Username.Equals(loginDto.Username, StringComparison.OrdinalIgnoreCase) && u.Password == loginDto.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });
            }

            return Ok(new { username = user.Username, role = user.Role, fullName = user.FullName });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] UserDto userDto)
        {
            if (!System.IO.File.Exists(_dataFile))
                return BadRequest("Database not found.");

            var json = System.IO.File.ReadAllText(_dataFile);
            var users = JsonSerializer.Deserialize<List<UserDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<UserDto>();

            if (users.Any(u => u.Username.Equals(userDto.Username, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { message = "Tài khoản đã tồn tại." });
            }

            users.Add(userDto);
            System.IO.File.WriteAllText(_dataFile, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));

            return Ok(new { message = "Đăng ký thành công" });
        }
    }
}
