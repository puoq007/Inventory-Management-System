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

    /// <summary>
    /// Renames a Locator ID: creates new record, migrates PhysicalJig references, then deletes old record.
    /// </summary>
    [HttpPost("{oldId}/rename")]
    public async Task<IActionResult> RenameLocator(string oldId, [FromBody] Locator updatedLocator)
    {
        var existing = await _context.Locators.FindAsync(oldId);
        if (existing == null) return NotFound($"Locator '{oldId}' not found.");

        var newId = updatedLocator.Id?.Trim();
        if (string.IsNullOrEmpty(newId))
            return BadRequest("New ID cannot be empty.");

        if (newId != oldId)
        {
            var conflict = await _context.Locators.FindAsync(newId);
            if (conflict != null)
                return BadRequest($"Locator ID '{newId}' already exists.");
        }

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (newId != oldId)
            {
                // 1. Create new locator with new ID + updated fields
                var newLocator = new Locator
                {
                    Id       = newId,
                    Site     = updatedLocator.Site,
                    Cabinet  = updatedLocator.Cabinet,
                    Shelf    = updatedLocator.Shelf,
                    Position = updatedLocator.Position,
                    Type     = updatedLocator.Type
                };

                _context.Locators.Add(newLocator);
                await _context.SaveChangesAsync();

                // 2. Migrate PhysicalJigs references
                var jigsToUpdate = await _context.PhysicalJigs
                    .Where(j => j.LocatorId == oldId || j.HomeLocatorId == oldId)
                    .ToListAsync();

                foreach (var jig in jigsToUpdate)
                {
                    if (jig.LocatorId     == oldId) jig.LocatorId     = newId;
                    if (jig.HomeLocatorId == oldId) jig.HomeLocatorId = newId;
                }
                await _context.SaveChangesAsync();

                // 3. Delete old locator
                _context.Locators.Remove(existing);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return Ok(newLocator);
            }
            else
            {
                // ID unchanged — just update other fields
                existing.Site     = updatedLocator.Site;
                existing.Cabinet  = updatedLocator.Cabinet;
                existing.Shelf    = updatedLocator.Shelf;
                existing.Position = updatedLocator.Position;
                existing.Type     = updatedLocator.Type;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok(existing);
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Rename failed: {ex.Message}");
        }
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
