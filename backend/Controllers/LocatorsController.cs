using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocatorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public LocatorsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Locator>>> GetLocators()
    {
        return await _context.Locators.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Locator>> GetLocator(string id)
    {
        var locator = await _context.Locators.FindAsync(id);
        if (locator == null) return NotFound();
        return locator;
    }

    [HttpPost]
    public async Task<ActionResult<Locator>> PostLocator(Locator locator)
    {
        if (string.IsNullOrEmpty(locator.Id))
            locator.Id = Guid.NewGuid().ToString();

        _context.Locators.Add(locator);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLocator), new { id = locator.Id }, locator);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutLocator(string id, Locator locator)
    {
        if (id != locator.Id) return BadRequest();
        _context.Entry(locator).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLocator(string id)
    {
        var locator = await _context.Locators.FindAsync(id);
        if (locator == null) return NotFound();
        _context.Locators.Remove(locator);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
