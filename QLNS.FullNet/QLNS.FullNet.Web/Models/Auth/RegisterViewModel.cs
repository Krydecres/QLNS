using System.ComponentModel.DataAnnotations;

namespace QLNS.FullNet.Web.Models.Auth;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Tài khoản không được để trống")]
    [Display(Name = "Tài khoản")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "Employee";
}
