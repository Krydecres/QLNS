using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Controllers;

[Authorize(Roles = "Admin")]
public class EmployeeShiftController : Controller
{
    private readonly AppDbContext _context;

    public EmployeeShiftController(AppDbContext context)
    {
        _context = context;
    }

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