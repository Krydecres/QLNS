using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using QLNS.FullNet.Web.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNS.FullNet.Web.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly AppDbContext _context;

        public LeaveRequestController(AppDbContext context)
        {
            _context = context;
        }

        // --- NHÂN VIÀN ---

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyLeaves()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null) return NotFound("Hồ sơ nhân viên không tồn tại.");

            var leaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employee.Id)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(leaves);
        }

        // --- NGÀY NGHỈ CỦA TÔI ---

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyDaysOff(int? year)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null) return NotFound("Hồ sơ nhân viên không tồn tại.");

            int selectedYear = year ?? DateTime.Now.Year;
            var today = DateTime.Today;

            // Lấy tất cả ngày nghỉ lễ hợp lệ (Month/Day != 0)
            var holidays = await _context.Holidays
                .Where(h => h.Month >= 1 && h.Month <= 12 && h.Day >= 1 && h.Day <= 31)
                .OrderBy(h => h.Month).ThenBy(h => h.Day)
                .ToListAsync();

            // Lấy đơn xin nghỉ đã duyệt của nhân viên trong năm
            var leaveRequests = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employee.Id
                    && l.Status == LeaveRequestStatus.Approved
                    && (l.StartDate.Year == selectedYear || l.EndDate.Year == selectedYear))
                .OrderBy(l => l.StartDate)
                .ToListAsync();

            // Tạo danh sách gộp
            var allDays = new List<DayOffItem>();

            // Thêm ngày nghỉ lễ của năm được chọn
            foreach (var h in holidays)
            {
                var d = h.GetDateForYear(selectedYear);
                allDays.Add(new DayOffItem
                {
                    Type = "Holiday",
                    Date = d,
                    Title = h.Name,
                    Description = h.Description,
                    BadgeClass = "bg-danger",
                    Icon = "bi-star-fill"
                });
            }

            // Thêm đơn xin nghỉ đã duyệt
            foreach (var lr in leaveRequests)
            {
                allDays.Add(new DayOffItem
                {
                    Type = "Leave",
                    Date = lr.StartDate,
                    EndDate = lr.EndDate,
                    Title = "Nghỉ phép: " + lr.Reason,
                    Description = lr.ApprovalNote,
                    BadgeClass = "bg-success",
                    Icon = "bi-calendar-check"
                });
            }

            // Phân loại sắp tới / đã qua
            var upcoming = allDays
                .Where(d => d.Date >= today)
                .OrderBy(d => d.Date)
                .ToList();

            var past = allDays
                .Where(d => d.Date < today)
                .OrderByDescending(d => d.Date)
                .ToList();

            var vm = new MyDaysOffViewModel
            {
                UpcomingDays = upcoming,
                PastDays = past,
                Holidays = holidays,
                LeaveRequests = leaveRequests,
                CurrentYear = selectedYear
            };

            ViewBag.SelectedYear = selectedYear;
            return View(vm);
        }

        [Authorize(Roles = "Employee")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveRequest model)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null) return NotFound("Kồ sơ nhân viên không tồn tại.");

            // Gán thông tin mậc định
            model.EmployeeId = employee.Id;
            model.Status = LeaveRequestStatus.Pending;
            model.CreatedAt = DateTime.Now;

            // Xóa validation errors không cần thiết
            ModelState.Remove("Employee");
            ModelState.Remove("ApprovedBy");

            if (ModelState.IsValid)
            {
                if (model.EndDate < model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bật đầu.");
                    return View(model);
                }

                _context.LeaveRequests.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gửi đơn xin nghỉ phép thàng!";
                return RedirectToAction(nameof(MyLeaves));
            }

            return View(model);
        }

        // --- QUẢN TRỊ VIÂN ---

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approval()
        {
            var requests = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.ApprovedBy)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(int id, LeaveRequestStatus status, string note)
        {
            var request = await _context.LeaveRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound("Không tìm thấy đơn xin nghỉ phép.");
            }

            if (request.Status != LeaveRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Đơn này đá đỳc xỬ lý trước đó.";
                return RedirectToAction(nameof(Approval));
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                request.ApprovedById = userId;
            }

            request.Status = status;
            request.ApprovalNote = note;

            await _context.SaveChangesAsync();
            string action = status == LeaveRequestStatus.Approved ? "đá duyệt" : "từ chôi";
            TempData["SuccessMessage"] = "Đã " + action + " đơn nghỉ phép.";

            return RedirectToAction(nameof(Approval));
        }
    }
}