namespace QLNS.Api.DTOs.Employee;

/// <summary>
/// Dữ liệu trả về cho Angular khi xem chi tiết một nhân viên.
/// Chỉ expose các trường hiển thị, không trả toàn bộ Entity.
/// </summary>
public class EmployeeDetailDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
}
