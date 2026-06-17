namespace QLNS.Api.DTOs.EmployeeShift;

/// <summary>
/// Dữ liệu nhận từ Angular khi Admin tạo mới hoặc cập nhật lịch phân ca.
/// </summary>
public class EmployeeShiftDto
{
    public int EmployeeId { get; set; }
    public int ShiftId { get; set; }
    public DateTime WorkDate { get; set; } = DateTime.Today;
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
}
