using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System.Security.Claims;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QLNS.FullNet.Web.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class TimekeepingController : Controller
    {
        private readonly AppDbContext _context;

        public TimekeepingController(AppDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. DANH SÁCH CHẤM CÔNG (Admin)
        // =======================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Timekeepings
                .Include(t => t.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= endOfDay);
            }

            var timekeepings = await query.OrderByDescending(t => t.Date).ToListAsync();

            ViewBag.Employees = await _context.Employees.AsNoTracking().ToListAsync();
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(timekeepings);
        }

        // =======================================================
        // 2. XUẤT DỮ LIỆU RA EXCEL (Admin)
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
                query = query.Where(t => t.Date >= startDate.Value);

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

                worksheet.Cell(currentRow, 1).Value = "ID Chấm Công";
                worksheet.Cell(currentRow, 2).Value = "Tên Nhân Viên";
                worksheet.Cell(currentRow, 3).Value = "Ngày Chấm Công";
                worksheet.Cell(currentRow, 4).Value = "Giờ Vào";
                worksheet.Cell(currentRow, 5).Value = "Giờ Ra";
                worksheet.Cell(currentRow, 6).Value = "Trạng Thái";
                worksheet.Cell(currentRow, 7).Value = "Ghi Chú";
                worksheet.Row(currentRow).Style.Font.Bold = true;

                foreach (var item in timekeepings)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.Id;
                    worksheet.Cell(currentRow, 2).Value = item.Employee?.FullName ?? "N/A";
                    worksheet.Cell(currentRow, 3).Value = item.Date.ToString("dd/MM/yyyy");
                    worksheet.Cell(currentRow, 4).Value = item.CheckInTime.HasValue ? item.CheckInTime.Value.ToString(@"hh\:mm") : "-";
                    worksheet.Cell(currentRow, 5).Value = item.CheckOutTime.HasValue ? item.CheckOutTime.Value.ToString(@"hh\:mm") : "-";
                    worksheet.Cell(currentRow, 6).Value = item.Status;
                    worksheet.Cell(currentRow, 7).Value = item.Note;
                }

                worksheet.Columns().AdjustToContents();

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
        // 3. CHẤM CÔNG HÔM NAY - CHECK IN (Admin chấm hộ hoặc Employee tự chấm)
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int? employeeId)
        {
            Employee? employee = null;

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
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
                employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy hồ sơ nhân sự khớp với tài khoản '{userEmail}'.";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/");
                }
            }

            var today = DateTime.Today;
            var record = await _context.Timekeepings
                .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Date.Date == today);

            if (record != null)
            {
                TempData["ErrorMessage"] = $"Nhân viên '{employee.FullName}' đã được chấm công hôm nay rồi.";
            }
            else
            {
                _context.Timekeepings.Add(new Timekeeping
                {
                    EmployeeId = employee.Id,
                    Date = DateTime.Now,
                    CheckInTime = DateTime.Now.TimeOfDay,
                    Status = "Có mặt"
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Check-in cho '{employee.FullName}' thành công lúc {DateTime.Now:HH:mm}.";
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        // =======================================================
        // 4. CHECK OUT (Employee tự check-out)
        // =======================================================
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null) return NotFound("Hồ sơ nhân viên không tồn tại.");

            var today = DateTime.Today;
            var record = await _context.Timekeepings
                .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Date.Date == today);

            if (record != null && record.CheckInTime != null)
            {
                if (record.CheckOutTime == null)
                {
                    record.CheckOutTime = DateTime.Now.TimeOfDay;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Check-out thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Bạn đã check-out hôm nay rồi.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Bạn chưa check-in hôm nay.";
            }

            return RedirectToAction(nameof(MyAttendance));
        }

        // =======================================================
        // 5. CHẤM CÔNG CỦA TÔI (Employee - có filter ngày)
        // =======================================================
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyAttendance(DateTime? startDate, DateTime? endDate)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
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
                query = query.Where(t => t.Date >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= endOfDay);
            }

            var timekeepings = await query.OrderByDescending(t => t.Date).ToListAsync();

            ViewBag.TodayRecord = timekeepings.FirstOrDefault(t => t.Date.Date == DateTime.Today);
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(timekeepings);
        }
    }
}