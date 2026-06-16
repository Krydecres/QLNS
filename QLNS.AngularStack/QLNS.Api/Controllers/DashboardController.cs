using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var currentMonth = today.Month;
        var currentYear = today.Year;

        var totalEmployees = await _context.Employees.CountAsync();
        var totalDepartments = await _context.Departments.CountAsync();
        var totalPositions = await _context.Positions.CountAsync();

        var presentToday = await _context.Timekeepings
            .Where(t =>
                t.Date >= today &&
                t.Date < tomorrow &&
                t.CheckInTime != null)
            .Select(t => t.EmployeeId)
            .Distinct()
            .CountAsync();

        var approvedLeaveToday = await _context.LeaveRequests
            .Where(l =>
                l.Status == LeaveRequestStatus.Approved &&
                l.StartDate.Date <= today &&
                l.EndDate.Date >= today)
            .Select(l => l.EmployeeId)
            .Distinct()
            .CountAsync();

        var pendingLeaveRequests = await _context.LeaveRequests
            .CountAsync(l => l.Status == LeaveRequestStatus.Pending);

        var notCheckedInToday = totalEmployees - presentToday - approvedLeaveToday;

        if (notCheckedInToday < 0)
        {
            notCheckedInToday = 0;
        }

        var attendanceRate = totalEmployees == 0
            ? 0
            : Math.Round((decimal)presentToday / totalEmployees * 100, 1);

        var totalSalaryThisMonth = await _context.Salaries
            .Where(s => s.Month == currentMonth && s.Year == currentYear)
            .SumAsync(s => (decimal?)s.TotalSalary) ?? 0;

        var recentAttendance = new List<object>();

        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var nextDay = day.AddDays(1);

            var count = await _context.Timekeepings
                .Where(t =>
                    t.Date >= day &&
                    t.Date < nextDay &&
                    t.CheckInTime != null)
                .Select(t => t.EmployeeId)
                .Distinct()
                .CountAsync();

            recentAttendance.Add(new
            {
                Date = day.ToString("dd/MM"),
                Count = count
            });
        }

        var employees = await _context.Employees
            .Include(e => e.Department)
            .AsNoTracking()
            .ToListAsync();

        var employeesByDepartment = employees
            .GroupBy(e => e.Department != null ? e.Department.Name : "Chưa có phòng ban")
            .Select(g => new
            {
                DepartmentName = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var leaveSummary = new
        {
            Pending = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveRequestStatus.Pending),
            Approved = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveRequestStatus.Approved),
            Rejected = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveRequestStatus.Rejected)
        };

        return Ok(new
        {
            TotalEmployees = totalEmployees,
            TotalDepartments = totalDepartments,
            TotalPositions = totalPositions,
            PresentToday = presentToday,
            ApprovedLeaveToday = approvedLeaveToday,
            NotCheckedInToday = notCheckedInToday,
            AttendanceRate = attendanceRate,
            PendingLeaveRequests = pendingLeaveRequests,
            TotalSalaryThisMonth = totalSalaryThisMonth,
            CurrentMonth = currentMonth,
            CurrentYear = currentYear,
            RecentAttendance = recentAttendance,
            EmployeesByDepartment = employeesByDepartment,
            LeaveSummary = leaveSummary,
            UpdatedAt = DateTime.Now
        });
    }

    [HttpGet("employee/{username}")]
    public async Task<IActionResult> GetEmployeeDashboard(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { message = "Username không được để trống." });
        }

        var appUser = await _context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (appUser == null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản người dùng." });
        }

        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                (!string.IsNullOrEmpty(appUser.Email) && e.Email == appUser.Email) ||
                e.Email == appUser.Username);

        if (employee == null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ nhân viên." });
        }

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var currentMonth = today.Month;
        var currentYear = today.Year;

        var monthStart = new DateTime(currentYear, currentMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var workingDayTarget = Enumerable.Range(1, DateTime.DaysInMonth(currentYear, currentMonth))
            .Select(day => new DateTime(currentYear, currentMonth, day))
            .Count(day => day.DayOfWeek != DayOfWeek.Sunday);

        var workDaysThisMonth = await _context.Timekeepings
            .Where(t =>
                t.EmployeeId == employee.Id &&
                t.Date >= monthStart &&
                t.Date <= monthEnd &&
                t.CheckInTime != null)
            .Select(t => t.Date.Date)
            .Distinct()
            .CountAsync();

        var salaryThisMonth = await _context.Salaries
            .Where(s =>
                s.EmployeeId == employee.Id &&
                s.Month == currentMonth &&
                s.Year == currentYear)
            .Select(s => new
            {
                s.BaseSalary,
                s.Allowance,
                s.Deduction,
                s.TotalSalary
            })
            .FirstOrDefaultAsync();

        var todayAttendance = await _context.Timekeepings
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.EmployeeId == employee.Id &&
                t.Date >= today &&
                t.Date < tomorrow);

        var todayShift = await _context.EmployeeShifts
            .Include(es => es.Shift)
            .AsNoTracking()
            .Where(es =>
                es.EmployeeId == employee.Id &&
                es.IsActive &&
                es.WorkDate.Date == today)
            .OrderBy(es => es.Shift != null ? es.Shift.StartTime : TimeSpan.Zero)
            .Select(es => new
            {
                ShiftName = es.Shift != null ? es.Shift.Name : "Chưa có ca",
                StartTime = es.Shift != null ? es.Shift.StartTime.ToString(@"hh\:mm") : "",
                EndTime = es.Shift != null ? es.Shift.EndTime.ToString(@"hh\:mm") : "",
                es.Note
            })
            .FirstOrDefaultAsync();

        var pendingLeaves = await _context.LeaveRequests
            .CountAsync(l => l.EmployeeId == employee.Id && l.Status == LeaveRequestStatus.Pending);

        var approvedLeaves = await _context.LeaveRequests
            .CountAsync(l => l.EmployeeId == employee.Id && l.Status == LeaveRequestStatus.Approved);

        var rejectedLeaves = await _context.LeaveRequests
            .CountAsync(l => l.EmployeeId == employee.Id && l.Status == LeaveRequestStatus.Rejected);

        var recentAttendance = await _context.Timekeepings
            .Where(t => t.EmployeeId == employee.Id)
            .OrderByDescending(t => t.Date)
            .Take(7)
            .Select(t => new
            {
                Date = t.Date.ToString("dd/MM/yyyy"),
                CheckInTime = t.CheckInTime != null ? t.CheckInTime.Value.ToString(@"hh\:mm") : "-",
                CheckOutTime = t.CheckOutTime != null ? t.CheckOutTime.Value.ToString(@"hh\:mm") : "-",
                Status = t.Status ?? "Có mặt",
                Note = t.Note
            })
            .ToListAsync();

        var upcomingLeaves = await _context.LeaveRequests
            .Where(l =>
                l.EmployeeId == employee.Id &&
                l.Status == LeaveRequestStatus.Approved &&
                l.EndDate.Date >= today)
            .OrderBy(l => l.StartDate)
            .Take(3)
            .Select(l => new
            {
                StartDate = l.StartDate.ToString("dd/MM/yyyy"),
                EndDate = l.EndDate.ToString("dd/MM/yyyy"),
                l.Reason
            })
            .ToListAsync();

        var attendanceStatus = "Chưa chấm công";

        if (todayAttendance?.CheckInTime != null && todayAttendance.CheckOutTime == null)
        {
            attendanceStatus = "Đã check-in";
        }
        else if (todayAttendance?.CheckInTime != null && todayAttendance.CheckOutTime != null)
        {
            attendanceStatus = "Đã hoàn thành";
        }

        return Ok(new
        {
            Employee = new
            {
                employee.Id,
                employee.FullName,
                employee.Email,
                employee.PhoneNumber,
                employee.AvatarUrl,
                DepartmentName = employee.Department != null ? employee.Department.Name : "Chưa có phòng ban",
                PositionName = employee.Position != null ? employee.Position.Name : "Chưa có chức vụ"
            },
            CurrentMonth = currentMonth,
            CurrentYear = currentYear,
            WorkDaysThisMonth = workDaysThisMonth,
            WorkingDayTarget = workingDayTarget,
            AttendanceStatus = attendanceStatus,
            TodayCheckInTime = todayAttendance?.CheckInTime != null
                ? todayAttendance.CheckInTime.Value.ToString(@"hh\:mm")
                : "-",
            TodayCheckOutTime = todayAttendance?.CheckOutTime != null
                ? todayAttendance.CheckOutTime.Value.ToString(@"hh\:mm")
                : "-",
            TodayShift = todayShift,
            SalaryThisMonth = salaryThisMonth,
            LeaveSummary = new
            {
                Pending = pendingLeaves,
                Approved = approvedLeaves,
                Rejected = rejectedLeaves
            },
            RecentAttendance = recentAttendance,
            UpcomingLeaves = upcomingLeaves,
            UpdatedAt = DateTime.Now
        });
    }
}