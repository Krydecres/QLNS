using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using QLNS.FullNet.Web.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace QLNS.FullNet.Web.Controllers
{
    [Authorize]
    public class SalariesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ISalaryCalculationService _salaryService;
        private readonly IEmailService _emailService;

        public SalariesController(AppDbContext context, ISalaryCalculationService salaryService, IEmailService emailService)
        {
            _context = context;
            _salaryService = salaryService;
            _emailService = emailService;
        }

        // ============ HIỂN THỊ DANH SÁCH ============
        public async Task<IActionResult> Index(int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            var query = _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.Month == selectedMonth && s.Year == selectedYear);

            if (User.IsInRole("Employee"))
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);
                if (emp != null)
                {
                    query = query.Where(s => s.EmployeeId == emp.Id);
                }
            }

            var salaries = await query
                .AsNoTracking()
                .ToListAsync();

            return View(salaries);
        }

        // ============ TÍNH LƯƠNG TỰ ĐỘNG ============
        public async Task<IActionResult> Seed(int month, int year)
        {
            if (month == 0 || year == 0)
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }

            // xoá dữ liệu cũ của tháng này
            var existingSalaries = await _context.Salaries
                .Where(s => s.Month == month && s.Year == year)
                .ToListAsync();
            _context.Salaries.RemoveRange(existingSalaries);
            await _context.SaveChangesAsync();

            var employees = await _context.Employees
                .AsNoTracking()
                .ToListAsync();

            foreach (var e in employees)
            {
                var salary = await _salaryService.CalculateAsync(e, month, year);

                if (salary != null)
                {
                    _context.Salaries.Add(salary);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { month, year });
        }

        // ============ THÊM MỚI (GET/POST) ĐÃ BỊ XÓA THEO YÊU CẦU ============

        // ============ CHỈNH SỬA (GET) ============
        public async Task<IActionResult> Edit(int id)
        {
            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
                return NotFound();

            await PopulateEmployeesDropdown(salary.EmployeeId);
            return View(salary);
        }

        // ============ CHỈNH SỬA (POST) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Salary salary)
        {
            if (id != salary.Id)
                return NotFound();

            // Loại bỏ validation cho navigation property
            ModelState.Remove("Employee");

            if (!ModelState.IsValid)
            {
                await PopulateEmployeesDropdown(salary.EmployeeId);
                return View(salary);
            }

            try
            {
                // Tính lại tổng lương
                salary.TotalSalary = salary.BaseSalary + salary.Allowance - salary.Deduction;

                _context.Update(salary);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Salaries.AnyAsync(s => s.Id == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index), new { month = salary.Month, year = salary.Year });
        }

        // ============ XÓA (POST) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var salary = await _context.Salaries.FindAsync(id);

            if (salary == null)
                return NotFound();

            int month = salary.Month;
            int year = salary.Year;

            _context.Salaries.Remove(salary);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { month, year });
        }

        // ============ XÓA TOÀN BỘ (POST) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var all = await _context.Salaries.ToListAsync();
            _context.Salaries.RemoveRange(all);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa toàn bộ {all.Count} bản ghi lương. Vui lòng tính lại.";
            return RedirectToAction(nameof(Index));
        }

        // ============ XUẤT EXCEL ============
        public async Task<IActionResult> ExportExcel(int month, int year)
        {
            var salaries = await _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.Month == month && s.Year == year)
                .AsNoTracking()
                .ToListAsync();

            if (!salaries.Any())
            {
                TempData["ErrorMessage"] = $"Không có dữ liệu lương của tháng {month}/{year} để xuất.";
                return RedirectToAction(nameof(Index), new { month, year });
            }

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Lương T{month}-{year}");

            // Header
            worksheet.Cell(1, 1).Value = "Kỳ lương";
            worksheet.Cell(1, 2).Value = "Mã NV";
            worksheet.Cell(1, 3).Value = "Tên nhân viên";
            worksheet.Cell(1, 4).Value = "Lương ngày công";
            worksheet.Cell(1, 5).Value = "Phụ cấp";
            worksheet.Cell(1, 6).Value = "Khấu trừ";
            worksheet.Cell(1, 7).Value = "Tổng lương";

            // Format Header
            var headerRange = worksheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            // Dữ liệu
            int row = 2;
            foreach (var item in salaries)
            {
                worksheet.Cell(row, 1).Value = $"T{item.Month}/{item.Year}";
                worksheet.Cell(row, 2).Value = item.EmployeeId;
                worksheet.Cell(row, 3).Value = item.Employee?.FullName;
                worksheet.Cell(row, 4).Value = item.BaseSalary;
                worksheet.Cell(row, 5).Value = item.Allowance;
                worksheet.Cell(row, 6).Value = item.Deduction;
                worksheet.Cell(row, 7).Value = item.TotalSalary;
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BangLuong_T{month}_{year}.xlsx");
        }

        // ============ GỬI EMAIL PHIẾU LƯƠNG (CÁ NHÂN) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPayslip(int id)
        {
            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null || salary.Employee == null || string.IsNullOrEmpty(salary.Employee.Email))
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu lương hoặc nhân viên chưa có email.";
                return RedirectToAction(nameof(Index), new { month = salary?.Month, year = salary?.Year });
            }

            string subject = $"[QLNS] Phiếu lương kỳ T{salary.Month}/{salary.Year}";
            string body = $@"
                <h3>Chào {salary.Employee.FullName},</h3>
                <p>Phòng Nhân sự gửi bạn chi tiết bảng lương kỳ <b>T{salary.Month}/{salary.Year}</b>:</p>
                <ul>
                    <li><b>Lương ngày công:</b> {salary.BaseSalary:N0} đ</li>
                    <li><b>Phụ cấp:</b> {salary.Allowance:N0} đ</li>
                    <li><b>Khấu trừ (Phạt/Vắng):</b> {salary.Deduction:N0} đ</li>
                </ul>
                <h4 style='color: green;'>Tổng lương thực nhận: {salary.TotalSalary:N0} đ</h4>
                <br/>
                <p>Nếu có thắc mắc, vui lòng liên hệ phòng Nhân sự.</p>
                <p>Trân trọng.</p>
            ";

            await _emailService.SendEmailAsync(salary.Employee.Email, subject, body);

            TempData["SuccessMessage"] = $"Đã gửi phiếu lương thành công đến {salary.Employee.Email}";
            return RedirectToAction(nameof(Index), new { month = salary.Month, year = salary.Year });
        }

        // ============ GỬI EMAIL PHIẾU LƯƠNG (TẤT CẢ) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAllPayslips(int month, int year)
        {
            var salaries = await _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.Month == month && s.Year == year && s.Employee != null && !string.IsNullOrEmpty(s.Employee.Email))
                .ToListAsync();

            if (!salaries.Any())
            {
                TempData["ErrorMessage"] = "Không có nhân viên nào có email hợp lệ để gửi.";
                return RedirectToAction(nameof(Index), new { month, year });
            }

            int successCount = 0;
            foreach (var salary in salaries)
            {
                string subject = $"[QLNS] Phiếu lương kỳ T{salary.Month}/{salary.Year}";
                string body = $@"
                    <h3>Chào {salary.Employee!.FullName},</h3>
                    <p>Phòng Nhân sự gửi bạn chi tiết bảng lương kỳ <b>T{salary.Month}/{salary.Year}</b>:</p>
                    <ul>
                        <li><b>Lương ngày công:</b> {salary.BaseSalary:N0} đ</li>
                        <li><b>Phụ cấp:</b> {salary.Allowance:N0} đ</li>
                        <li><b>Khấu trừ (Phạt/Vắng):</b> {salary.Deduction:N0} đ</li>
                    </ul>
                    <h4 style='color: green;'>Tổng lương thực nhận: {salary.TotalSalary:N0} đ</h4>
                    <br/>
                    <p>Nếu có thắc mắc, vui lòng liên hệ phòng Nhân sự.</p>
                    <p>Trân trọng.</p>
                ";

                await _emailService.SendEmailAsync(salary.Employee.Email!, subject, body);
                successCount++;
            }

            TempData["SuccessMessage"] = $"Đã gửi thành công {successCount} phiếu lương.";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        // ============ HELPER: Dropdown nhân viên ============
        private async Task PopulateEmployeesDropdown(int? selectedId = null)
        {
            var employees = await _context.Employees
                .OrderBy(e => e.FullName)
                .Select(e => new { e.Id, e.FullName })
                .ToListAsync();

            ViewBag.Employees = new SelectList(employees, "Id", "FullName", selectedId);
        }
    }
}