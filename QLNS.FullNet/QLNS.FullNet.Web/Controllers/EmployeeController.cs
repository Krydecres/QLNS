using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data; // Thay đổi theo namespace AppDbContext của bạn
using QLNS.FullNet.Data.Entities;
using System.Security.Claims;
using QLNS.FullNet.Web.Services;

namespace QLNS.FullNet.Web.Controllers
{
    [Authorize(Roles = "Admin,Employee")] // Cho phép cả Admin và Employee truy cập
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public EmployeeController(AppDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
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
            // Ưu tiên lấy email từ claim. Nếu rỗng (tài khoản không có email), chuyển sang dùng Username
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = User.Identity?.Name;
            }
            
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

        // 7b. DANH SÁCH THÀNH VIÊN PHÒNG BAN (Dành cho Role Employee)
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyDepartment()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Auth");

            var me = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (me == null)
                return NotFound($"Không tìm thấy hồ sơ nhân sự cho tài khoản: {userEmail}.");

            if (me.DepartmentId == null)
            {
                ViewBag.NoDepartment = true;
                return View(new List<Employee>());
            }

            // Lấy tất cả thành viên cùng phòng ban (kể cả bản thân)
            var members = await _context.Employees
                .Include(e => e.Position)
                .Where(e => e.DepartmentId == me.DepartmentId)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.Department = me.Department;
            ViewBag.CurrentEmployeeId = me.Id;
            return View(members);
        }

        // 8. FORM TỰ CHỈNH SỬA HỒ SƠ (Dành cho Nhân viên)
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EditMyProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = User.Identity?.Name;
            }
            
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
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = User.Identity?.Name;
            }
            
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

        // 11. TẢI ẢNH ĐẠI DIỆN LÊN CLOUDINARY
[Authorize(Roles = "Employee")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
{
    if (avatarFile == null || avatarFile.Length == 0)
    {
        TempData["ErrorMessage"] = "Vui lòng chọn một file ảnh.";
        return RedirectToAction("MyProfile");
    }

    var userEmail = User.FindFirstValue(ClaimTypes.Email);

    if (string.IsNullOrEmpty(userEmail))
    {
        userEmail = User.Identity?.Name;
    }

    if (string.IsNullOrEmpty(userEmail))
    {
        return RedirectToAction("Login", "Auth");
    }

    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

    if (employee == null)
    {
        return NotFound("Không tìm thấy hồ sơ nhân viên.");
    }

    try
    {
        var avatarUrl = await _cloudinaryService.UploadImageAsync(avatarFile, "qlns/avatars");

        if (string.IsNullOrEmpty(avatarUrl))
        {
            TempData["ErrorMessage"] = "Upload ảnh thất bại.";
            return RedirectToAction("MyProfile");
        }

        employee.AvatarUrl = avatarUrl;

        _context.Update(employee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật ảnh đại diện thành công.";
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "Lỗi upload ảnh: " + ex.Message;
    }

    return RedirectToAction("MyProfile");
}
    }
}