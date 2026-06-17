using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.DTOs.LeaveRequest;

/// <summary>
/// Dữ liệu nhận từ Angular khi Employee tạo đơn xin nghỉ phép.
/// </summary>
public class CreateLeaveRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Dữ liệu nhận từ Angular khi Admin duyệt hoặc từ chối đơn nghỉ phép.
/// </summary>
public class ProcessRequestDto
{
    public LeaveRequestStatus Status { get; set; }
    public string? Note { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
}

/// <summary>
/// Một ngày nghỉ (holiday hoặc leave) trong danh sách "Ngày nghỉ của tôi".
/// </summary>
public class DayOffItemDto
{
    public string Type { get; set; } = string.Empty; // "Holiday" hoac "Leave"
    public DateTime Date { get; set; }
    public DateTime? EndDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BadgeClass { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Response trả về cho Angular ở endpoint "Ngày nghỉ của tôi".
/// </summary>
public class MyDaysOffResponseDto
{
    public List<DayOffItemDto> UpcomingDays { get; set; } = new();
    public List<DayOffItemDto> PastDays { get; set; } = new();
    public int CurrentYear { get; set; }
    public int TotalHolidays { get; set; }
    public int TotalLeaves { get; set; }
}
