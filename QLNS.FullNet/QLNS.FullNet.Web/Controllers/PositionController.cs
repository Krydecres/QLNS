using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Controllers;

[Authorize(Roles = "Admin")]
public class PositionController : Controller
{
    private readonly AppDbContext _context;

    public PositionController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Position
    public async Task<IActionResult> Index()
    {
        var positions = await _context.Positions.ToListAsync();
        return View(positions);
    }

    // GET: Position/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Position/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name")] Position position)
    {
        if (ModelState.IsValid)
        {
            _context.Positions.Add(position);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm mới chức vụ thành công.";
            return RedirectToAction(nameof(Index));
        }
        return View(position);
    }

    // GET: Position/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var position = await _context.Positions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (position == null)
        {
            return NotFound();
        }

        var employeesInPosition = await _context.Employees
            .Where(e => e.PositionId == id)
            .Include(e => e.Department)
            .AsNoTracking()
            .ToListAsync();

        ViewData["PositionName"] = position.Name;
        return View(employeesInPosition);
    }

    // GET: Position/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var position = await _context.Positions.FindAsync(id);
        if (position == null) return NotFound();

        return View(position);
    }

    // POST: Position/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Position position)
    {
        if (id != position.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(position);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật chức vụ thành công.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Positions.AnyAsync(e => e.Id == position.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(position);
    }

    // POST: Position/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var position = await _context.Positions.FindAsync(id);
        if (position != null)
        {
            _context.Positions.Remove(position);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa chức vụ thành công.";
        }
        return RedirectToAction(nameof(Index));
    }
}