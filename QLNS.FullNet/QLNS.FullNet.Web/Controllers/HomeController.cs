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

            // Lấy tất cả bản ghi chấm công trong ngày
            var records = await _context.Timekeepings
                .Where(t => t.Date.Date == day)
                .ToListAsync();

            // Lấy danh sách nhân viên có đơn nghỉ phép được duyệt trong ngày đó
            var leaveEmployeeIds = await _context.LeaveRequests
                .Where(r => r.StartDate.Date <= day && r.EndDate.Date >= day
                            && r.Status == LeaveRequestStatus.Approved)
                .Select(r => r.EmployeeId)
                .ToListAsync();

            int present = records.Count(t => t.Status == "Có mặt");
            // Vắng có phép: bản ghi trong ngày đó mà nhân viên có đơn phép, hoặc không check-in nhưng có đơn phép
            int onLeave = records.Count(t => t.Status != "Có mặt" && leaveEmployeeIds.Contains(t.EmployeeId));
            // Cộng thêm những người không có bản ghi chấm công nhưng có đơn phép
            var checkedIds = records.Select(t => t.EmployeeId).ToHashSet();
            int leaveNoRecord = leaveEmployeeIds.Count(id => !checkedIds.Contains(id));
            onLeave += leaveNoRecord;
            // Vắng không phép: bản ghi "Vắng mặt" mà không có đơn phép
            int absent = records.Count(t => t.Status != "Có mặt" && !leaveEmployeeIds.Contains(t.EmployeeId));

            vm.AttendanceLabels.Add(day.ToString("dd/MM"));
            vm.AttendancePresentData.Add(present);
            vm.AttendanceLeaveData.Add(onLeave);
            vm.AttendanceAbsentData.Add(absent);
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
            var today = now.Date;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            // Chấm công tháng này
            var timekeepingsThisMonth = await _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id && t.Date >= firstDayOfMonth)
                .ToListAsync();

            vm.DaysWorked = timekeepingsThisMonth.Count(t => t.Status == "Có mặt");

            // Tính số ngày nghỉ phép đã được duyệt trong tháng tính đến hiện tại
            var approvedLeaves = await _context.LeaveRequests
                .Where(r => r.EmployeeId == employee.Id && r.Status == LeaveRequestStatus.Approved
                            && r.StartDate.Year == now.Year && r.StartDate.Month <= now.Month
                            && r.EndDate.Year == now.Year && r.EndDate.Month >= now.Month)
                .ToListAsync();

            var workedDates = timekeepingsThisMonth.Where(t => t.Status == "Có mặt").Select(t => t.Date.Date).ToHashSet();
            int approvedLeaveDaysCount = 0;
            int daysToCheck = now.Month == today.Month ? today.Day : DateTime.DaysInMonth(now.Year, now.Month);

            for (int i = 1; i <= daysToCheck; i++)
            {
                var currentDate = new DateTime(now.Year, now.Month, i);
                if (workedDates.Contains(currentDate.Date)) continue;

                if (approvedLeaves.Any(lr => currentDate.Date >= lr.StartDate.Date && currentDate.Date <= lr.EndDate.Date))
                {
                    approvedLeaveDaysCount++;
                }
            }

            vm.ApprovedLeaveDays = approvedLeaveDaysCount;

            vm.RecentTimekeepings = await _context.Timekeepings
                .Include(t => t.Shift)
                .Where(t => t.EmployeeId == employee.Id)
                .OrderByDescending(t => t.Date)
                .Take(5)
                .ToListAsync();

            // Bản ghi chấm công hôm nay (để hiển thị nút Check-in/Check-out)
            vm.TodayTimekeeping = await _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id && t.Date.Date == today)
                .OrderByDescending(t => t.CheckInTime)
                .FirstOrDefaultAsync();

            // Đơn nghỉ phép (5 đơn gần nhất)
            vm.LeaveRequests = await _context.LeaveRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Đơn cập nhật hồ sơ
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
