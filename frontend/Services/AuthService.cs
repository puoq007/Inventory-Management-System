using Microsoft.JSInterop;

namespace frontend.Services;

public class AuthService
{
    private readonly IJSRuntime _jsRuntime;
    
    // In-memory pending state
    public string? PendingEmployeeId { get; set; }

    public AuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SetAuthAsync(string employeeId, string role, string name)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_employee", employeeId);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_role", role);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_name", name);
    }

    public async Task ClearAuthAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_employee");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_role");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_name");
    }

    public async Task<(string? EmployeeId, string? Role, string? Name)> GetAuthAsync()
    {
        var emp = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_employee");
        var role = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_role");
        var name = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_name");
        return (emp, role, name);
    }
}
