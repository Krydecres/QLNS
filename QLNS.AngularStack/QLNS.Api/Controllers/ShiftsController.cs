using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Api.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ShiftsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShiftsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetShifts([FromQuery] string? search)
    {
        var query = _context.Shifts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Name.Contains(search) ||
                (s.Description != null && s.Description.Contains(search)));
        }

        var shifts = await query
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.Name,
                StartTime = s.StartTime.ToString(@"hh\:mm"),
                EndTime = s.EndTime.ToString(@"hh\:mm"),
                s.BreakMinutes,
                s.Description,
                s.WageMultiplier,
                s.IsActive
            })
            .ToListAsync();

        return Ok(shifts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShift(int id)
    {
        var shift = await _context.Shifts
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.Name,
                StartTime = s.StartTime.ToString(@"hh\:mm"),
                EndTime = s.EndTime.ToString(@"hh\:mm"),
                s.BreakMinutes,
                s.Description,
                s.WageMultiplier,
                s.IsActive
            })
            .FirstOrDefaultAsync();

        if (shift == null)
        {
            return NotFound(new { message = "Không tìm thấy ca làm." });
        }

        return Ok(shift);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShift([FromBody] ShiftDto model)
    {
        var validationResult = ValidateShiftDto(model, out var startTime, out var endTime);

        if (validationResult != null)
        {
            return validationResult;
        }

        var shift = new Shift
        {
            Name = model.Name.Trim(),
            StartTime = startTime,
            EndTime = endTime,
            BreakMinutes = model.BreakMinutes,
            Description = model.Description,
            WageMultiplier = model.WageMultiplier,
            IsActive = model.IsActive
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Tạo ca làm thành công.",
            shift.Id
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShift(int id, [FromBody] ShiftDto model)
    {
        var shift = await _context.Shifts.FindAsync(id);

        if (shift == null)
        {
            return NotFound(new { message = "Không tìm thấy ca làm." });
        }

        var validationResult = ValidateShiftDto(model, out var startTime, out var endTime);

        if (validationResult != null)
        {
            return validationResult;
        }

        shift.Name = model.Name.Trim();
        shift.StartTime = startTime;
        shift.EndTime = endTime;
        shift.BreakMinutes = model.BreakMinutes;
        shift.Description = model.Description;
        shift.WageMultiplier = model.WageMultiplier;
        shift.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật ca làm thành công." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShift(int id)
    {
        var shift = await _context.Shifts.FindAsync(id);

        if (shift == null)
        {
            return NotFound(new { message = "Không tìm thấy ca làm." });
        }

        shift.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã ngưng sử dụng ca làm." });
    }

    private IActionResult? ValidateShiftDto(
        ShiftDto model,
        out TimeSpan startTime,
        out TimeSpan endTime)
    {
        startTime = TimeSpan.Zero;
        endTime = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return BadRequest(new { message = "Tên ca làm không được để trống." });
        }

        if (!TimeSpan.TryParse(model.StartTime, out startTime))
        {
            return BadRequest(new { message = "Giờ bắt đầu không hợp lệ." });
        }

        if (!TimeSpan.TryParse(model.EndTime, out endTime))
        {
            return BadRequest(new { message = "Giờ kết thúc không hợp lệ." });
        }

        if (startTime == endTime)
        {
            return BadRequest(new { message = "Giờ bắt đầu và giờ kết thúc không được trùng nhau." });
        }

        if (model.BreakMinutes < 0 || model.BreakMinutes > 240)
        {
            return BadRequest(new { message = "Thời gian nghỉ phải từ 0 đến 240 phút." });
        }

        if (model.WageMultiplier <= 0)
        {
            return BadRequest(new { message = "Hệ số lương phải lớn hơn 0." });
        }

        return null;
    }

    public class ShiftDto
    {
        public string Name { get; set; } = string.Empty;
        public string StartTime { get; set; } = "08:00";
        public string EndTime { get; set; } = "17:00";
        public int BreakMinutes { get; set; }
        public string? Description { get; set; }
        public decimal WageMultiplier { get; set; } = 1.0m;
        public bool IsActive { get; set; } = true;
    }
}