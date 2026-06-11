using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using QLNS.FullNet.Web.Services;

namespace QLNS.FullNet.Web.Controllers
{
    public class SalariesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ISalaryCalculationService _salaryService;

        public SalariesController(AppDbContext context, ISalaryCalculationService salaryService)
        {
            _context = context;
            _salaryService = salaryService;
        }

        // ============ HIỂN THỊ DANH SÁCH ============
        public async Task<IActionResult> Index(int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            var salaries = await _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.Month == selectedMonth && s.Year == selectedYear)
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