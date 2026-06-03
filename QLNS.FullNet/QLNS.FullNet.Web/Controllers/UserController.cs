using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System.Security.Claims;

namespace QLNS.FullNet.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly AppDbContext _context;

    private static readonly string[] Roles = ["Admin", "Employee"];

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.AppUsers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.Username.Contains(search) ||
                u.FullName.Contains(search) ||
                u.Email.Contains(search) ||
                u.Role.Contains(search));
        }

        var users = await query
            .OrderBy(u => u.Id)
            .ToListAsync();

        ViewBag.Search = search;

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> EditRole(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        ViewBag.Roles = Roles;

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(int id, string role)
    {
        var user = await _context.AppUsers.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        if (!Roles.Contains(role))
        {
            ModelState.AddModelError("Role", "Role không hợp lệ.");
            ViewBag.Roles = Roles;
            return View(user);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == user.Id.ToString() && user.Role == "Admin" && role != "Admin")
        {
            TempData["ErrorMessage"] = "Bạn không thể tự hạ quyền Admin của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        user.Role = role;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật quyền người dùng thành công.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        var user = await _context.AppUsers.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ModelState.AddModelError("newPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");
            return View(user);
        }

        var passwordHasher = new PasswordHasher<AppUser>();
        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Reset mật khẩu thành công.";

        return RedirectToAction(nameof(Index));
    }
}