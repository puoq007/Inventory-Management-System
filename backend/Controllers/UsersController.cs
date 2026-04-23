using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

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
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserAccount>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpGet("{employeeId}")]
    public async Task<ActionResult<UserAccount>> GetUser(string employeeId)
    {
        var user = await _context.Users.FindAsync(employeeId);
        if (user == null) return NotFound();
        return user;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserAccount>> PostUser(UserAccount userAccount)
    {
        // Hash password if provided
        if (!string.IsNullOrEmpty(userAccount.Password))
            userAccount.Password = BCrypt.Net.BCrypt.HashPassword(userAccount.Password);

        _context.Users.Add(userAccount);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUser), new { employeeId = userAccount.EmployeeId }, userAccount);
    }

    [HttpPut("{employeeId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutUser(string employeeId, UserAccount userAccount)
    {
        if (employeeId != userAccount.EmployeeId) return BadRequest();

        var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (existingUser != null && !string.IsNullOrEmpty(existingUser.Name) && existingUser.Name != userAccount.Name)
        {
            var transactions = await _context.Transactions.Where(t => t.User == existingUser.Name).ToListAsync();
            foreach (var t in transactions) t.User = userAccount.Name;
        }

        // Preserve existing hashed password if not being changed
        if (string.IsNullOrEmpty(userAccount.Password) && existingUser != null)
            userAccount.Password = existingUser.Password;

        _context.Entry(userAccount).State = EntityState.Modified;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(employeeId)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{employeeId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string employeeId)
    {
        var user = await _context.Users.FindAsync(employeeId);
        if (user == null) return NotFound();
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (user == null) return Unauthorized();

        // Support both hashed and legacy plaintext passwords (auto-migrate on login)
        bool passwordValid;
        if (!string.IsNullOrEmpty(user.Password) && user.Password.StartsWith("$2"))
        {
            // Already hashed
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        }
        else
        {
            // Legacy plaintext — verify then migrate to hash
            passwordValid = user.Password == request.Password;
            if (passwordValid)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                await _context.SaveChangesAsync();
            }
        }

        if (!passwordValid) return Unauthorized();

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var key = System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "SuperSecretKeyForJigInventorySystem12345!");

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.EmployeeId),
            new(System.Security.Claims.ClaimTypes.Name, user.Name),
            new(System.Security.Claims.ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new
        {
            token = tokenHandler.WriteToken(token),
            user = new UserAccount { EmployeeId = user.EmployeeId, Name = user.Name, Role = user.Role }
        });
    }

    [HttpPut("{employeeId}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(string employeeId, [FromBody] ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(employeeId);
        if (user == null) return NotFound();

        // Verify old password (support both hashed and legacy)
        bool oldValid = (!string.IsNullOrEmpty(user.Password) && user.Password.StartsWith("$2"))
            ? BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password)
            : user.Password == request.OldPassword;

        if (!oldValid) return BadRequest("รหัสผ่านเดิมไม่ถูกต้อง");

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{employeeId}/change-name")]
    [Authorize]
    public async Task<IActionResult> ChangeName(string employeeId, [FromBody] ChangeNameRequest request)
    {
        var user = await _context.Users.FindAsync(employeeId);
        if (user == null) return NotFound();

        string oldName = user.Name;
        user.Name = request.Name;

        if (!string.IsNullOrEmpty(oldName) && oldName != request.Name)
        {
            var transactions = await _context.Transactions.Where(t => t.User == oldName).ToListAsync();
            foreach (var t in transactions) t.User = request.Name;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    private bool UserExists(string employeeId) => _context.Users.Any(e => e.EmployeeId == employeeId);
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class ChangeNameRequest
{
    public string Name { get; set; } = "";
}

public class LoginRequest
{
    public string EmployeeId { get; set; } = "";
    public string Password { get; set; } = "";
}
