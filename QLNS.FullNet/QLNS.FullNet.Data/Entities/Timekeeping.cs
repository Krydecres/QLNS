using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.FullNet.Data.Entities
{
    public class Timekeeping
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nhân viên")]
        public int EmployeeId { get; set; }
        
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [Display(Name = "Ngày chấm công")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Giờ vào")]
        public TimeSpan? CheckInTime { get; set; }

        [Display(Name = "Giờ ra")]
        public TimeSpan? CheckOutTime { get; set; }

        [Display(Name = "Trạng thái")]
        [MaxLength(50)]
        public string Status { get; set; } = "Present"; // Present, Absent, Late, HalfDay

        [Display(Name = "Ghi chú")]
        [MaxLength(250)]
        public string? Note { get; set; }
    }
}