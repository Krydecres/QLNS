using System.ComponentModel.DataAnnotations;

namespace QLNS.FullNet.Data.Entities;


/*
Name          → tên ca, ví dụ: Ca sáng, Ca chiều
StartTime     → giờ bắt đầu
EndTime       → giờ kết thúc
BreakMinutes  → số phút nghỉ giữa ca
Description   → mô tả thêm
IsActive      → ca còn sử dụng hay không */
public class Shift
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên ca làm không được để trống.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giờ bắt đầu không được để trống.")]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "Giờ kết thúc không được để trống.")]
    public TimeSpan EndTime { get; set; }

    [Range(0, 240, ErrorMessage = "Thời gian nghỉ phải từ 0 đến 240 phút.")]
    public int BreakMinutes { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    [Display(Name = "Hệ số lương (Tăng ca)")]
    public decimal WageMultiplier { get; set; } = 1.0m;

    public bool IsActive { get; set; } = true;
}