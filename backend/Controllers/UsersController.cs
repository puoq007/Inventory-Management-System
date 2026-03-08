using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserAccount>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpGet("{employeeId}")]
    public async Task<ActionResult<UserAccount>> GetUser(string employeeId)
    {
        var user = await _context.Users.FindAsync(employeeId);
        if (user == null)
        {
            return NotFound();
        }
        return user;
    }

    [HttpPost]
    public async Task<ActionResult<UserAccount>> PostUser(UserAccount userAccount)
    {
        _context.Users.Add(userAccount);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { employeeId = userAccount.EmployeeId }, userAccount);
    }

    [HttpPut("{employeeId}")]
    public async Task<IActionResult> PutUser(string employeeId, UserAccount userAccount)
    {
        if (employeeId != userAccount.EmployeeId)
        {
            return BadRequest();
        }

        _context.Entry(userAccount).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(employeeId))
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

    [HttpDelete("{employeeId}")]
    public async Task<IActionResult> DeleteUser(string employeeId)
    {
        var user = await _context.Users.FindAsync(employeeId);
        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserAccount>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (user == null || user.Password != request.Password)
        {
            return Unauthorized();
        }

        // Return user without password
        return Ok(new UserAccount
        {
            EmployeeId = user.EmployeeId,
            Name = user.Name,
            Role = user.Role
        });
    }

    private bool UserExists(string employeeId)
    {
        return _context.Users.Any(e => e.EmployeeId == employeeId);
    }
}

public class LoginRequest
{
    public string EmployeeId { get; set; } = "";
    public string Password { get; set; } = "";
}
