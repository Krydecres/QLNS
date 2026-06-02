using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.FullNet.Data.Entities
{
    public enum LeaveRequestStatus
    {
        [Display(Name = "Chờ duyệt")]
        Pending = 0,
        
        [Display(Name = "Đã duyệt")]
        Approved = 1,
        
        [Display(Name = "Đã từ chối")]
        Rejected = 2
    }

    public class LeaveRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nhân viên không được để trống")]
        [Display(Name = "Nhân viên")]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Lý do không được để trống")]
        [Display(Name = "Lý do")]
        public string Reason { get; set; } = string.Empty;

        [Display(Name = "Trạng thái")]
        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Người duyệt")]
        public int? ApprovedById { get; set; }

        [ForeignKey("ApprovedById")]
        public AppUser? ApprovedBy { get; set; }

        [Display(Name = "Ghi chú duyệt")]
        public string? ApprovalNote { get; set; }
    }
}
