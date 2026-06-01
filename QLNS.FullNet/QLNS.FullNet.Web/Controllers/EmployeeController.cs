using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data; // Thay đổi theo namespace AppDbContext của bạn
using QLNS.FullNet.Data.Entities;
using System.Security.Claims;

namespace QLNS.FullNet.Web.Controllers
{
    [Authorize(Roles = "Admin,Employee")] // Cho phép cả Admin và Employee truy cập
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // 1. HIỂN THỊ DANH SÁCH NHÂN VIÊN
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            // Sử dụng Include để lấy kèm thông tin Phòng ban (Foreign Key)
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position) // Thêm Include cho Position
                .ToListAsync();
            return View(employees);
        }

        // 2. FORM THÊM MỚI NHÂN VIÊN (GET)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Lấy danh sách phòng ban đổ vào DropdownList (Select) trên Giao diện
            ViewBag.DepartmentId = new SelectList(_context.Departments, "Id", "Name");
            ViewBag.PositionId = new SelectList(_context.Positions, "Id", "Name"); // Thêm cho Position
            return View();
        }

        // 3. XỬ LÝ THÊM MỚI NHÂN VIÊN (POST)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm mới nhân viên thành công.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.DepartmentId = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
            ViewBag.PositionId = new SelectList(_context.Positions, "Id", "Name", employee.PositionId); // Thêm cho Position
            return View(employee);
        }

        // 4. FORM SỬA THÔNG TIN NHÂN VIÊN (GET)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            ViewBag.DepartmentId = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
            ViewBag.PositionId = new SelectList(_context.Positions, "Id", "Name", employee.PositionId); // Thêm cho Position
            return View(employee);
        }

        // 5. XỬ LÝ SỬA THÔNG TIN (POST)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.DepartmentId = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
            ViewBag.PositionId = new SelectList(_context.Positions, "Id", "Name", employee.PositionId); // Thêm cho Position
            return View(employee);
        }

        // 6. XỬ LÝ XÓA NHÂN VIÊN (POST - Gọi qua Form hoặc Ajax để bảo mật)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa nhân viên thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }

        // 7. XEM HỒ SƠ CÁ NHÂN (Dành cho Role Employee)
        public async Task<IActionResult> MyProfile()
        {
            // Ưu tiên lấy email từ claim, nếu không có thì lấy username
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            
            if (string.IsNullOrEmpty(userEmail)) 
            {
                return RedirectToAction("Login", "Auth");
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null) return NotFound($"Không tìm thấy hồ sơ nhân sự nào được liên kết với tài khoản: {userEmail}. Vui lòng liên hệ Admin để tạo hồ sơ nhân viên với Email tương ứng.");

            return View(employee);
        }

        // 8. FORM TỰ CHỈNH SỬA HỒ SƠ (Dành cho Nhân viên)
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EditMyProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);
            
            if (employee == null) return NotFound();

            return View(employee);
        }

        // 9. XỬ LÝ LƯU YÊU CẦU CHỈNH SỬA (POST)
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMyProfile(int id, string phoneNumber, DateTime? dateOfBirth)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);
            
            if (employee == null || employee.Id != id) return NotFound();

            // Lưu vào bảng Yêu cầu thay đổi thay vì cập nhật trực tiếp vào Employee
            var request = new ProfileUpdateRequest
            {
                EmployeeId = employee.Id,
                NewPhoneNumber = phoneNumber,
                NewDateOfBirth = dateOfBirth,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            
            _context.ProfileUpdateRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Yêu cầu cập nhật thông tin đã được gửi đến Quản trị viên chờ phê duyệt.";
            return RedirectToAction(nameof(MyProfile));
        }

        // 10. DANH SÁCH YÊU CẦU CẬP NHẬT (Dành cho Admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PendingProfileUpdates()
        {
            var requests = await _context.ProfileUpdateRequests
                .Include(r => r.Employee)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
                
            return View(requests);
        }

        // 11. XỬ LÝ PHÊ DUYỆT YÊU CẦU (Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProfileUpdate(int id, bool isApproved)
        {
            var request = await _context.ProfileUpdateRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (request != null && request.Status == "Pending")
            {
                request.Status = isApproved ? "Approved" : "Rejected";
                
                if (isApproved && request.Employee != null)
                {
                    request.Employee.PhoneNumber = request.NewPhoneNumber;
                    request.Employee.DateOfBirth = request.NewDateOfBirth;
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = isApproved ? "Đã phê duyệt yêu cầu cập nhật hồ sơ." : "Đã từ chối yêu cầu cập nhật hồ sơ.";
            }
            return RedirectToAction(nameof(PendingProfileUpdates));
        }
    }
}