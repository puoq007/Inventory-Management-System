using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace backend.Controllers;

/// <summary>
/// Controller จัดการบัญชีผู้ใช้ — CRUD, Login (JWT), เปลี่ยนรหัสผ่าน, เปลี่ยนชื่อ
/// รองรับการย้ายรหัสผ่านแบบ Plaintext เดิมเป็น BCrypt อัตโนมัติ
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>ดึงรายชื่อผู้ใช้ทั้งหมด (สิทธิ์ Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserAccount>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    /// <summary>ดึงผู้ใช้ตาม EmployeeId</summary>
    /// <param name="employeeId">รหัสพนักงาน</param>
    [HttpGet("{employeeId}")]
    public async Task<ActionResult<UserAccount>> GetUser(string employeeId)
    {
        var user = await _context.Users.FindAsync(employeeId);
        if (user == null) return NotFound();
        return user;
    }

    /// <summary>สร้างบัญชีผู้ใช้ใหม่ — Hash รหัสผ่านด้วย BCrypt (สิทธิ์ Admin)</summary>
    /// <param name="userAccount">ข้อมูลผู้ใช้ใหม่</param>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserAccount>> PostUser(UserAccount userAccount)
    {
        // Hash รหัสผ่านถ้ามีการกรอก
        if (!string.IsNullOrEmpty(userAccount.Password))
            userAccount.Password = BCrypt.Net.BCrypt.HashPassword(userAccount.Password);

        _context.Users.Add(userAccount);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUser), new { employeeId = userAccount.EmployeeId }, userAccount);
    }

    /// <summary>อัปเดตข้อมูลผู้ใช้ — ซิงค์ชื่อในธุรกรรมด้วย (สิทธิ์ Admin)</summary>
    /// <param name="employeeId">รหัสพนักงาน</param>
    /// <param name="userAccount">ข้อมูลที่อัปเดต</param>
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

        // เก็บรหัสผ่านเดิมถ้าไม่ได้เปลี่ยน
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

    /// <summary>ลบบัญชีผู้ใช้ (สิทธิ์ Admin)</summary>
    /// <param name="employeeId">รหัสพนักงานที่ต้องการลบ</param>
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

    /// <summary>
    /// ตรวจสอบสิทธิ์และสร้าง JWT Token — รองรับการย้ายรหัสผ่าน Plaintext เป็น BCrypt อัตโนมัติ
    /// </summary>
    /// <param name="request">ข้อมูล Login (EmployeeId + Password)</param>
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (user == null) return Unauthorized();

        // รองรับทั้งรหัสผ่านแบบ Hash และ Plaintext (ย้ายอัตโนมัติตอน Login)
        bool passwordValid;
        if (!string.IsNullOrEmpty(user.Password) && user.Password.StartsWith("$2"))
        {
            // เป็นแบบ Hash อยู่แล้ว
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        }
        else
        {
            // รหัสผ่านเก่าแบบ Plaintext — ตรวจสอบแล้วย้ายเป็น Hash
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

    /// <summary>เปลี่ยนรหัสผ่าน — ตรวจสอบรหัสเดิมก่อน (รองรับทั้ง Hash และ Plaintext)</summary>
    /// <param name="employeeId">รหัสพนักงาน</param>
    /// <param name="request">รหัสผ่านเดิมและใหม่</param>
    [HttpPut("{employeeId}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(string employeeId, [FromBody] ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(employeeId);
        if (user == null) return NotFound();

        // ตรวจสอบรหัสเดิม (รองรับทั้ง Hash และ Plaintext)
        bool oldValid = (!string.IsNullOrEmpty(user.Password) && user.Password.StartsWith("$2"))
            ? BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password)
            : user.Password == request.OldPassword;

        if (!oldValid) return BadRequest("รหัสผ่านเดิมไม่ถูกต้อง");

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>เปลี่ยนชื่อแสดงผล — อัปเดตชื่อในธุรกรรมด้วย</summary>
    /// <param name="employeeId">รหัสพนักงาน</param>
    /// <param name="request">ชื่อใหม่</param>
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

/// <summary>โมเดล Request สำหรับเปลี่ยนรหัสผ่าน</summary>
public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

/// <summary>โมเดล Request สำหรับเปลี่ยนชื่อ</summary>
public class ChangeNameRequest
{
    public string Name { get; set; } = "";
}

/// <summary>โมเดล Request สำหรับ Login</summary>
public class LoginRequest
{
    public string EmployeeId { get; set; } = "";
    public string Password { get; set; } = "";
}
