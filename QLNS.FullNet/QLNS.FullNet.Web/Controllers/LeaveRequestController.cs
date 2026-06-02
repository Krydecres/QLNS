using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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