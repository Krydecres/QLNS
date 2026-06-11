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
    public class LeaveRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeaveRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. LẤY DANH SÁCH XIN NGHỈ CỦA TÔI (Employee)
        // =======================================================
        [HttpGet("my-leaves/{username}")]
        public async Task<IActionResult> GetMyLeaves(string username)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            var employee = await _context.Employees.FirstOrDefaultAsync(e =>
                (!string.IsNullOrEmpty(appUser.Email) && e.Email == appUser.Email) ||
                e.Email == appUser.Username);
            if (employee == null) return NotFound(new { message = "Hồ sơ nhân viên không tồn tại." });

            var leaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employee.Id)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new {
                    l.Id,
                    l.StartDate,
                    l.EndDate,
                    l.Reason,
                    l.Status,
                    l.CreatedAt,
                    l.ApprovalNote,
                    ApprovedByName = l.ApprovedBy != null ? l.ApprovedBy.FullName : null
                })
                .ToListAsync();

            return Ok(leaves);
        }

        public class LeaveRequestCreateDto
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        // =======================================================
        // 2. TẠO MỚI ĐƠN XIN NGHỈ (Employee)
        // =======================================================
        [HttpPost("my-leaves/{username}")]
        public async Task<IActionResult> CreateLeaveRequest(string username, [FromBody] LeaveRequestCreateDto model)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            var employee = await _context.Employees.FirstOrDefaultAsync(e =>
                (!string.IsNullOrEmpty(appUser.Email) && e.Email == appUser.Email) ||
                e.Email == appUser.Username);
            if (employee == null) return NotFound(new { message = "Hồ sơ nhân viên không tồn tại." });

            if (model.EndDate < model.StartDate)
            {
                return BadRequest(new { message = "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu." });
            }

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason,
                Status = LeaveRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gửi đơn xin nghỉ phép thành công!" });
        }

        // =======================================================
        // 3. LẤY DANH SÁCH CHO QUẢN TRỊ VIÊN (Admin)
        // =======================================================
        [HttpGet("approval")]
        public async Task<IActionResult> GetApprovalList()
        {
            var requests = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.ApprovedBy)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new {
                    l.Id,
                    l.StartDate,
                    l.EndDate,
                    l.Reason,
                    l.Status,
                    l.CreatedAt,
                    l.ApprovalNote,
                    EmployeeName = l.Employee != null ? l.Employee.FullName : null,
                    ApprovedByName = l.ApprovedBy != null ? l.ApprovedBy.FullName : null
                })
                .ToListAsync();

            return Ok(requests);
        }

        public class ProcessRequestDto
        {
            public LeaveRequestStatus Status { get; set; }
            public string? Note { get; set; }
            public string AdminUsername { get; set; } = string.Empty;
        }

        // =======================================================
        // 4. DUYỆT / TỪ CHỐI ĐƠN (Admin)
        // =======================================================
        [HttpPut("approval/{id}")]
        public async Task<IActionResult> ProcessRequest(int id, [FromBody] ProcessRequestDto model)
        {
            var request = await _context.LeaveRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn xin nghỉ phép." });
            }

            if (request.Status != LeaveRequestStatus.Pending)
            {
                return BadRequest(new { message = "Đơn này đã được xử lý trước đó." });
            }

            var adminUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == model.AdminUsername);
            if (adminUser != null)
            {
                request.ApprovedById = adminUser.Id;
            }

            request.Status = model.Status;
            request.ApprovalNote = model.Note;

            await _context.SaveChangesAsync();

            string action = model.Status == LeaveRequestStatus.Approved ? "đã duyệt" : "từ chối";
            return Ok(new { message = $"Đã {action} đơn nghỉ phép." });
        }

        // =======================================================
        // 5. NGÀY NGHỈ CỦA TÔI (Employee)
        // =======================================================
        public class DayOffItemDto
        {
            public string Type { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public DateTime? EndDate { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string BadgeClass { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
        }

        public class MyDaysOffResponseDto
        {
            public System.Collections.Generic.List<DayOffItemDto> UpcomingDays { get; set; } = new System.Collections.Generic.List<DayOffItemDto>();
            public System.Collections.Generic.List<DayOffItemDto> PastDays { get; set; } = new System.Collections.Generic.List<DayOffItemDto>();
            public int CurrentYear { get; set; }
            public int TotalHolidays { get; set; }
            public int TotalLeaves { get; set; }
        }

        [HttpGet("my-days-off/{username}")]
        public async Task<IActionResult> GetMyDaysOff(string username, [FromQuery] int? year)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            var employee = await _context.Employees.FirstOrDefaultAsync(e =>
                (!string.IsNullOrEmpty(appUser.Email) && e.Email == appUser.Email) ||
                e.Email == appUser.Username);
            if (employee == null) return NotFound(new { message = "Hồ sơ nhân viên không tồn tại." });

            int selectedYear = year ?? DateTime.Now.Year;
            var today = DateTime.Today;

            var holidays = await _context.Holidays
                .Where(h => h.Month >= 1 && h.Month <= 12 && h.Day >= 1 && h.Day <= 31)
                .OrderBy(h => h.Month).ThenBy(h => h.Day)
                .ToListAsync();

            var leaveRequests = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employee.Id
                    && l.Status == LeaveRequestStatus.Approved
                    && (l.StartDate.Year == selectedYear || l.EndDate.Year == selectedYear))
                .OrderBy(l => l.StartDate)
                .ToListAsync();

            var allDays = new System.Collections.Generic.List<DayOffItemDto>();

            foreach (var h in holidays)
            {
                try {
                    int daysInMonth = DateTime.DaysInMonth(selectedYear, h.Month);
                    int day = h.Day > daysInMonth ? daysInMonth : h.Day;
                    var d = new DateTime(selectedYear, h.Month, day);
                    allDays.Add(new DayOffItemDto
                    {
                        Type = "Holiday",
                        Date = d,
                        Title = h.Name,
                        Description = h.Description,
                        BadgeClass = "bg-danger",
                        Icon = "bi-star-fill"
                    });
                } catch { }
            }

            foreach (var lr in leaveRequests)
            {
                allDays.Add(new DayOffItemDto
                {
                    Type = "Leave",
                    Date = lr.StartDate,
                    EndDate = lr.EndDate,
                    Title = "Nghỉ phép: " + lr.Reason,
                    Description = lr.ApprovalNote,
                    BadgeClass = "bg-success",
                    Icon = "bi-calendar-check"
                });
            }

            var upcoming = allDays
                .Where(d => d.Date >= today)
                .OrderBy(d => d.Date)
                .ToList();

            var past = allDays
                .Where(d => d.Date < today)
                .OrderByDescending(d => d.Date)
                .ToList();

            var response = new MyDaysOffResponseDto
            {
                UpcomingDays = upcoming,
                PastDays = past,
                CurrentYear = selectedYear,
                TotalHolidays = holidays.Count,
                TotalLeaves = leaveRequests.Count
            };

            return Ok(response);
        }
    }
}
