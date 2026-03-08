using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JigSpecsController : ControllerBase
{
    private readonly AppDbContext _context;

    public JigSpecsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JigSpec>>> GetJigSpecs()
    {
        return await _context.JigSpecs.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JigSpec>> GetJigSpec(string id)
    {
        var jigSpec = await _context.JigSpecs.FindAsync(id);

        if (jigSpec == null)
        {
            return NotFound();
        }

        return jigSpec;
    }

    [HttpPost]
    public async Task<ActionResult<JigSpec>> PostJigSpec(JigSpec jigSpec)
    {
        _context.JigSpecs.Add(jigSpec);
        UpdatePartMappingsForSpec(jigSpec.Id, jigSpec.PartNumber);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJigSpec), new { id = jigSpec.Id }, jigSpec);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutJigSpec(string id, JigSpec jigSpec)
    {
        if (id != jigSpec.Id)
        {
            // Handle ID change
            if (JigSpecExists(jigSpec.Id))
            {
                return Conflict("A Jig Specification with the new ID already exists.");
            }

            var oldJigSpec = await _context.JigSpecs.FindAsync(id);
            if (oldJigSpec == null)
            {
                return NotFound();
            }

            // Create new record
            var newJigSpec = new JigSpec
            {
                Id = jigSpec.Id,
                Name = jigSpec.Name,
                PartNumber = jigSpec.PartNumber,
                JigRequired = jigSpec.JigRequired,
                Rev = jigSpec.Rev,
                ToyNumber = jigSpec.ToyNumber,
                Week = jigSpec.Week,
                Item = jigSpec.Item,
                PartType = jigSpec.PartType,
                JigType = jigSpec.JigType,
                ToolNo = jigSpec.ToolNo,
                ToolType = jigSpec.ToolType,
                TotalStepPrint = jigSpec.TotalStepPrint,
                UnitAmount = jigSpec.UnitAmount,
                Feed = jigSpec.Feed,
                Scan = jigSpec.Scan,
                PictureUrl = jigSpec.PictureUrl
            };

            _context.JigSpecs.Add(newJigSpec);

            // Fetch dependent PhysicalJigs
            var dependentJigs = await _context.PhysicalJigs
                .Where(pj => pj.SpecId == id)
                .ToListAsync();

            // Update dependent PhysicalJigs to point to new SpecId
            foreach (var pj in dependentJigs)
            {
                pj.SpecId = jigSpec.Id;
            }

            // Remove old PartJigMappings
            var oldMappings = await _context.PartJigMappings.Where(m => m.SpecId == id).ToListAsync();
            _context.PartJigMappings.RemoveRange(oldMappings);

            // Reconcile new mappings
            UpdatePartMappingsForSpec(jigSpec.Id, jigSpec.PartNumber);

            // Remove old record
            _context.JigSpecs.Remove(oldJigSpec);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        _context.Entry(jigSpec).State = EntityState.Modified;
        UpdatePartMappingsForSpec(jigSpec.Id, jigSpec.PartNumber);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!JigSpecExists(id))
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
    public async Task<IActionResult> DeleteJigSpec(string id)
    {
        var jigSpec = await _context.JigSpecs.FindAsync(id);
        if (jigSpec == null)
        {
            return NotFound();
        }

        var oldMappings = await _context.PartJigMappings.Where(m => m.SpecId == id).ToListAsync();
        _context.PartJigMappings.RemoveRange(oldMappings);

        _context.JigSpecs.Remove(jigSpec);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool JigSpecExists(string id)
    {
        return _context.JigSpecs.Any(e => e.Id == id);
    }

    private void UpdatePartMappingsForSpec(string specId, string partNumberString)
    {
        var existingMappings = _context.PartJigMappings.Where(m => m.SpecId == specId).ToList();
        if (existingMappings.Any())
        {
            _context.PartJigMappings.RemoveRange(existingMappings);
        }

        if (!string.IsNullOrWhiteSpace(partNumberString))
        {
            var parts = partNumberString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(p => p.Trim())
                                        .Where(p => !string.IsNullOrEmpty(p))
                                        .Distinct();
            foreach (var part in parts)
            {
                _context.PartJigMappings.Add(new PartJigMapping { PartNumber = part, SpecId = specId });
            }
        }
    }
}
