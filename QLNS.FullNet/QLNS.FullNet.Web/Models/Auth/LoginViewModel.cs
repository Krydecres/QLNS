using System.ComponentModel.DataAnnotations;

namespace QLNS.FullNet.Web.Models.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Tài khoản không được để trống")]
    [Display(Name = "Tài khoản")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
}
