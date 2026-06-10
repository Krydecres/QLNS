using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PositionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PositionsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Positions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Position>>> GetPositions()
    {
        return await _context.Positions.ToListAsync();
    }

    // GET: api/Positions/5
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetPosition(int id)
    {
        var position = await _context.Positions.FindAsync(id);

        if (position == null)
        {
            return NotFound();
        }

        var employees = await _context.Employees
            .Where(e => e.PositionId == id)
            .Include(e => e.Department)
            .AsNoTracking()
            .ToListAsync();

        return new {
            position = position,
            employees = employees
        };
    }

    // PUT: api/Positions/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPosition(int id, Position position)
    {
        if (id != position.Id)
        {
            return BadRequest();
        }

        _context.Entry(position).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PositionExists(id))
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

    // POST: api/Positions
    [HttpPost]
    public async Task<ActionResult<Position>> PostPosition(Position position)
    {
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetPosition", new { id = position.Id }, position);
    }

    // DELETE: api/Positions/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePosition(int id)
    {
        var position = await _context.Positions.FindAsync(id);
        if (position == null)
        {
            return NotFound();
        }

        _context.Positions.Remove(position);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool PositionExists(int id)
    {
        return _context.Positions.Any(e => e.Id == id);
    }
}
