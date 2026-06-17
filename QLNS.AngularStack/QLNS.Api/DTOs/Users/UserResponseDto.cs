namespace QLNS.Api.DTOs.Users;

/// <summary>
/// Dữ liệu trả về cho Angular khi liệt kê/xem thông tin tài khoản.
/// Không bao giờ trả PasswordHash.
/// </summary>
public class UserResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Dữ liệu nhận từ Angular khi Admin thay đổi Role của một tài khoản.
/// </summary>
public class UpdateRoleDto
{
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Dữ liệu nhận từ Angular khi Admin reset mật khẩu cho một tài khoản.
/// </summary>
public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}
