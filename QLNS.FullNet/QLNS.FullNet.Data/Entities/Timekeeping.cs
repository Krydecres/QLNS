using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.FullNet.Data.Entities;

public class Timekeeping
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Nhân viên")]
    public int EmployeeId { get; set; }

    [ForeignKey("EmployeeId")]
    public Employee? Employee { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày")]
    public DateTime Date { get; set; }

    [Display(Name = "Gi៝ vào (Check-in)")]
    public TimeSpan? CheckInTime { get; set; }

    [Display(Name = "Giờ ra (Check-out)")]
    public TimeSpan? CheckOutTime { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500)]
    public string? Note { get; set; }
}