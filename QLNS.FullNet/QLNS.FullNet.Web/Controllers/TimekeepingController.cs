using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
// LƯU Ý: Đổi 'QLNS.FullNet' thành namespace thực tế chứa file DbContext và Models của bạn
using QLNS.FullNet.Data; 
using QLNS.FullNet.Data.Entities;
using System.Security.Claims;

namespace QLNS.FullNet.Web.Controllers
{
    [Authorize(Roles = "Admin,Employee")] // Cho phép cả Admin và Employee sử dụng (sẽ phân quyền chi tiết cho từng Action)
    public class TimekeepingController : Controller
    {
        
        private readonly AppDbContext _context;

        public TimekeepingController(AppDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. GIAO DIỆN HIỂN THỊ DANH SÁCH CHẤM CÔNG
        // =======================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Timekeepings
                .Include(t => t.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // Cộng thêm 1 ngày và trừ đi 1 tick để bao gồm đến 23:59:59 của ngày kết thúc
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= endOfDay);
            }

            var timekeepings = await query.OrderByDescending(t => t.Date).ToListAsync();

            // Lấy danh sách nhân viên để Admin có thể chọn chấm công
            ViewBag.Employees = await _context.Employees.AsNoTracking().ToListAsync();

            // Lưu giữ lại giá trị lọc để hiển thị lên Form
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(timekeepings);
        }

        // =======================================================
        // 2. TÍNH NĂNG XUẤT DỮ LIỆU RA EXCEL
        // =======================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Timekeepings
                .Include(t => t.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= endOfDay);
            }

            var timekeepings = await query.OrderByDescending(t => t.Date).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("DuLieuChamCong");
                var currentRow = 1;

                // Tạo dòng Tiêu đề (Header)
                worksheet.Cell(currentRow, 1).Value = "ID Chấm Công";
                worksheet.Cell(currentRow, 2).Value = "Tên Nhân Viên";
                worksheet.Cell(currentRow, 3).Value = "Ngày Chấm Công";
                worksheet.Cell(currentRow, 4).Value = "Trạng Thái";
                
                // Làm đậm dòng tiêu đề cho đẹp
                worksheet.Row(currentRow).Style.Font.Bold = true;

                // Đổ dữ liệu từ Database vào các dòng tiếp theo
                foreach (var item in timekeepings)
                {
                    currentRow++;
                    
                    worksheet.Cell(currentRow, 1).Value = item.Id;
                    // Nếu Employee null thì hiển thị "N/A" để tránh lỗi
                    worksheet.Cell(currentRow, 2).Value = item.Employee?.FullName ?? "N/A";
                    
                    // LƯU Ý: Thay item.Date và item.Status bằng tên cột thực tế trong bảng Timekeeping của nhóm bạn
                    // Ví dụ: worksheet.Cell(currentRow, 3).Value = item.CheckInTime.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(currentRow, 3).Value = item.Date.ToString("dd/MM/yyyy") ?? ""; 
                    worksheet.Cell(currentRow, 4).Value = item.Status; 
                }

                // Tự động căn chỉnh độ rộng các cột theo nội dung
                worksheet.Columns().AdjustToContents();

                // Trả file Excel về cho trình duyệt tải xuống
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(
                        content, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        "DuLieuChamCong.xlsx"
                    );
                }
            }
        }

        // =======================================================
        // 4. CHẤM CÔNG HÔM NAY (Cho Employee & Admin tự chấm)
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int? employeeId)
        {
            Employee employee = null;

            // Nếu Admin chọn chấm công cho một nhân viên cụ thể từ danh sách
            if (employeeId.HasValue && User.IsInRole("Admin"))
            {
                employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId.Value);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên đã chọn.";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/");
                }
            }
            else
            {
                // Tính năng tự chấm công (Dành cho tài khoản Employee)
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
                employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

                if (employee == null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy hồ sơ nhân sự nào khớp với tài khoản đang đăng nhập ('{userEmail}').";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/");
                }
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var alreadyCheckedIn = await _context.Timekeepings
                .AnyAsync(t => t.EmployeeId == employee.Id && t.Date >= today && t.Date < tomorrow);

            if (alreadyCheckedIn) TempData["ErrorMessage"] = $"Nhân viên '{employee.FullName}' đã được chấm công cho ngày hôm nay rồi.";
            else
            {
                _context.Timekeepings.Add(new Timekeeping
                {
                    EmployeeId = employee.Id,
                    Date = DateTime.Now,
                    Status = "Có mặt"
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Chấm công cho '{employee.FullName}' thành công lúc {DateTime.Now:HH:mm}.";
            }

            // Trở về trang trước đó
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        // =======================================================
        // 5. CHẤM CÔNG CỦA TÔI (Cho Role Employee)
        // =======================================================
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyAttendance(DateTime? startDate, DateTime? endDate)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = User.Identity?.Name;
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ nhân sự của bạn.";
                return RedirectToAction("EmployeeDashboard", "Home");
            }

            var query = _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id)
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= endOfDay);
            }

            var timekeepings = await query.OrderByDescending(t => t.Date).ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(timekeepings);
        }
    }
}