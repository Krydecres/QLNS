using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Models
{
    public class EmployeeDashboardViewModel
    {
        public Employee? Employee { get; set; }

        // Chấm công
        public int DaysWorked { get; set; }
        public int WorkingDaysInMonth { get; set; } = 22;
        public List<Timekeeping> RecentTimekeepings { get; set; } = new();

        // Đơn từ / Yêu cầu
        public List<ProfileUpdateRequest> ProfileRequests { get; set; } = new();

        // Lương tháng hiện tại
        public Salary? CurrentSalary { get; set; }

        // Lương tháng trước
        public Salary? PreviousSalary { get; set; }

        // Lịch sử lương 6 tháng gần nhất
        public List<Salary> SalaryHistory { get; set; } = new();

        // Lương ngày hiện tại (ước tính)
        public decimal EstimatedSalaryToDate =>
            CurrentSalary != null && WorkingDaysInMonth > 0
                ? Math.Round(CurrentSalary.TotalSalary / WorkingDaysInMonth * DaysWorked, 0)
                : 0;
    }
}
