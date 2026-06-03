using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System.Threading.Tasks;
using System.Linq;

namespace QLNS.FullNet.Web.Controllers;

[Authorize(Roles = "Admin")]
public class HolidayController : Controller
{
    private readonly AppDbContext _context;

    public HolidayController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Holiday
    public async Task<IActionResult> Index()
    {
        var holidays = await _context.Holidays
            .OrderBy(h => h.Month)
            .ThenBy(h => h.Day)
            .ToListAsync();
        return View(holidays);
    }

    // GET: Holiday/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Holiday/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Month,Day,Description")] Holiday holiday)
    {
        if (ModelState.IsValid)
        {
            // Kiểm tra tính hợp lệ của ngày/tháng
            try
            {
                DateTime.DaysInMonth(2024, holiday.Month); // kiểm tra tháng hợp lệ
                int maxDay = DateTime.DaysInMonth(2024, holiday.Month);
                if (holiday.Day > maxDay)
                {
                    ModelState.AddModelError("Day", $"Tháng {holiday.Month} chỉ có tối đa {maxDay} ngày.");
                    return View(holiday);
                }
            }
            catch
            {
                ModelState.AddModelError("Month", "Tháng không hợp lệ.");
                return View(holiday);
            }

            _context.Add(holiday);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm ngày nghỉ lễ thành công.";
            return RedirectToAction(nameof(Index));
        }
        return View(holiday);
    }

    // GET: Holiday/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var holiday = await _context.Holidays.FindAsync(id);
        if (holiday == null)
        {
            return NotFound();
        }
        return View(holiday);
    }

    // POST: Holiday/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Month,Day,Description")] Holiday holiday)
    {
        if (id != holiday.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Kiểm tra tính hợp lệ của ngày/tháng
            try
            {
                int maxDay = DateTime.DaysInMonth(2024, holiday.Month);
                if (holiday.Day > maxDay)
                {
                    ModelState.AddModelError("Day", $"Tháng {holiday.Month} chỉ có tối đa {maxDay} ngày.");
                    return View(holiday);
                }
            }
            catch
            {
                ModelState.AddModelError("Month", "Tháng không hợp lệ.");
                return View(holiday);
            }

            try
            {
                _context.Update(holiday);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật ngày nghỉ lễ thành công.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HolidayExists(holiday.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(holiday);
    }

    // POST: Holiday/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var holiday = await _context.Holidays.FindAsync(id);
        if (holiday != null)
        {
            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa ngày nghỉ lễ thành công.";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool HolidayExists(int id)
    {
        return _context.Holidays.Any(e => e.Id == id);
    }
}
