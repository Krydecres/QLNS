using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QLNS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeaveRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. LẤY DANH SÁCH XIN NGHỈ CỦA TÔI (Employee)
        // =======================================================
        [HttpGet("my-leaves/{username}")]
        public async Task<IActionResult> GetMyLeaves(string username)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == appUser.Email);
            if (employee == null) return NotFound(new { message = "Hồ sơ nhân viên không tồn tại." });

            var leaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employee.Id)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new {
                    l.Id,
                    l.StartDate,
                    l.EndDate,
                    l.Reason,
                    l.Status,
                    l.CreatedAt,
                    l.ApprovalNote,
                    ApprovedByName = l.ApprovedBy != null ? l.ApprovedBy.FullName : null
                })
                .ToListAsync();

            return Ok(leaves);
        }

        public class LeaveRequestCreateDto
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        // =======================================================
        // 2. TẠO MỚI ĐƠN XIN NGHỈ (Employee)
        // =======================================================
        [HttpPost("my-leaves/{username}")]
        public async Task<IActionResult> CreateLeaveRequest(string username, [FromBody] LeaveRequestCreateDto model)
        {
            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == appUser.Email);
            if (employee == null) return NotFound(new { message = "Hồ sơ nhân viên không tồn tại." });

            if (model.EndDate < model.StartDate)
            {
                return BadRequest(new { message = "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu." });
            }

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason,
                Status = LeaveRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gửi đơn xin nghỉ phép thành công!" });
        }

        // =======================================================
        // 3. LẤY DANH SÁCH CHO QUẢN TRỊ VIÊN (Admin)
        // =======================================================
        [HttpGet("approval")]
        public async Task<IActionResult> GetApprovalList()
        {
            var requests = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.ApprovedBy)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new {
                    l.Id,
                    l.StartDate,
                    l.EndDate,
                    l.Reason,
                    l.Status,
                    l.CreatedAt,
                    l.ApprovalNote,
                    EmployeeName = l.Employee != null ? l.Employee.FullName : null,
                    ApprovedByName = l.ApprovedBy != null ? l.ApprovedBy.FullName : null
                })
                .ToListAsync();

            return Ok(requests);
        }

        public class ProcessRequestDto
        {
            public LeaveRequestStatus Status { get; set; }
            public string? Note { get; set; }
            public string AdminUsername { get; set; } = string.Empty;
        }

        // =======================================================
        // 4. DUYỆT / TỪ CHỐI ĐƠN (Admin)
        // =======================================================
        [HttpPut("approval/{id}")]
        public async Task<IActionResult> ProcessRequest(int id, [FromBody] ProcessRequestDto model)
        {
            var request = await _context.LeaveRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn xin nghỉ phép." });
            }

            if (request.Status != LeaveRequestStatus.Pending)
            {
                return BadRequest(new { message = "Đơn này đã được xử lý trước đó." });
            }

            var adminUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == model.AdminUsername);
            if (adminUser != null)
            {
                request.ApprovedById = adminUser.Id;
            }

            request.Status = model.Status;
            request.ApprovalNote = model.Note;

            await _context.SaveChangesAsync();

            string action = model.Status == LeaveRequestStatus.Approved ? "đã duyệt" : "từ chối";
            return Ok(new { message = $"Đã {action} đơn nghỉ phép." });
        }
    }
}
