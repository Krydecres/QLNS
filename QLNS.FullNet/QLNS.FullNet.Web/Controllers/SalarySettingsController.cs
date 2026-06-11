using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalarySettingsController : Controller
    {
        private readonly AppDbContext _context;

        public SalarySettingsController(AppDbContext context)
        {
            _context = context;
        }

        // ================================================
        // Trang tổng hợp cài đặt lương (Position + Shift)
        // ================================================
        public async Task<IActionResult> Index()
        {
            var positions = await _context.Positions.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            var shifts = await _context.Shifts.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

            ViewBag.Positions = positions;
            ViewBag.Shifts = shifts;
            return View();
        }

        // ==============================================
        // Cập nhật Lương ngày công của Chức vụ (AJAX)
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePositionWage(int positionId, decimal dailyWage)
        {
            var position = await _context.Positions.FindAsync(positionId);
            if (position == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chức vụ.";
                return RedirectToAction(nameof(Index));
            }

            position.DailyWage = dailyWage;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật lương ngày công cho '{position.Name}' thành {dailyWage:N0}đ.";
            return RedirectToAction(nameof(Index));
        }

        // =================================================
        // Cập nhật Hệ số lương của Ca làm việc
        // =================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShiftMultiplier(int shiftId, decimal wageMultiplier)
        {
            var shift = await _context.Shifts.FindAsync(shiftId);
            if (shift == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ca làm việc.";
                return RedirectToAction(nameof(Index));
            }

            shift.WageMultiplier = wageMultiplier;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật hệ số lương cho ca '{shift.Name}' thành {wageMultiplier:0.##}x.";
            return RedirectToAction(nameof(Index));
        }
    }
}
