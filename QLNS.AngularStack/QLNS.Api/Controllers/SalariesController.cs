using ClosedXML.Excel;
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
    private readonly IEmailService _emailService;

    public SalariesController(
        AppDbContext context,
        ISalaryCalculationService salaryCalculationService,
        IEmailService emailService)
    {
        _context = context;
        _salaryCalculationService = salaryCalculationService;
        _emailService = emailService;
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

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearSalaries(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "Tháng phải từ 1 đến 12." });
        }

        var salaries = await _context.Salaries
            .Where(s => s.Month == month && s.Year == year)
            .ToListAsync();

        if (salaries.Any())
        {
            _context.Salaries.RemoveRange(salaries);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Xóa dữ liệu bảng lương thành công." });
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

    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel(
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

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("BangLuong");

        worksheet.Cell(1, 1).Value = "BÁO CÁO BẢNG LƯƠNG CHI TIẾT";
        worksheet.Range(1, 1, 1, 10).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(2, 1).Value = $"Tháng {month}/{year}";
        worksheet.Range(2, 1, 2, 10).Merge();
        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(4, 1).Value = "STT";
        worksheet.Cell(4, 2).Value = "Mã nhân viên";
        worksheet.Cell(4, 3).Value = "Họ tên";
        worksheet.Cell(4, 4).Value = "Chức vụ";
        worksheet.Cell(4, 5).Value = "Tháng";
        worksheet.Cell(4, 6).Value = "Năm";
        worksheet.Cell(4, 7).Value = "Lương công";
        worksheet.Cell(4, 8).Value = "Phụ cấp";
        worksheet.Cell(4, 9).Value = "Khấu trừ";
        worksheet.Cell(4, 10).Value = "Tổng lương";

        var headerRange = worksheet.Range(4, 1, 4, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var row = 5;
        var index = 1;

        foreach (var item in salaries)
        {
            worksheet.Cell(row, 1).Value = index;
            worksheet.Cell(row, 2).Value = item.EmployeeId;
            worksheet.Cell(row, 3).Value = item.EmployeeName;
            worksheet.Cell(row, 4).Value = item.PositionName;
            worksheet.Cell(row, 5).Value = item.Month;
            worksheet.Cell(row, 6).Value = item.Year;
            worksheet.Cell(row, 7).Value = item.BaseSalary;
            worksheet.Cell(row, 8).Value = item.Allowance;
            worksheet.Cell(row, 9).Value = item.Deduction;
            worksheet.Cell(row, 10).Value = item.TotalSalary;

            row++;
            index++;
        }

        if (salaries.Count > 0)
        {
            var dataRange = worksheet.Range(5, 1, row - 1, 10);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.Range(5, 7, row - 1, 10)
                .Style.NumberFormat.Format = "#,##0";
        }

        worksheet.Cell(row + 1, 9).Value = "Tổng cộng:";
        worksheet.Cell(row + 1, 9).Style.Font.Bold = true;

        worksheet.Cell(row + 1, 10).Value = salaries.Sum(s => s.TotalSalary);
        worksheet.Cell(row + 1, 10).Style.Font.Bold = true;
        worksheet.Cell(row + 1, 10).Style.NumberFormat.Format = "#,##0";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"BangLuong_{month}_{year}.xlsx";

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost("bulk-send-email")]
    public async Task<IActionResult> BulkSendEmail(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "Tháng phải từ 1 đến 12." });
        }

        var salaries = await _context.Salaries
            .Include(s => s.Employee)
            .Where(s => s.Month == month && s.Year == year)
            .ToListAsync();

        if (salaries.Count == 0)
        {
            return BadRequest(new { message = "Không có dữ liệu lương trong tháng này để gửi." });
        }

        int successCount = 0;
        foreach (var s in salaries)
        {
            if (s.Employee != null && !string.IsNullOrEmpty(s.Employee.Email))
            {
                var subject = $"Thông báo lương tháng {month}/{year}";
                var body = $@"
                    <h3>Xin chào {s.Employee.FullName},</h3>
                    <p>Dưới đây là kết quả tính lương tháng {month}/{year} của bạn:</p>
                    <ul>
                        <li>Lương cơ bản: {s.BaseSalary:N0} VNĐ</li>
                        <li>Phụ cấp: {s.Allowance:N0} VNĐ</li>
                        <li>Khấu trừ: {s.Deduction:N0} VNĐ</li>
                        <li><strong>Tổng lương nhận: {s.TotalSalary:N0} VNĐ</strong></li>
                    </ul>
                    <p>Trân trọng,</p>
                    <p>Phòng Nhân sự</p>";

                try
                {
                    await _emailService.SendEmailAsync(s.Employee.Email, subject, body);
                    successCount++;
                }
                catch (Exception ex)
                {
                    // Log error if needed
                    Console.WriteLine(ex.Message);
                }
            }
        }

        return Ok(new { message = $"Đã gửi thành công {successCount}/{salaries.Count} email." });
    }
}