using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;

namespace QLNS.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SalarySettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SalarySettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var positions = await _context.Positions
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.DailyWage
            })
            .ToListAsync();

        var shifts = await _context.Shifts
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.StartTime,
                s.EndTime,
                s.WageMultiplier,
                s.IsActive
            })
            .ToListAsync();

        return Ok(new
        {
            positions,
            shifts
        });
    }

    [HttpPut("positions/{id}/daily-wage")]
    public async Task<IActionResult> UpdatePositionDailyWage(
        int id,
        [FromBody] UpdateDailyWageDto model)
    {
        if (model.DailyWage < 0)
        {
            return BadRequest(new
            {
                message = "Lương ngày công không được âm."
            });
        }

        var position = await _context.Positions.FindAsync(id);

        if (position == null)
        {
            return NotFound(new { message = "Không tìm thấy chức vụ." });
        }

        position.DailyWage = model.DailyWage;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật lương ngày công thành công."
        });
    }

    [HttpPut("shifts/{id}/wage-multiplier")]
    public async Task<IActionResult> UpdateShiftWageMultiplier(
        int id,
        [FromBody] UpdateWageMultiplierDto model)
    {
        if (model.WageMultiplier <= 0)
        {
            return BadRequest(new
            {
                message = "Hệ số lương phải lớn hơn 0."
            });
        }

        var shift = await _context.Shifts.FindAsync(id);

        if (shift == null)
        {
            return NotFound(new { message = "Không tìm thấy ca làm." });
        }

        shift.WageMultiplier = model.WageMultiplier;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật hệ số lương ca làm thành công."
        });
    }

    public class UpdateDailyWageDto
    {
        public decimal DailyWage { get; set; }
    }

    public class UpdateWageMultiplierDto
    {
        public decimal WageMultiplier { get; set; }
    }
}