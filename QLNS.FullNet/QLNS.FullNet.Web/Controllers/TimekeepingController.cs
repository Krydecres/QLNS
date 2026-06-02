using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace QLNS.FullNet.Web.Controllers
{
    [Authorize]
    public class TimekeepingController : Controller
    {
        private readonly AppDbContext _context;

        public TimekeepingController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyAttendance()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null) return NotFound("Hồ sơ nhân viên không tồn tại.");

            var records = await _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            ViewBag.TodayRecord = records.FirstOrDefault(t => t.Date.Date == DateTime.Today);

            return View(records);
        }

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null) return NotFound("Hổ sġ nhân viên không tồn tại.");

            var today = DateTime.Today;
            var record = await _context.Timekeepings.FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Date == today);

            if (record == null)
            {
                record = new Timekeeping
                {
                    EmployeeId = employee.Id,
                    Date = today,
                    CheckInTime = DateTime.Now.TimeOfDay
                };
                _context.Timekeepings.Add(record);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Check-in thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Bạn đã check-in ngày hôm nay rọi.";
            }

            return RedirectToAction(nameof(MyAttendance));
        }

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null) return NotFound("Hổ sġ nhân viên không tồn tại.");

            var today = DateTime.Today;
            var record = await _context.Timekeepings.FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Date == today);

            if (record != null && record.CheckInTime != null)
            {
                if (record.CheckOutTime == null)
                {
                    record.CheckOutTime = DateTime.Now.TimeOfDay; // Corrected to DateTime.Now.TimeOfDay
	                record.CheckOutTime = DateTime.Now.TimeOfDay;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Check-out thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Bạn đã check-out ngày hôm nay rồi.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Bạn chưa check-in ngày hôm nay.";
            }

            return RedirectToAction(nameof(MyAttendance));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var records = await _context.Timekeepings
                .Include(t => t.Employee)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
            return View(records);
        }

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
                    .FirstOrDefaultAsync(t => t.EmployeeId == model.EmployeeId && t.Date == model.Date);

                if (existing != null)
                {
                    existing.CheckInTime = model.CheckInTime;
                    existing.CheckOutTime = model.CheckOutTime;
                    existing.Note = model.Note;
                    _context.Update(existing);
                }
                else
                {
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