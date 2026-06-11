using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.FullNet.Data.Entities
{
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Phòng ban")]
        public int? DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        [Display(Name = "Chức vụ")]
        public int? PositionId { get; set; }
        [ForeignKey("PositionId")]
        public Position? Position { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Lương cơ bản")]
        public decimal BaseSalary { get; set; } = 5000000;

        [Display(Name = "Phụ cấp")]
        public decimal Allowance { get; set; } = 0;
    }
}