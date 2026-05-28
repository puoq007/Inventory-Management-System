using Microsoft.JSInterop;
using System.Text.Json;

namespace frontend.Services;

/// <summary>
/// บริการจัดการสิทธิ์ผู้ใช้ — เก็บ/อ่านข้อมูลผู้ใช้และ JWT Token ผ่าน localStorage
/// ใช้ร่วมกับ ApiClientService เพื่อแนบ Token ทุก Request
/// </summary>
public class AuthService
{
    private readonly IJSRuntime _jsRuntime;
    
    /// <summary>เก็บรหัสพนักงานที่รอกรอกรหัสผ่าน (ใช้สำหรับขั้นตอน Login 2 ขั้นตอน)</summary>
    public string? PendingEmployeeId { get; set; }

    public AuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// บันทึกข้อมูลผู้ใช้ลง localStorage หลัง Login สำเร็จ
    /// </summary>
    public async Task SetAuthAsync(string employeeId, string role, string name, string token = "")
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_employee", employeeId);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_role", role);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_name", name);
        if (!string.IsNullOrEmpty(token))
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
    }

    /// <summary>ล้างข้อมูลผู้ใช้จาก localStorage เมื่อ Logout</summary>
    public async Task ClearAuthAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_employee");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_role");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_name");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
    }

    /// <summary>ดึงข้อมูลผู้ใช้ที่เก็บไว้ (EmployeeId, Role, Name)</summary>
    public async Task<(string? EmployeeId, string? Role, string? Name)> GetAuthAsync()
    {
        var emp  = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_employee");
        var role = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_role");
        var name = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_name");
        
        if (string.IsNullOrEmpty(role))
        {
            role = "Guest";
            name = "Guest User";
        }
        
        return (emp, role, name);
    }

    /// <summary>ดึง JWT Token จาก localStorage</summary>
    public async Task<string?> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_token");
    }

    /// <summary>ตรวจสอบว่า JWT Token หมดอายุหรือไม่มี — คืน true หากหมดอายุ/ไม่มี Token</summary>
    public async Task<bool> IsTokenExpiredAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return true;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return true;
            var payload = parts[1];
            // เติม Padding Base64
            payload += new string('=', (4 - payload.Length % 4) % 4);
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("exp", out var expProp))
            {
                var exp = expProp.GetInt64();
                var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
                return expiry <= DateTimeOffset.UtcNow;
            }
            return true;
        }
        catch { return true; }
    }
}
