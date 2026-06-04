using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ShiftController : Controller
{
    private readonly AppDbContext _context;

    public ShiftController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Shifts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Name.Contains(search) ||
                (s.Description != null && s.Description.Contains(search)));
        }

        var shifts = await query
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        ViewBag.Search = search;

        return View(shifts);
    }

    public IActionResult Create()
    {
        return View(new Shift
        {
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            BreakMinutes = 60,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Shift shift)
    {
        ValidateShiftTime(shift);

        if (await _context.Shifts.AnyAsync(s => s.Name == shift.Name))
        {
            ModelState.AddModelError("Name", "Tên ca làm này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(shift);
        }

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thêm ca làm việc thành công.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var shift = await _context.Shifts.FindAsync(id);

        if (shift == null)
        {
            return NotFound();
        }

        return View(shift);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Shift shift)
    {
        if (id != shift.Id)
        {
            return NotFound();
        }

        ValidateShiftTime(shift);

        if (await _context.Shifts.AnyAsync(s => s.Id != shift.Id && s.Name == shift.Name))
        {
            ModelState.AddModelError("Name", "Tên ca làm này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(shift);
        }

        try
        {
            _context.Shifts.Update(shift);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật ca làm việc thành công.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ShiftExists(shift.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var shift = await _context.Shifts.FindAsync(id);

        if (shift != null)
        {
            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa ca làm việc thành công.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidateShiftTime(Shift shift)
    {
        if (shift.EndTime <= shift.StartTime)
        {
            ModelState.AddModelError("EndTime", "Giờ kết thúc phải lớn hơn giờ bắt đầu.");
        }
    }

    private bool ShiftExists(int id)
    {
        return _context.Shifts.Any(s => s.Id == id);
    }
}