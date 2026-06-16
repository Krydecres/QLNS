using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeShiftsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeShiftsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployeeShifts(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? employeeId,
        [FromQuery] int? shiftId)
    {
        var query = _context.EmployeeShifts
            .Include(es => es.Employee)
            .ThenInclude(e => e!.Department)
            .Include(es => es.Employee)
            .ThenInclude(e => e!.Position)
            .Include(es => es.Shift)
            .AsNoTracking()
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(es => es.WorkDate >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            var nextDate = endDate.Value.Date.AddDays(1);
            query = query.Where(es => es.WorkDate < nextDate);
        }

        if (employeeId.HasValue && employeeId.Value > 0)
        {
            query = query.Where(es => es.EmployeeId == employeeId.Value);
        }

        if (shiftId.HasValue && shiftId.Value > 0)
        {
            query = query.Where(es => es.ShiftId == shiftId.Value);
        }

        var employeeShifts = await query
            .OrderByDescending(es => es.WorkDate)
            .ThenBy(es => es.Shift!.StartTime)
            .ToListAsync();

        var result = employeeShifts.Select(es => new
        {
            es.Id,
            es.EmployeeId,
            EmployeeName = es.Employee?.FullName ?? "",
            Email = es.Employee?.Email ?? "",
            DepartmentName = es.Employee?.Department?.Name ?? "",
            PositionName = es.Employee?.Position?.Name ?? "",
            es.ShiftId,
            ShiftName = es.Shift?.Name ?? "",
            StartTime = es.Shift?.StartTime.ToString(@"hh\:mm") ?? "",
            EndTime = es.Shift?.EndTime.ToString(@"hh\:mm") ?? "",
            WageMultiplier = es.Shift?.WageMultiplier ?? 1,
            WorkDate = es.WorkDate.ToString("yyyy-MM-dd"),
            es.Note,
            es.IsActive
        });

        return Ok(result);
    }

    [HttpGet("my-shifts/{username}")]
