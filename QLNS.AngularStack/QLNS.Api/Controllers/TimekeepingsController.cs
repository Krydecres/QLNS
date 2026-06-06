using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace QLNS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimekeepingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TimekeepingsController(AppDbContext context)
        {
            _context = context;
        }

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
        }

        public class UserDto
        {
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
        }

        public class CheckInDto
        {
            public string? Username { get; set; } // If admin checking in for someone
        }

        // =======================================================
        // 1. GET ALL (Cho Admin)
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.Timekeepings
                .Include(t => t.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= endOfDay);
            }

            var timekeepings = await query.OrderByDescending(t => t.Date).ToListAsync();

            var result = timekeepings.Select(t => new TimekeepingDto
            {
                Id = t.Id,
                Username = "", // Not strictly needed here, or can be fetched if needed
                FullName = t.Employee?.FullName ?? "N/A",
                Email = t.Employee?.Email ?? "",
                Date = t.Date,
                CheckInTime = t.CheckInTime?.ToString(@"hh\:mm"),
                CheckOutTime = t.CheckOutTime?.ToString(@"hh\:mm"),
                Status = t.Status,
                Note = t.Note
            }).ToList();

            return Ok(result);
        }

        // =======================================================
        // 2. CHẤM CÔNG CỦA TÔI (Employee)
        // =======================================================
        [HttpGet("{username}")]
        public async Task<IActionResult> GetMyAttendance(string username, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null) return NotFound(new { message = "User not found" });

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == appUser.Email);
            if (employee == null) return NotFound(new { message = "Employee profile not found" });

            var query = _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id)
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= endOfDay);
            }

            var timekeepings = await query.OrderByDescending(t => t.Date).ToListAsync();

            var result = timekeepings.Select(t => new TimekeepingDto
            {
                Id = t.Id,
                Username = username,
                FullName = employee.FullName,
                Date = t.Date,
                CheckInTime = t.CheckInTime?.ToString(@"hh\:mm"),
                CheckOutTime = t.CheckOutTime?.ToString(@"hh\:mm"),
                Status = t.Status,
                Note = t.Note
            }).ToList();

            return Ok(result);
        }

        // =======================================================
        // 3. NHẬP CÔNG THỦ CÔNG (Admin)
        // =======================================================
        [HttpPost("manual-entry")]
        public async Task<IActionResult> ManualEntry([FromBody] TimekeepingDto model)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (appUser == null) return NotFound(new { message = "User not found" });

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == appUser.Email);
            if (employee == null) return NotFound(new { message = "Employee profile not found" });

            var existing = await _context.Timekeepings
                .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Date.Date == model.Date.Date);

            TimeSpan? checkIn = string.IsNullOrEmpty(model.CheckInTime) ? null : TimeSpan.Parse(model.CheckInTime);
            TimeSpan? checkOut = string.IsNullOrEmpty(model.CheckOutTime) ? null : TimeSpan.Parse(model.CheckOutTime);

            if (existing != null)
            {
                existing.CheckInTime = checkIn;
                existing.CheckOutTime = checkOut;
                existing.Status = model.Status ?? "Có mặt";
                existing.Note = model.Note;
                _context.Update(existing);
            }
            else
            {
                var newRecord = new Timekeeping
                {
                    EmployeeId = employee.Id,
                    Date = model.Date,
                    CheckInTime = checkIn,
                    CheckOutTime = checkOut,
                    Status = model.Status ?? "Có mặt",
                    Note = model.Note
                };
                _context.Timekeepings.Add(newRecord);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Lưu chấm công thành công" });
        }

        // =======================================================
        // 4. LẤY DANH SÁCH USER ĐỂ CHỌN
        // =======================================================
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.AppUsers
                .Where(u => u.Role == "Employee" || u.Role == "Admin") // Filter if needed
                .Select(u => new UserDto
                {
                    Username = u.Username,
                    FullName = u.FullName
                }).ToListAsync();

            return Ok(users);
        }

        // =======================================================
        // 5. CHECK IN
        // =======================================================
        [HttpPost("check-in/{username}")]
        public async Task<IActionResult> CheckIn(string username)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null) return NotFound(new { message = "User not found" });

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == appUser.Email);
            if (employee == null) return NotFound(new { message = "Employee profile not found" });

            var today = DateTime.Today;
            var record = await _context.Timekeepings
                .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Date.Date == today);

            if (record != null)
            {
                return BadRequest(new { message = $"Nhân viên '{employee.FullName}' đã được chấm công hôm nay rồi." });
            }

            _context.Timekeepings.Add(new Timekeeping
            {
                EmployeeId = employee.Id,
                Date = DateTime.Now,
                CheckInTime = DateTime.Now.TimeOfDay,
                Status = "Có mặt"
            });
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Check-in cho '{employee.FullName}' thành công lúc {DateTime.Now:HH:mm}." });
        }

        // =======================================================
        // 6. CHECK OUT
        // =======================================================
        [HttpPost("check-out/{username}")]
        public async Task<IActionResult> CheckOut(string username)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null) return NotFound(new { message = "User not found" });

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == appUser.Email);
            if (employee == null) return NotFound(new { message = "Employee profile not found" });

            var today = DateTime.Today;
            var record = await _context.Timekeepings
                .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Date.Date == today);

            if (record != null && record.CheckInTime != null)
            {
                if (record.CheckOutTime == null)
                {
                    record.CheckOutTime = DateTime.Now.TimeOfDay;
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Check-out thành công!" });
                }
                else
                {
                    return BadRequest(new { message = "Bạn đã check-out hôm nay rồi." });
                }
            }
            
            return BadRequest(new { message = "Bạn chưa check-in hôm nay." });
        }

        // =======================================================
        // 7. EXPORT EXCEL
        // =======================================================
        [HttpGet("export")]
        public async Task<IActionResult> ExportExcel([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.Timekeepings
                .Include(t => t.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= endOfDay);
            }

            var timekeepings = await query.OrderByDescending(t => t.Date).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("DuLieuChamCong");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "ID Chấm Công";
                worksheet.Cell(currentRow, 2).Value = "Tên Nhân Viên";
                worksheet.Cell(currentRow, 3).Value = "Ngày Chấm Công";
                worksheet.Cell(currentRow, 4).Value = "Giờ Vào";
                worksheet.Cell(currentRow, 5).Value = "Giờ Ra";
                worksheet.Cell(currentRow, 6).Value = "Trạng Thái";
                worksheet.Cell(currentRow, 7).Value = "Ghi Chú";
                worksheet.Row(currentRow).Style.Font.Bold = true;

                foreach (var item in timekeepings)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.Id;
                    worksheet.Cell(currentRow, 2).Value = item.Employee?.FullName ?? "N/A";
                    worksheet.Cell(currentRow, 3).Value = item.Date.ToString("dd/MM/yyyy");
                    worksheet.Cell(currentRow, 4).Value = item.CheckInTime.HasValue ? item.CheckInTime.Value.ToString(@"hh\:mm") : "-";
                    worksheet.Cell(currentRow, 5).Value = item.CheckOutTime.HasValue ? item.CheckOutTime.Value.ToString(@"hh\:mm") : "-";
                    worksheet.Cell(currentRow, 6).Value = item.Status ?? "Có mặt";
                    worksheet.Cell(currentRow, 7).Value = item.Note;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "DuLieuChamCong.xlsx"
                    );
                }
            }
        }
    }
}
