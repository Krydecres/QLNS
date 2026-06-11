using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QLNS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HolidaysController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HolidaysController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var holidays = await _context.Holidays
                .OrderBy(h => h.Month)
                .ThenBy(h => h.Day)
                .ToListAsync();
            return Ok(holidays);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound(new { message = "Không tìm thấy ngày lễ" });
            return Ok(holiday);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Holiday holiday)
        {
            try
            {
                int maxDay = DateTime.DaysInMonth(2024, holiday.Month); // 2024 is a leap year so Feb has 29
                if (holiday.Day > maxDay || holiday.Day < 1)
                {
                    return BadRequest(new { message = $"Tháng {holiday.Month} chỉ có tối đa {maxDay} ngày và tối thiểu 1 ngày." });
                }
            }
            catch
            {
                return BadRequest(new { message = "Tháng không hợp lệ." });
            }

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm ngày nghỉ lễ thành công.", holiday });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Holiday holiday)
        {
            if (id != holiday.Id) return BadRequest(new { message = "ID không khớp" });

            try
            {
                int maxDay = DateTime.DaysInMonth(2024, holiday.Month);
                if (holiday.Day > maxDay || holiday.Day < 1)
                {
                    return BadRequest(new { message = $"Tháng {holiday.Month} chỉ có tối đa {maxDay} ngày và tối thiểu 1 ngày." });
                }
            }
            catch
            {
                return BadRequest(new { message = "Tháng không hợp lệ." });
            }

            _context.Entry(holiday).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật ngày nghỉ lễ thành công." });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Holidays.AnyAsync(h => h.Id == id))
                    return NotFound(new { message = "Không tìm thấy ngày lễ" });
                else throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound(new { message = "Không tìm thấy ngày lễ" });

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa ngày nghỉ lễ thành công." });
        }
    }
}
