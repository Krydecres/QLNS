using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System.Security.Claims;

namespace QLNS.FullNet.Web.Controllers;

[Authorize]
public class EmployeeShiftController : Controller
{
    private readonly AppDbContext _context;

    public EmployeeShiftController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(DateTime? workDate, int? employeeId, int? shiftId)
    {
        var query = _context.EmployeeShifts
            .Include(es => es.Employee)
            .Include(es => es.Shift)
            .AsQueryable();

        if (workDate.HasValue)
        {
            query = query.Where(es => es.WorkDate.Date == workDate.Value.Date);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(es => es.EmployeeId == employeeId.Value);
        }

        if (shiftId.HasValue)
        {
            query = query.Where(es => es.ShiftId == shiftId.Value);
        }

        var employeeShifts = await query
            .OrderByDescending(es => es.WorkDate)
            .ThenBy(es => es.Shift!.StartTime)
            .ThenBy(es => es.Employee!.FullName)
            .ToListAsync();

        await LoadSelectLists(employeeId, shiftId);

        ViewBag.WorkDate = workDate?.ToString("yyyy-MM-dd");

        return View(employeeShifts);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await LoadSelectLists();

        return View(new EmployeeShift
        {
            WorkDate = DateTime.Today,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(EmployeeShift employeeShift)
    {
        await ValidateEmployeeShift(employeeShift);

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(employeeShift.EmployeeId, employeeShift.ShiftId);
            return View(employeeShift);
        }

        _context.EmployeeShifts.Add(employeeShift);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gán ca cho nhân viên thành công.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var employeeShift = await _context.EmployeeShifts.FindAsync(id);

        if (employeeShift == null)
        {
            return NotFound();
        }

        await LoadSelectLists(employeeShift.EmployeeId, employeeShift.ShiftId);

        return View(employeeShift);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, EmployeeShift employeeShift)
    {
        if (id != employeeShift.Id)
        {
            return NotFound();
        }

        await ValidateEmployeeShift(employeeShift);

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(employeeShift.EmployeeId, employeeShift.ShiftId);
            return View(employeeShift);
        }

        _context.EmployeeShifts.Update(employeeShift);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật lịch ca thành công.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var employeeShift = await _context.EmployeeShifts.FindAsync(id);

        if (employeeShift != null)
        {
            _context.EmployeeShifts.Remove(employeeShift);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa lịch ca thành công.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> MyShifts(DateTime? fromDate, DateTime? toDate)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            email = User.Identity?.Name;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction("Login", "Auth");
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == email);

        if (employee == null)
        {
            ViewBag.ErrorMessage = "Không tìm thấy hồ sơ nhân viên tương ứng với tài khoản đang đăng nhập.";
            ViewBag.FromDate = DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.ToDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");

            return View(new List<EmployeeShift>());
        }

        var startDate = fromDate ?? DateTime.Today;
        var endDate = toDate ?? DateTime.Today.AddDays(7);

        if (endDate.Date < startDate.Date)
        {
            ModelState.AddModelError("", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            endDate = startDate;
        }

        var myShifts = await _context.EmployeeShifts
            .Include(es => es.Employee)
            .Include(es => es.Shift)
            .Where(es =>
                es.EmployeeId == employee.Id &&
                es.WorkDate.Date >= startDate.Date &&
                es.WorkDate.Date <= endDate.Date &&
                es.IsActive)
            .OrderBy(es => es.WorkDate)
            .ThenBy(es => es.Shift!.StartTime)
            .ToListAsync();

        ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
        ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");
        ViewBag.EmployeeName = employee.FullName;

        return View(myShifts);
    }

    private async Task LoadSelectLists(int? selectedEmployeeId = null, int? selectedShiftId = null)
    {
        var employees = await _context.Employees
            .OrderBy(e => e.FullName)
            .ToListAsync();

        var shifts = await _context.Shifts
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        ViewBag.Employees = new SelectList(employees, "Id", "FullName", selectedEmployeeId);
        ViewBag.Shifts = new SelectList(shifts, "Id", "Name", selectedShiftId);
    }

    private async Task ValidateEmployeeShift(EmployeeShift employeeShift)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeShift.EmployeeId);

        if (!employeeExists)
        {
            ModelState.AddModelError("EmployeeId", "Nhân viên không tồn tại.");
        }

        var shiftExists = await _context.Shifts
            .AnyAsync(s => s.Id == employeeShift.ShiftId && s.IsActive);

        if (!shiftExists)
        {
            ModelState.AddModelError("ShiftId", "Ca làm không tồn tại hoặc đã ngừng sử dụng.");
        }

        var isDuplicate = await _context.EmployeeShifts.AnyAsync(es =>
            es.Id != employeeShift.Id &&
            es.EmployeeId == employeeShift.EmployeeId &&
            es.ShiftId == employeeShift.ShiftId &&
            es.WorkDate.Date == employeeShift.WorkDate.Date);

        if (isDuplicate)
        {
            ModelState.AddModelError("", "Nhân viên này đã được gán vào ca này trong cùng ngày.");
        }
    }
}