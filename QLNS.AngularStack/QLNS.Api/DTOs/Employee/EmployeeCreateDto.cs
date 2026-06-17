namespace QLNS.Api.DTOs.Employee;

/// <summary>
/// Dữ liệu nhận từ Angular khi Admin tạo mới nhân viên.
/// </summary>
public class EmployeeCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public decimal BaseSalary { get; set; } = 5000000;
    public decimal Allowance { get; set; } = 0;
}
