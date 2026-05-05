using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace frontend.Services;

/// <summary>
/// บริการเรียก API แบบรวมศูนย์ — แนบ Token อัตโนมัติทุก Request และ Redirect ไปหน้า Login เมื่อ 401
/// ใช้แทนการเรียก HttpClient โดยตรงใน Component ต่างๆ
/// </summary>
public class ApiClientService
{
    private readonly HttpClient _http;
    private readonly AuthService _authService;
    private readonly NavigationManager _navManager;

    public ApiClientService(HttpClient http, AuthService authService, NavigationManager navManager)
    {
        _http = http;
        _authService = authService;
        _navManager = navManager;
    }

    /// <summary>
    /// แนบ JWT Token เข้า Header ของทุก Request อัตโนมัติ — ดึง Token จาก AuthService
    /// </summary>
    private async Task EnsureTokenAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    /// <summary>
    /// ตรวจสอบสถานะ Response — Redirect ไป Login ถ้า 401 Unauthorized
    /// </summary>
    private void HandleError(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _navManager.NavigateTo("/login", true);
        }
    }

    /// <summary>เรียก GET แล้วแปลง JSON เป็น Object ที่ต้องการ</summary>
    public async Task<T?> GetFromJsonAsync<T>(string url)
    {
        await EnsureTokenAsync();
        try
        {
            var response = await _http.GetAsync(url);
            HandleError(response);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _navManager.NavigateTo("/login", true);
            return default;
        }
        catch
        {
            throw; // ปล่อย Error อื่นขึ้นไป
        }
    }

    /// <summary>เรียก GET แล้วคืน HttpResponseMessage ดิบ</summary>
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        await EnsureTokenAsync();
        var response = await _http.GetAsync(url);
        HandleError(response);
        return response;
    }

    /// <summary>เรียก POST พร้อมข้อมูลแบบ JSON</summary>
    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T value)
    {
        await EnsureTokenAsync();
        var response = await _http.PostAsJsonAsync(url, value);
        HandleError(response);
        return response;
    }

    /// <summary>เรียก POST พร้อม HttpContent (ใช้สำหรับอัปโหลดไฟล์)</summary>
    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        await EnsureTokenAsync();
        var response = await _http.PostAsync(url, content);
        HandleError(response);
        return response;
    }

    /// <summary>เรียก PUT พร้อมข้อมูลแบบ JSON (สำหรับอัปเดตข้อมูล)</summary>
    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T value)
    {
        await EnsureTokenAsync();
        var response = await _http.PutAsJsonAsync(url, value);
        HandleError(response);
        return response;
    }

    /// <summary>เรียก DELETE</summary>
    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        await EnsureTokenAsync();
        var response = await _http.DeleteAsync(url);
        HandleError(response);
        return response;
    }
}
