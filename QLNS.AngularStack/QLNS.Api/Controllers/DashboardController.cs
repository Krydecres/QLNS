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
}