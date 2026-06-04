using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using QLNS.FullNet.Web.Models;
using System.Security.Claims;

namespace QLNS.FullNet.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Employee"))
        {
            return RedirectToAction(nameof(EmployeeDashboard));
        }

        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var now = DateTime.Now;
        var today = now.Date;
        var firstDayThisMonth = new DateTime(now.Year, now.Month, 1);

        var prevMonth = now.Month == 1 ? 12 : now.Month - 1;
        var prevYear  = now.Month == 1 ? now.Year - 1 : now.Year;

        var vm = new AdminDashboardViewModel();

        // ── Nhân sự ──────────────────────────────────────────────
        vm.TotalEmployees   = await _context.Employees.CountAsync();
        vm.TotalDepartments = await _context.Departments.CountAsync();
        vm.TotalPositions   = await _context.Positions.CountAsync();

        // ── Chấm công hôm nay ────────────────────────────────────
        var todayTimekeepings = await _context.Timekeepings
            .Where(t => t.Date.Date == today)
            .ToListAsync();

        vm.PresentToday    = todayTimekeepings.Count(t => t.Status == "Có mặt");
        vm.AbsentToday     = todayTimekeepings.Count(t => t.Status == "Vắng có phép");
        vm.NotCheckedToday = vm.TotalEmployees - todayTimekeepings.Count;

        // ── Đơn từ chờ duyệt ─────────────────────────────────────
        vm.PendingLeaveRequests  = await _context.LeaveRequests.CountAsync(r => r.Status == LeaveRequestStatus.Pending);
        vm.PendingProfileUpdates = await _context.ProfileUpdateRequests.CountAsync(r => r.Status == "Pending");

        // ── Lương ─────────────────────────────────────────────────
        var salariesThisMonth = await _context.Salaries
            .Where(s => s.Month == now.Month && s.Year == now.Year)
            .ToListAsync();

        vm.SalaryCalculatedCount = salariesThisMonth.Count;
        vm.TotalSalaryThisMonth  = salariesThisMonth.Sum(s => s.TotalSalary);

        vm.TotalSalaryLastMonth = await _context.Salaries
            .Where(s => s.Month == prevMonth && s.Year == prevYear)
            .SumAsync(s => s.TotalSalary);

        // ── Biểu đồ lương 6 tháng ────────────────────────────────
        for (int i = 5; i >= 0; i--)
        {
            var d   = now.AddMonths(-i);
            var sum = await _context.Salaries
                .Where(s => s.Month == d.Month && s.Year == d.Year)
                .SumAsync(s => s.TotalSalary);
            vm.SalaryChartLabels.Add($"T{d.Month}/{d.Year}");
            vm.SalaryChartData.Add(Math.Round(sum / 1_000_000, 1)); // đơn vị triệu
        }

        // ── Biểu đồ chấm công 7 ngày ─────────────────────────────
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var records = await _context.Timekeepings
                .Where(t => t.Date.Date == day)
                .ToListAsync();

            vm.AttendanceLabels.Add(day.ToString("dd/MM"));
            vm.AttendancePresentData.Add(records.Count(t => t.Status == "Có mặt"));
            vm.AttendanceLeaveData.Add(records.Count(t => t.Status == "Vắng có phép"));
            vm.AttendanceAbsentData.Add(records.Count(t => t.Status == "Vắng không phép"));
        }

        // ── Nhân sự theo phòng ban ────────────────────────────────
        var deptGroups = await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.DepartmentId != null)
            .GroupBy(e => e.Department!.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        vm.DeptLabels = deptGroups.Select(d => d.Name).ToList();
        vm.DeptData   = deptGroups.Select(d => d.Count).ToList();

        // ── Nhân viên chưa chấm công hôm nay ─────────────────────
        var checkedInIds = todayTimekeepings.Select(t => t.EmployeeId).ToHashSet();
        var leaveToday   = await _context.LeaveRequests
            .Where(r => r.StartDate <= today && r.EndDate >= today && r.Status == LeaveRequestStatus.Approved)
            .Select(r => r.EmployeeId)
            .ToListAsync();

        var absentList = await _context.Employees
            .Include(e => e.Department)
            .Where(e => !checkedInIds.Contains(e.Id))
            .Take(10)
            .ToListAsync();

        vm.AbsentEmployees = absentList.Select(e => new AbsentEmployeeInfo
        {
            Id             = e.Id,
            FullName       = e.FullName,
            DepartmentName = e.Department?.Name,
            Status         = leaveToday.Contains(e.Id) ? "Nghỉ có phép" : "Chưa chấm công"
        }).ToList();

        return View(vm);
    }

    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> EmployeeDashboard()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return RedirectToAction("Login", "Auth");

        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Email == userEmail);

        var vm = new EmployeeDashboardViewModel { Employee = employee };

        if (employee != null)
        {
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            // Chấm công tháng này
            var timekeepingsThisMonth = await _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id && t.Date >= firstDayOfMonth)
                .ToListAsync();

            vm.DaysWorked = timekeepingsThisMonth.Count(t => t.Status == "Có mặt");

            vm.RecentTimekeepings = await _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id)
                .OrderByDescending(t => t.Date)
                .Take(5)
                .ToListAsync();

            // Đơn từ
            vm.ProfileRequests = await _context.ProfileUpdateRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .ToListAsync();

            // Lương tháng hiện tại
            vm.CurrentSalary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id
                                       && s.Month == now.Month
                                       && s.Year == now.Year);

            // Lương tháng trước
            var prevMonth = now.Month == 1 ? 12 : now.Month - 1;
            var prevYear  = now.Month == 1 ? now.Year - 1 : now.Year;
            vm.PreviousSalary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id
                                       && s.Month == prevMonth
                                       && s.Year == prevYear);

            // Lịch sử 6 tháng
            vm.SalaryHistory = await _context.Salaries
                .Where(s => s.EmployeeId == employee.Id)
                .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
                .Take(6)
                .ToListAsync();
        }

        return View(vm);
    }
}
