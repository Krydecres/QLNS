using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Controllers;

[Authorize(Roles = "Admin")]
public class DepartmentController : Controller
{
    private readonly AppDbContext _context;

    // Khai báo và tiêm AppDbContext thông qua Constructor
    public DepartmentController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Truy vấn danh sách phòng ban
    public async Task<IActionResult> Index()
    {
        var departments = await _context.Departments.ToListAsync();
        return View(departments);
    }

    // GET: Hiển thị form Thêm mới
    public IActionResult Create()
    {
        return View();
    }

    // POST: Xử lý lưu form Thêm mới
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name")] Department department)
    {
        if (ModelState.IsValid)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm mới phòng ban thành công.";
            return RedirectToAction(nameof(Index));
        }
        return View(department);
    }

    // GET: Department/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var department = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (department == null)
        {
            return NotFound();
        }

        var employeesInDepartment = await _context.Employees
            .Where(e => e.DepartmentId == id)
            .Include(e => e.Position)
            .AsNoTracking()
            .ToListAsync();

        ViewData["DepartmentName"] = department.Name;
        return View(employeesInDepartment);
    }

    // GET: Hiển thị form Chỉnh sửa
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound();

        return View(department);
    }

    // POST: Xử lý lưu form Chỉnh sửa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Department department)
    {
        if (id != department.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật phòng ban thành công.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Departments.AnyAsync(e => e.Id == department.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(department);
    }

    // POST: Xử lý Xóa phòng ban
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department != null)
        {
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa phòng ban thành công.";
        }
        return RedirectToAction(nameof(Index));
    }
}