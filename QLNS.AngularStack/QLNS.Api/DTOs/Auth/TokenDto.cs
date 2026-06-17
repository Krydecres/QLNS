namespace QLNS.Api.DTOs.Auth;

/// <summary>
/// Dữ liệu trả về cho Angular sau khi đăng nhập thành công.
/// Chỉ expose Username, Role, FullName - không bao giờ trả PasswordHash.
/// </summary>
public class AuthResponseDto
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

/// <summary>
/// Dữ liệu nhận từ Angular khi đăng ký tài khoản mới.
/// </summary>
public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
