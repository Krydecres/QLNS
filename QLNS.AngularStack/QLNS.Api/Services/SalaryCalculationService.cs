using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Services;

//Service này là nơi chứa logic tính lương. 
//Nó lấy nhân viên, chức vụ, chấm công, ca làm, nghỉ phép và ngày lễ. Sau đó tính tổng lương rồi lưu vào bảng Salarie

public class SalaryCalculationService : ISalaryCalculationService
{
    private readonly AppDbContext _context;

    public SalaryCalculationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Salary?> CalculateAsync(int employeeId, int month, int year)
    {
        if (month < 1 || month > 12) return null;

        var employee = await _context.Employees
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null) return null;

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var timekeepings = await _context.Timekeepings
            .Include(t => t.Shift)
            .Where(t => t.EmployeeId == employeeId
                && t.Date >= startDate
                && t.Date <= endDate
                && t.CheckInTime != null)
            .ToListAsync();

        var approvedLeaves = await _context.LeaveRequests
            .Where(l => l.EmployeeId == employeeId
                && l.Status == LeaveRequestStatus.Approved
                && l.StartDate.Date <= endDate.Date
                && l.EndDate.Date >= startDate.Date)
            .ToListAsync();

        var holidays = await _context.Holidays
            .Where(h => h.Month == month)
            .ToListAsync();

        decimal dailyWage = employee.Position?.DailyWage ?? 0;
        decimal workedSalary = 0;

        foreach (var item in timekeepings)
        {
            decimal multiplier = item.Shift?.WageMultiplier ?? 1.0m;
            workedSalary += dailyWage * multiplier;
        }

        var workedDates = timekeepings
            .Select(t => t.Date.Date)
            .Distinct()
            .ToList();

        decimal leaveAndHolidaySalary = 0;
        int daysInMonth = DateTime.DaysInMonth(year, month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, month, day);

            if (workedDates.Contains(currentDate.Date)) continue;

            bool isHoliday = holidays.Any(h => h.Day == day);

            bool isApprovedLeave = approvedLeaves.Any(l =>
                currentDate.Date >= l.StartDate.Date &&
                currentDate.Date <= l.EndDate.Date);

            if (isHoliday || isApprovedLeave)
            {
                leaveAndHolidaySalary += dailyWage;
            }
        }

        decimal baseSalary = workedSalary + leaveAndHolidaySalary;
        decimal allowance = employee.Allowance;
        decimal deduction = 0;
        decimal totalSalary = baseSalary + allowance - deduction;

        if (totalSalary < 0) totalSalary = 0;

        var salary = await _context.Salaries
            .FirstOrDefaultAsync(s =>
                s.EmployeeId == employeeId &&
                s.Month == month &&
                s.Year == year);

        if (salary == null)
        {
            salary = new Salary
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year
            };

            _context.Salaries.Add(salary);
        }

        salary.BaseSalary = baseSalary;
        salary.Allowance = allowance;
        salary.Deduction = deduction;
        salary.TotalSalary = totalSalary;

        await _context.SaveChangesAsync();

        return salary;
    }

    public async Task<List<Salary>> CalculateForAllAsync(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var employeeIds = await _context.EmployeeShifts
            .Where(es => es.WorkDate >= startDate && es.WorkDate <= endDate)
            .Select(es => es.EmployeeId)
            .Distinct()
            .ToListAsync();

        var result = new List<Salary>();

        foreach (var employeeId in employeeIds)
        {
            var salary = await CalculateAsync(employeeId, month, year);

            if (salary != null)
            {
                result.Add(salary);
            }
        }

        return result;
    }
}