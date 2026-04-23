using Microsoft.JSInterop;
using System.Text.Json;

namespace frontend.Services;

public class AuthService
{
    private readonly IJSRuntime _jsRuntime;
    
    public string? PendingEmployeeId { get; set; }

    public AuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SetAuthAsync(string employeeId, string role, string name, string token = "")
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_employee", employeeId);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_role", role);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_name", name);
        if (!string.IsNullOrEmpty(token))
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
    }

    public async Task ClearAuthAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_employee");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_role");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_name");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
    }

    public async Task<(string? EmployeeId, string? Role, string? Name)> GetAuthAsync()
    {
        var emp  = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_employee");
        var role = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_role");
        var name = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_name");
        return (emp, role, name);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_token");
    }

    /// <summary>Returns true if the stored JWT token is expired or missing.</summary>
    public async Task<bool> IsTokenExpiredAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return true;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return true;
            var payload = parts[1];
            // Pad base64
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
