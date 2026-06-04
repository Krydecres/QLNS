using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.FullNet.Data.Entities;

public class EmployeeShift
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhân viên.")]
    [Display(Name = "Nhân viên")]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ca làm.")]
    [Display(Name = "Ca làm")]
    public int ShiftId { get; set; }

    [ForeignKey(nameof(ShiftId))]
    public Shift? Shift { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày làm việc.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày làm việc")]
    public DateTime WorkDate { get; set; } = DateTime.Today;

    [StringLength(255)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Display(Name = "Đang áp dụng")]
    public bool IsActive { get; set; } = true;
}