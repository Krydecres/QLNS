using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace QLNS.FullNet.Web.Controllers.Api
{
    [ApiController]
    [Route("api/departments")]
    public class DepartmentsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartmentsApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/departments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
        {
            var departments = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Id)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name
                })
                .ToListAsync();

            return Ok(departments);
        }

        // GET: /api/departments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
        {
            var department = await _context.Departments
                .AsNoTracking()
                .Where(d => d.Id == id)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name
                })
                .FirstOrDefaultAsync();

            if (department == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy phòng ban."
                });
            }

            return Ok(department);
        }

        // POST: /api/departments
        [HttpPost]
        public async Task<ActionResult<DepartmentDto>> CreateDepartment(DepartmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    message = "Tên phòng ban không được để trống."
                });
            }

            var department = new Department
            {
                Name = request.Name.Trim()
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            var result = new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name
            };

            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, result);
        }

        // PUT: /api/departments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, DepartmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    message = "Tên phòng ban không được để trống."
                });
            }

            var department = await _context.Departments.FindAsync(id);

            if (department == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy phòng ban cần cập nhật."
                });
            }

            department.Name = request.Name.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật phòng ban thành công.",
                data = new DepartmentDto
                {
                    Id = department.Id,
                    Name = department.Name
                }
            });
        }

        // DELETE: /api/departments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy phòng ban cần xóa."
                });
            }

            var hasEmployees = await _context.Employees
                .AnyAsync(e => e.DepartmentId == id);

            if (hasEmployees)
            {
                return BadRequest(new
                {
                    message = "Không thể xóa phòng ban vì vẫn còn nhân viên thuộc phòng ban này."
                });
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DepartmentRequest
    {
        [Required(ErrorMessage = "Tên phòng ban không được để trống.")]
        public string Name { get; set; } = string.Empty;
    }
}