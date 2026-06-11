using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Services
{
    public class SalaryCalculationService : ISalaryCalculationService
    {
        private readonly AppDbContext _context;

        public SalaryCalculationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Salary?> CalculateAsync(Employee employee, int month, int year)
        {
            // Đảm bảo lấy đầy đủ thông tin Chức vụ
            var empWithPosition = await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Id == employee.Id);
                
            if (empWithPosition == null) return null;

            var timekeepings = await _context.Timekeepings
                .Include(t => t.Shift)
                .Where(t => t.EmployeeId == employee.Id && t.Date.Month == month && t.Date.Year == year)
                .ToListAsync();
                
            var leaveRequests = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == employee.Id && lr.Status == LeaveRequestStatus.Approved 
                             && lr.StartDate.Year == year && lr.StartDate.Month <= month
                             && lr.EndDate.Year == year && lr.EndDate.Month >= month)
                .ToListAsync();

            // Mức lương ngày công lấy từ Chức vụ (không dùng Lương cơ bản nữa)
            decimal positionDailyWage = empWithPosition.Position?.DailyWage ?? 0;
            
            decimal workedSalary = 0;
            var workedDates = timekeepings.Where(t => t.CheckInTime != null).Select(t => t.Date.Date).Distinct().ToList();

            // Tính tiền cho các ca làm việc
            foreach (var t in timekeepings.Where(t => t.CheckInTime != null))
            {
                decimal multiplier = t.Shift?.WageMultiplier ?? 1.0m;
                workedSalary += positionDailyWage * multiplier;
            }

            // Lấy tất cả các ngày trong tháng
            int daysInMonth = DateTime.DaysInMonth(year, month);
            decimal leaveSalary = 0;

            for (int i = 1; i <= daysInMonth; i++)
            {
                var currentDate = new DateTime(year, month, i);

                // Nếu ngày đó đã đi làm thì không tính lương nghỉ phép nữa
                if (workedDates.Contains(currentDate.Date)) continue;

                // Kiểm tra xem ngày này có nằm trong đơn nghỉ phép đã duyệt không
                bool isApprovedLeave = leaveRequests.Any(lr => currentDate.Date >= lr.StartDate.Date && currentDate.Date <= lr.EndDate.Date);
                if (isApprovedLeave)
                {
                    leaveSalary += positionDailyWage * 1.0m; // Hưởng 100% lương
                }
            }

            decimal earnedBase = workedSalary + leaveSalary; // Lương ngày công thực tế (đã tính ca + nghỉ phép)
            decimal allowance = empWithPosition.Allowance;
            decimal deduction = 0;
            
            decimal totalSalary = earnedBase + allowance - deduction;

            if (totalSalary < 0) totalSalary = 0;

            return new Salary
            {
                EmployeeId = employee.Id,
                Month = month,
                Year = year,
                BaseSalary = earnedBase,   // Lương ngày công thực tế (DailyWage × ngày công × hệ số ca)
                Allowance = allowance,
                Deduction = deduction,
                TotalSalary = totalSalary
            };
        }
    }
}