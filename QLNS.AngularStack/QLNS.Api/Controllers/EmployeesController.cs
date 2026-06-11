using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Employees
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .ToListAsync();
    }

    // GET: api/Employees/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound();
        }

        return employee;
    }

    // PUT: api/Employees/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutEmployee(int id, Employee employee)
    {
        if (id != employee.Id)
        {
            return BadRequest();
        }

        // Lấy thông tin email cũ trước khi update
        var existingEmployee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (existingEmployee == null)
        {
            return NotFound();
        }

        _context.Entry(employee).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();

            // Nếu email thay đổi → cập nhật AppUser.Email tương ứng để giữ liên kết
            if (!string.Equals(existingEmployee.Email, employee.Email, StringComparison.OrdinalIgnoreCase))
            {
                var appUser = await _context.AppUsers
                    .FirstOrDefaultAsync(u => u.Email == existingEmployee.Email);
                if (appUser != null)
                {
                    appUser.Email = employee.Email;
                    await _context.SaveChangesAsync();
                }
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EmployeeExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Employees
    [HttpPost]
    public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetEmployee", new { id = employee.Id }, employee);
    }

    // DELETE: api/Employees/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool EmployeeExists(int id)
    {
        return _context.Employees.Any(e => e.Id == id);
    }

    // GET: api/Employees/my-profile
    [HttpGet("my-profile")]
    public async Task<ActionResult<Employee>> GetMyProfile([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username)) return BadRequest("Username is required");

        var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
        if (appUser == null) return NotFound("User not found");

        // Tìm theo email của AppUser, fallback username nếu không có email
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e =>
                (!string.IsNullOrEmpty(appUser.Email) && e.Email == appUser.Email) ||
                e.Email == appUser.Username);

        if (employee == null) return NotFound("Employee profile not found");

        return employee;
    }

    // GET: api/Employees/my-department
    [HttpGet("my-department")]
    public async Task<ActionResult<object>> GetMyDepartment([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username)) return BadRequest("Username is required");

        var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
        if (appUser == null) return NotFound("User not found");

        // Tìm theo email của AppUser, fallback username nếu không có email
        var me = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e =>
                (!string.IsNullOrEmpty(appUser.Email) && e.Email == appUser.Email) ||
                e.Email == appUser.Username);

        if (me == null) return NotFound("Employee profile not found");

        if (me.DepartmentId == null)
        {
            return new { Department = (object)null, Members = new List<Employee>() };
        }

        var members = await _context.Employees
            .Include(e => e.Position)
            .Where(e => e.DepartmentId == me.DepartmentId)
            .OrderBy(e => e.FullName)
            .ToListAsync();

        return new { Department = me.Department, Members = members };
    }

    // POST: api/Employees/request-update
    [HttpPost("request-update")]
    public async Task<IActionResult> RequestProfileUpdate([FromBody] ProfileUpdateRequest requestDto)
    {
        // Nhận EmployeeId, NewPhoneNumber, NewDateOfBirth từ requestDto
        requestDto.Status = "Pending";
        requestDto.CreatedAt = DateTime.Now;

        _context.ProfileUpdateRequests.Add(requestDto);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Yêu cầu cập nhật đã được gửi." });
    }

    // GET: api/Employees/pending-updates
    [HttpGet("pending-updates")]
    public async Task<ActionResult<IEnumerable<ProfileUpdateRequest>>> GetPendingUpdates()
    {
        return await _context.ProfileUpdateRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    // POST: api/Employees/approve-update/5
    [HttpPost("approve-update/{id}")]
    public async Task<IActionResult> ApproveUpdate(int id, [FromQuery] bool isApproved)
    {
        var request = await _context.ProfileUpdateRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null || request.Status != "Pending")
        {
            return BadRequest("Yêu cầu không hợp lệ hoặc đã được xử lý.");
        }

        request.Status = isApproved ? "Approved" : "Rejected";

        if (isApproved && request.Employee != null)
        {
            request.Employee.PhoneNumber = request.NewPhoneNumber;
            request.Employee.DateOfBirth = request.NewDateOfBirth;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = isApproved ? "Đã phê duyệt yêu cầu." : "Đã từ chối yêu cầu." });
    }
}
