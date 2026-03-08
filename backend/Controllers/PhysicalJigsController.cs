using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhysicalJigsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PhysicalJigsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PhysicalJig>>> GetPhysicalJigs()
    {
        return await _context.PhysicalJigs.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PhysicalJig>> GetPhysicalJig(string id)
    {
        var jig = await _context.PhysicalJigs.FindAsync(id);

        if (jig == null)
        {
            return NotFound();
        }

        return jig;
    }

    [HttpPost]
    public async Task<ActionResult<PhysicalJig>> PostPhysicalJig(PhysicalJig jig)
    {
        _context.PhysicalJigs.Add(jig);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPhysicalJig), new { id = jig.Id }, jig);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutPhysicalJig(string id, PhysicalJig jig)
    {
        if (id != jig.Id)
        {
            return BadRequest();
        }

        _context.Entry(jig).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PhysicalJigExists(id))
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePhysicalJig(string id)
    {
        var jig = await _context.PhysicalJigs.FindAsync(id);
        if (jig == null)
        {
            return NotFound();
        }

        _context.PhysicalJigs.Remove(jig);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool PhysicalJigExists(string id)
    {
        return _context.PhysicalJigs.Any(e => e.Id == id);
    }
}
