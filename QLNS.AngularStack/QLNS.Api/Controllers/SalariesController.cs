using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Api.Services;
using QLNS.FullNet.Data;

namespace QLNS.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SalariesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISalaryCalculationService _salaryCalculationService;

    public SalariesController(
        AppDbContext context,
        ISalaryCalculationService salaryCalculationService)
    {
        _context = context;
        _salaryCalculationService = salaryCalculationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSalaries(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "Tháng phải từ 1 đến 12." });
        }

        var salaries = await _context.Salaries
            .Include(s => s.Employee)
            .ThenInclude(e => e!.Position)
            .Where(s => s.Month == month && s.Year == year)
            .OrderBy(s => s.Employee!.FullName)
            .Select(s => new
            {
                s.Id,
                s.EmployeeId,
                EmployeeName = s.Employee != null ? s.Employee.FullName : "",
                PositionName = s.Employee != null && s.Employee.Position != null
                    ? s.Employee.Position.Name
                    : "",
                s.Month,
                s.Year,
                s.BaseSalary,
                s.Allowance,
                s.Deduction,
                s.TotalSalary
            })
            .ToListAsync();

        return Ok(salaries);
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateAll(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "Tháng phải từ 1 đến 12." });
        }

        await _salaryCalculationService.CalculateForAllAsync(month, year);

        return Ok(new { message = "Tính lương thành công." });
    }

    [HttpPost("calculate/{employeeId}")]
    public async Task<IActionResult> CalculateOne(
        int employeeId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "Tháng phải từ 1 đến 12." });
        }

        var salary = await _salaryCalculationService.CalculateAsync(
            employeeId,
            month,
            year);

        if (salary == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy nhân viên hoặc dữ liệu không hợp lệ."
            });
        }

        return Ok(new
        {
            message = "Tính lương nhân viên thành công.",
            salary
        });
    }
}