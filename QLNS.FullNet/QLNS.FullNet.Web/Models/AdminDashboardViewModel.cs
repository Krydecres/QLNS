namespace QLNS.FullNet.Web.Models
{
    public class AdminDashboardViewModel
    {
        // Thống kê nhân sự
        public int TotalEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalPositions { get; set; }

        // Chấm công hôm nay
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int NotCheckedToday { get; set; }

        // Đơn từ chờ duyệt
        public int PendingLeaveRequests { get; set; }
        public int PendingProfileUpdates { get; set; }

        // Lương tháng hiện tại
        public int SalaryCalculatedCount { get; set; }
        public decimal TotalSalaryThisMonth { get; set; }
        public decimal TotalSalaryLastMonth { get; set; }

        // Biểu đồ lương 6 tháng
        public List<string> SalaryChartLabels { get; set; } = new();
        public List<decimal> SalaryChartData { get; set; } = new();

        // Biểu đồ chấm công 7 ngày (theo ngày)
        public List<string> AttendanceLabels { get; set; } = new();
        public List<int> AttendancePresentData { get; set; } = new();
        public List<int> AttendanceLeaveData { get; set; } = new();
        public List<int> AttendanceAbsentData { get; set; } = new();

        // Nhân sự theo phòng ban
        public List<string> DeptLabels { get; set; } = new();
        public List<int> DeptData { get; set; } = new();

        // Nhân viên chưa chấm công hôm nay
        public List<AbsentEmployeeInfo> AbsentEmployees { get; set; } = new();

        // Tính %
        public double PresentRate =>
            TotalEmployees > 0 ? Math.Round((double)PresentToday / TotalEmployees * 100, 1) : 0;

        public decimal SalaryDiff => TotalSalaryThisMonth - TotalSalaryLastMonth;
        public bool SalaryUp => SalaryDiff >= 0;
    }

    public class AbsentEmployeeInfo
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string? DepartmentName { get; set; }
        public string Status { get; set; } = "Chưa chấm công";
    }
}
