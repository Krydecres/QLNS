using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Models;

/// <summary>
/// Thông tin một ngày nghỉ (có thể là ngày nghỉ lễ hoặc đơn xin nghỉ)
/// </summary>
public class DayOffItem
{
    public string Type { get; set; } = string.Empty; // "Holiday" | "Leave"
    public DateTime Date { get; set; }
    public DateTime? EndDate { get; set; }  // dành cho LeaveRequest
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BadgeClass { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel cho trang Ngày nghỉ của tôi (Task 12.3)
/// </summary>
public class MyDaysOffViewModel
{
    public List<DayOffItem> UpcomingDays { get; set; } = new();
    public List<DayOffItem> PastDays { get; set; } = new();
    public List<Holiday> Holidays { get; set; } = new();
    public List<LeaveRequest> LeaveRequests { get; set; } = new();
    public int CurrentYear { get; set; } = DateTime.Now.Year;
}
