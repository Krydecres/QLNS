namespace QLNS.Api.DTOs.Timekeeping;

/// <summary>
/// Dữ liệu nhận/trả về khi thao tác với bản ghi chấm công.
/// Dùng cho cả Admin (nhập thủ công) và Employee (xem lịch sử của mình).
/// </summary>
public class TimekeepingDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }
    public string? Status { get; set; }
    public string? Note { get; set; }
    public string? ShiftName { get; set; } // Tên ca làm việc
}


/// <summary>
/// Thông tin tóm tắt User để hiển thị trong dropdown chọn nhân viên.
/// </summary>
public class UserSummaryDto
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
