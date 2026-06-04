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

        // ============ THÊM MỚI (GET) ============
        public async Task<IActionResult> Create()
        {
            await PopulateEmployeesDropdown();
            return View(new Salary
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year
            });
        }

        // ============ THÊM MỚI (POST) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Salary salary)
        {
            // Loại bỏ validation cho navigation property
            ModelState.Remove("Employee");

            if (!ModelState.IsValid)
            {
                await PopulateEmployeesDropdown(salary.EmployeeId);
                return View(salary);
            }

            // Tính tổng lương
            salary.TotalSalary = salary.BaseSalary + salary.Allowance - salary.Deduction;

            _context.Salaries.Add(salary);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { month = salary.Month, year = salary.Year });
        }

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