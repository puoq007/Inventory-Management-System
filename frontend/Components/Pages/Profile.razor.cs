using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using shared.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using frontend.Services;
using Net.Codecrete.QrCodeGenerator;

namespace frontend.Components.Pages;

/// <summary>
/// หน้าโปรไฟล์ผู้ใช้ — แสดงข้อมูลส่วนตัว, เปลี่ยนชื่อ, เปลี่ยนรหัสผ่าน
/// เข้าถึงได้ทุก Role (ต้อง Login ก่อน)
/// </summary>
public partial class Profile : ComponentBase
{

    private UserAccount? _user;
    private bool _loading = true;
    private string _oldPassword = "";
    private string _newPassword = "";
    private string _confirmPassword = "";
    private string _passwordMessage = "";
    private bool _passwordSuccess = false;
    private bool _changingPassword = false;
    private bool _editingName = false;
    private string _editName = "";
    private string _nameMessage = "";
    private bool _nameSuccess = false;
    private bool _showOld = false;
    private bool _showNew = false;
    private bool _showConfirm = false;

    protected override void OnInitialized()
    {
        Lang.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        Lang.OnChange -= StateHasChanged;
    }

    /// <summary>โหลดข้อมูลผู้ใช้จาก API หลัง Render ครั้งแรก</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var auth = await Auth.GetAuthAsync();
            if (string.IsNullOrEmpty(auth.EmployeeId))
            {
                NavManager.NavigateTo("/login");
                return;
            }

            try
            {
                _user = await Api.GetFromJsonAsync<UserAccount>($"api/users/{auth.EmployeeId}");
            }
            catch { }
            
            _loading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// เปลี่ยนรหัสผ่าน — ตรวจสอบรหัสเดิม, ยืนยันรหัสใหม่, และส่งไป API
    /// </summary>
    private async Task ChangePassword()
    {
        _passwordMessage = "";
        
        if (string.IsNullOrWhiteSpace(_oldPassword))
        {
            _passwordMessage = Lang.T("กรุณากรอกรหัสผ่านเดิม", "Please enter current password");
            _passwordSuccess = false;
            return;
        }
        if (string.IsNullOrWhiteSpace(_newPassword))
        {
            _passwordMessage = Lang.T("กรุณากรอกรหัสผ่านใหม่", "Please enter new password");
            _passwordSuccess = false;
            return;
        }
        if (_newPassword != _confirmPassword)
        {
            _passwordMessage = Lang.T("รหัสผ่านใหม่ไม่ตรงกัน", "New passwords do not match");
            _passwordSuccess = false;
            return;
        }

        _changingPassword = true;
        try
        {
            var response = await Api.PutAsJsonAsync($"api/users/{_user!.EmployeeId}/change-password", new { OldPassword = _oldPassword, NewPassword = _newPassword });
            if (response.IsSuccessStatusCode)
            {
                _passwordMessage = Lang.T("เปลี่ยนรหัสผ่านสำเร็จ!", "Password changed successfully!");
                _passwordSuccess = true;
                _oldPassword = "";
                _newPassword = "";
                _confirmPassword = "";
            }
            else
            {
                _passwordMessage = Lang.T("รหัสผ่านเดิมไม่ถูกต้อง", "Current password is incorrect");
                _passwordSuccess = false;
            }
        }
        catch
        {
            _passwordMessage = Lang.T("เกิดข้อผิดพลาด กรุณาลองใหม่", "An error occurred, please try again");
            _passwordSuccess = false;
        }
        _changingPassword = false;
    }

    /// <summary>เปิดโหมดแก้ไขชื่อ</summary>
    private void StartEditName()
    {
        _editName = _user?.Name ?? "";
        _editingName = true;
        _nameMessage = "";
    }

    /// <summary>บันทึกชื่อใหม่ผ่าน API และอัปเดต localStorage</summary>
    private async Task SaveName()
    {
        if (string.IsNullOrWhiteSpace(_editName))
        {
            _nameMessage = Lang.T("กรุณากรอกชื่อ", "Please enter a name");
            _nameSuccess = false;
            return;
        }

        try
        {
            var response = await Api.PutAsJsonAsync($"api/users/{_user!.EmployeeId}/change-name", new { Name = _editName });
            if (response.IsSuccessStatusCode)
            {
                _user.Name = _editName;
                _editingName = false;
                _nameMessage = Lang.T("เปลี่ยนชื่อสำเร็จ!", "Name changed!");
                _nameSuccess = true;
                await Auth.SetAuthAsync(_user.EmployeeId, _user.Role, _user.Name);
            }
            else
            {
                _nameMessage = Lang.T("เปลี่ยนชื่อไม่สำเร็จ", "Failed to change name");
                _nameSuccess = false;
            }
        }
        catch
        {
            _nameMessage = Lang.T("เกิดข้อผิดพลาด", "An error occurred");
            _nameSuccess = false;
        }
    }

    /// <summary>แปลง Role เป็นชื่อแสดงผลตามภาษา</summary>
    private string GetRoleDisplay(string role) => role switch
    {
        "Admin" => Lang.T("ผู้ดูแลระบบ", "Administrator"),
        "Engineer" => Lang.T("วิศวกร", "Engineer"),
        "ProdLead" => Lang.T("หัวหน้าฝ่ายผลิต", "Production Lead"),
        "Operator" => Lang.T("พนักงานปฏิบัติการ", "Operator"),
        _ => role
    };

    /// <summary>คืนค่า CSS Class สำหรับ Role Badge</summary>
    private string GetRoleBadgeClass(string role) => role switch
    {
        "Admin" => "bg-purple-900/40 text-purple-400 border-purple-800",
        "Engineer" => "bg-blue-900/40 text-blue-400 border-blue-800",
        "ProdLead" => "bg-amber-900/40 text-amber-400 border-amber-800",
        "Operator" => "bg-emerald-900/40 text-emerald-400 border-emerald-800",
        _ => "bg-slate-700 text-slate-400 border-slate-600"
    };

}