public async Task<IActionResult> GetMyShifts(
    string username,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate)
{
    if (string.IsNullOrWhiteSpace(username))
    {
        return BadRequest(new { message = "Không tìm thấy tài khoản đăng nhập." });
    }

    var appUser = await _context.AppUsers
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Username == username);

    var employeeEmail = appUser?.Email ?? username;

    var employee = await _context.Employees
        .Include(e => e.Department)
        .Include(e => e.Position)
        .AsNoTracking()
        .FirstOrDefaultAsync(e =>
            e.Email == employeeEmail ||
            e.Email == username);

    if (employee == null)
    {
        return NotFound(new
        {
            message = "Không tìm thấy hồ sơ nhân viên tương ứng với tài khoản đăng nhập."
        });
    }

    var query = _context.EmployeeShifts
        .Include(es => es.Shift)
        .AsNoTracking()
        .Where(es => es.EmployeeId == employee.Id)
        .AsQueryable();

    if (startDate.HasValue)
    {
        query = query.Where(es => es.WorkDate >= startDate.Value.Date);
    }

    if (endDate.HasValue)
    {
        var nextDate = endDate.Value.Date.AddDays(1);
        query = query.Where(es => es.WorkDate < nextDate);
    }

    var employeeShifts = await query
        .OrderBy(es => es.WorkDate)
        .ThenBy(es => es.Shift!.StartTime)
        .ToListAsync();

    var shiftResults = employeeShifts.Select(es => new
    {
        es.Id,
        es.ShiftId,
        ShiftName = es.Shift == null ? "" : es.Shift.Name,
        StartTime = es.Shift == null ? "" : es.Shift.StartTime.ToString(@"hh\:mm"),
        EndTime = es.Shift == null ? "" : es.Shift.EndTime.ToString(@"hh\:mm"),
        BreakMinutes = es.Shift == null ? 0 : es.Shift.BreakMinutes,
        WageMultiplier = es.Shift == null ? 1 : es.Shift.WageMultiplier,
        WorkDate = es.WorkDate.ToString("yyyy-MM-dd"),
        DayOfWeek = GetVietnameseDayOfWeek(es.WorkDate.DayOfWeek),
        es.Note,
        es.IsActive
    }).ToList();

    return Ok(new
    {
        Employee = new
        {
            employee.Id,
            employee.FullName,
            employee.Email,
            employee.PhoneNumber,
            employee.AvatarUrl,
            DepartmentName = employee.Department == null ? "" : employee.Department.Name,
            PositionName = employee.Position == null ? "" : employee.Position.Name
        },
        StartDate = startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : null,
        EndDate = endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : null,
        TotalShifts = shiftResults.Count,
        ActiveShifts = shiftResults.Count(s => s.IsActive),
        Shifts = shiftResults
    });
}

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions()
    {
        var employees = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .OrderBy(e => e.FullName)
            .Select(e => new
            {
                e.Id,
                e.FullName,
                e.Email,
                DepartmentName = e.Department != null ? e.Department.Name : "",
                PositionName = e.Position != null ? e.Position.Name : ""
            })
            .ToListAsync();

        var shifts = await _context.Shifts
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.Name,
                StartTime = s.StartTime.ToString(@"hh\:mm"),
                EndTime = s.EndTime.ToString(@"hh\:mm"),
                s.WageMultiplier
            })
            .ToListAsync();

        return Ok(new
        {
            employees,
            shifts
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmployeeShift([FromBody] EmployeeShiftDto model)
    {
        var validationResult = await ValidateEmployeeShiftDto(model);

        if (validationResult != null)
        {
            return validationResult;
        }

        var hasConflict = await HasConflictAsync(
            model.EmployeeId,
            model.ShiftId,
            model.WorkDate);

        if (hasConflict)
        {
            return BadRequest(new
            {
                message = "Nhân viên đã có ca làm trùng thời gian trong ngày này."
            });
        }

        var employeeShift = new EmployeeShift
        {
            EmployeeId = model.EmployeeId,
            ShiftId = model.ShiftId,
            WorkDate = model.WorkDate.Date,
            Note = model.Note,
            IsActive = model.IsActive
        };

        _context.EmployeeShifts.Add(employeeShift);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Gán ca cho nhân viên thành công.",
            employeeShift.Id
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployeeShift(int id, [FromBody] EmployeeShiftDto model)
    {
        var employeeShift = await _context.EmployeeShifts.FindAsync(id);

        if (employeeShift == null)
        {
            return NotFound(new { message = "Không tìm thấy lịch phân ca." });
        }

        var validationResult = await ValidateEmployeeShiftDto(model);

        if (validationResult != null)
        {
            return validationResult;
        }

        var hasConflict = await HasConflictAsync(
            model.EmployeeId,
            model.ShiftId,
            model.WorkDate,
            id);

        if (hasConflict)
        {
            return BadRequest(new
            {
                message = "Nhân viên đã có ca làm trùng thời gian trong ngày này."
            });
        }

        employeeShift.EmployeeId = model.EmployeeId;
        employeeShift.ShiftId = model.ShiftId;
        employeeShift.WorkDate = model.WorkDate.Date;
        employeeShift.Note = model.Note;
        employeeShift.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật lịch phân ca thành công." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployeeShift(int id)
    {
        var employeeShift = await _context.EmployeeShifts.FindAsync(id);

        if (employeeShift == null)
        {
            return NotFound(new { message = "Không tìm thấy lịch phân ca." });
        }

        employeeShift.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã ngưng áp dụng lịch phân ca." });
    }

    private async Task<IActionResult?> ValidateEmployeeShiftDto(EmployeeShiftDto model)
    {
        if (model.EmployeeId <= 0)
        {
            return BadRequest(new { message = "Vui lòng chọn nhân viên." });
        }

        if (model.ShiftId <= 0)
        {
            return BadRequest(new { message = "Vui lòng chọn ca làm." });
        }

        var employeeExists = await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId);

        if (!employeeExists)
        {
            return NotFound(new { message = "Không tìm thấy nhân viên." });
        }

        var shiftExists = await _context.Shifts.AnyAsync(s =>
            s.Id == model.ShiftId &&
            s.IsActive);

        if (!shiftExists)
        {
            return NotFound(new { message = "Không tìm thấy ca làm đang sử dụng." });
        }

        if (model.WorkDate == default)
        {
            return BadRequest(new { message = "Vui lòng chọn ngày làm việc." });
        }

        return null;
    }

    private async Task<bool> HasConflictAsync(
        int employeeId,
        int shiftId,
        DateTime workDate,
        int? ignoreId = null)
    {
        var selectedShift = await _context.Shifts.FindAsync(shiftId);

        if (selectedShift == null)
        {
            return false;
        }

        var dateStart = workDate.Date;
        var dateEnd = dateStart.AddDays(1);

        var existingShifts = await _context.EmployeeShifts
            .Include(es => es.Shift)
            .Where(es =>
                es.EmployeeId == employeeId &&
                es.IsActive &&
                es.WorkDate >= dateStart &&
                es.WorkDate < dateEnd)
            .Where(es => !ignoreId.HasValue || es.Id != ignoreId.Value)
            .ToListAsync();

        return existingShifts.Any(es =>
            es.Shift != null &&
            selectedShift.StartTime < es.Shift.EndTime &&
            es.Shift.StartTime < selectedShift.EndTime);
    }

private static string GetVietnameseDayOfWeek(DayOfWeek dayOfWeek)
{
    return dayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ 2",
        DayOfWeek.Tuesday => "Thứ 3",
        DayOfWeek.Wednesday => "Thứ 4",
        DayOfWeek.Thursday => "Thứ 5",
        DayOfWeek.Friday => "Thứ 6",
        DayOfWeek.Saturday => "Thứ 7",
        DayOfWeek.Sunday => "Chủ nhật",
        _ => ""
    };
}
    public class EmployeeShiftDto
    {
        public int EmployeeId { get; set; }
        public int ShiftId { get; set; }
        public DateTime WorkDate { get; set; } = DateTime.Today;
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;
    }
}