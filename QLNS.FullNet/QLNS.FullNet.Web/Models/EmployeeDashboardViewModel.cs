using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Models
{
    public class EmployeeDashboardViewModel
    {
        public Employee? Employee { get; set; }

        // Chấm công
        public int DaysWorked { get; set; }
        public int ApprovedLeaveDays { get; set; }
        public int WorkingDaysInMonth { get; set; } = 22;
        public List<Timekeeping> RecentTimekeepings { get; set; } = new();

        // Bản ghi chấm công hôm nay (để hiển thị nút Check-in / Check-out)
        public Timekeeping? TodayTimekeeping { get; set; }

        // Đơn nghỉ phép
        public List<LeaveRequest> LeaveRequests { get; set; } = new();

        // Đơn cập nhật hồ sơ
        public List<ProfileUpdateRequest> ProfileRequests { get; set; } = new();

        // Lương tháng hiện tại
        public Salary? CurrentSalary { get; set; }

        // Lương tháng trước
        public Salary? PreviousSalary { get; set; }

        // Lịch sử lương 6 tháng gần nhất
        public List<Salary> SalaryHistory { get; set; } = new();

        // Lương ngày hiện tại (ước tính)
        public decimal EstimatedSalaryToDate =>
            Employee != null
                ? Math.Round((DaysWorked + ApprovedLeaveDays) * (Employee.Position?.DailyWage ?? 0) + Employee.Allowance, 0)
                : 0;
    }
}
