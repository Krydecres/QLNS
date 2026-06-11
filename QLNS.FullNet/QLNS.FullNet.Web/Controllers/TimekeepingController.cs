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
                .Include(t => t.Shift)
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
                .Include(t => t.Shift)
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
                worksheet.Cell(currentRow, 4).Value = "Ca Làm";
                worksheet.Cell(currentRow, 5).Value = "Giờ Vào";
                worksheet.Cell(currentRow, 6).Value = "Giờ Ra";
                worksheet.Cell(currentRow, 7).Value = "Trạng Thái";
                worksheet.Cell(currentRow, 8).Value = "Ghi Chú";
                worksheet.Row(currentRow).Style.Font.Bold = true;

                foreach (var item in timekeepings)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.Id;
                    worksheet.Cell(currentRow, 2).Value = item.Employee?.FullName ?? "N/A";
                    worksheet.Cell(currentRow, 3).Value = item.Date.ToString("dd/MM/yyyy");
                    worksheet.Cell(currentRow, 4).Value = item.Shift?.Name ?? "Ca tự do";
                    worksheet.Cell(currentRow, 5).Value = item.CheckInTime.HasValue ? item.CheckInTime.Value.ToString(@"hh\:mm") : "-";
                    worksheet.Cell(currentRow, 6).Value = item.CheckOutTime.HasValue ? item.CheckOutTime.Value.ToString(@"hh\:mm") : "-";
                    worksheet.Cell(currentRow, 7).Value = item.Status ?? "Có mặt";
                    worksheet.Cell(currentRow, 8).Value = item.Note;
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
            bool isAdminCheckingForEmployee = employeeId.HasValue && User.IsInRole("Admin");

            if (isAdminCheckingForEmployee)
            {
                employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId.Value);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên đã chọn.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
                employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy hồ sơ nhân sự khớp với tài khoản '{userEmail}'.";
                    return RedirectToAction(nameof(MyAttendance));
                }
            }

            var today = DateTime.Today;
            
            var assignedShifts = await _context.EmployeeShifts
                .Where(es => es.EmployeeId == employee.Id && es.WorkDate.Date == today && es.IsActive)
                .ToListAsync();
                
            var existingRecords = await _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id && t.Date.Date == today)
                .ToListAsync();

            int? shiftIdToAssign = null;

            if (assignedShifts.Any())
            {
                // Tìm ca đầu tiên chưa được chấm
                var unrecordedShift = assignedShifts.FirstOrDefault(es => !existingRecords.Any(t => t.ShiftId == es.ShiftId));
                if (unrecordedShift != null)
                {
                    shiftIdToAssign = unrecordedShift.ShiftId;
                }
                else
                {
                    TempData["ErrorMessage"] = $"Nhân viên '{employee.FullName}' đã hoàn thành tất cả các ca được phân công hôm nay.";
                    return isAdminCheckingForEmployee ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(MyAttendance));
                }
            }
            else
            {
                // Không được phân ca. Chỉ cho check-in 1 lần (tương đương 1 ca mặc định).
                if (existingRecords.Any())
                {
                    TempData["ErrorMessage"] = $"Nhân viên '{employee.FullName}' đã được chấm công hôm nay rồi.";
                    return isAdminCheckingForEmployee ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(MyAttendance));
                }
            }

            _context.Timekeepings.Add(new Timekeeping
            {
                EmployeeId = employee.Id,
                ShiftId = shiftIdToAssign,
                Date = DateTime.Now,
                CheckInTime = DateTime.Now.TimeOfDay,
                Status = "Có mặt"
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Check-in cho '{employee.FullName}' thành công lúc {DateTime.Now:HH:mm}.";

            // Admin chấm hộ → quay lại trang Index; Employee tự chấm → quay lại MyAttendance
            return isAdminCheckingForEmployee
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(MyAttendance));
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
            
            // Tìm bản ghi chấm công gần nhất chưa có CheckOut
            var record = await _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id && t.Date.Date == today && t.CheckOutTime == null)
                .OrderByDescending(t => t.CheckInTime)
                .FirstOrDefaultAsync();

            if (record != null)
            {
                record.CheckOutTime = DateTime.Now.TimeOfDay;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Check-out thành công!";
            }
            else
            {
                // Kiểm tra xem đã có bản ghi nào hoàn tất chưa
                var anyCompleted = await _context.Timekeepings.AnyAsync(t => t.EmployeeId == employee.Id && t.Date.Date == today && t.CheckOutTime != null);
                if (anyCompleted)
                {
                    TempData["ErrorMessage"] = "Bạn đã hoàn thành check-out cho các ca làm việc của mình.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Bạn chưa check-in ca nào, hoặc đã bị đánh vắng.";
                }
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
                .Include(t => t.Shift)
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

        // =======================================================
        // 6. NHẬP CÔNG THỦ CÔNG (Admin)
        // =======================================================
        [Authorize(Roles = "Admin")]
        public IActionResult ManualEntry()
        {
            ViewBag.EmployeeId = new SelectList(_context.Employees, "Id", "FullName");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualEntry(Timekeeping model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Timekeepings
                    .FirstOrDefaultAsync(t => t.EmployeeId == model.EmployeeId && t.Date.Date == model.Date.Date);

                if (existing != null)
                {
                    existing.CheckInTime = model.CheckInTime;
                    existing.CheckOutTime = model.CheckOutTime;
                    existing.Note = model.Note;
                    if (!string.IsNullOrWhiteSpace(model.Status))
                        existing.Status = model.Status;
                    _context.Update(existing);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(model.Status))
                        model.Status = "Có mặt";
                    _context.Add(model);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật dữ liệu chấm công thành công.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.EmployeeId = new SelectList(_context.Employees, "Id", "FullName", model.EmployeeId);
            return View(model);
        }
    }
}