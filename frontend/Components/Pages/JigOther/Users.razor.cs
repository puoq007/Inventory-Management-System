using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using shared.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using frontend.Services;
using Net.Codecrete.QrCodeGenerator;

namespace frontend.Components.Pages.JigOther;

/// <summary>
/// หน้าจัดการผู้ใช้งาน (User Management) — เพิ่ม แก้ไข ลบ ผู้ใช้งานและพิมพ์ QR Code บัตรพนักงาน
/// เข้าถึงได้เฉพาะ Role: Admin
/// </summary>
public partial class Users : ComponentBase
{

    #region State Fields
    private bool _isLoading = true;
    private bool _showModal = false;
    private bool _isEditMode = false;
    private UserAccount _editingUser = new();
    private string _errorMessage = "";
    private string _currentRole = "";
    private string _searchQuery = "";
    private string _roleFilter = "All";
    private bool _showRoleFilter = false;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalItems = 0;
    private List<UserAccount> _allUsers = new();
    private IEnumerable<UserAccount>? _displayUsers;
    private HashSet<string> _selectedUserIds = new();
    #endregion

    #region QR Code Printing
    /// <summary>
    /// สร้าง QR Code จากรหัสพนักงานแล้วเรียก JS เพื่อพิมพ์สติกเกอร์ QR บัตรพนักงาน
    /// </summary>
    /// <param name="user">ผู้ใช้ที่ต้องการพิมพ์ QR</param>
    private async Task PrintQR(UserAccount user)
    {
        var qr = QrCode.EncodeText(user.EmployeeId, QrCode.Ecc.Medium);
        var svgStr = qr.ToSvgString(4, "#000000", "#ffffff");
        var data = new {
            id = user.EmployeeId,
            name = user.Name,
            role = user.Role
        };
        await JSRuntime.InvokeVoidAsync("printUserQR", svgStr, data);
    }

    /// <summary>
    /// สลับสถานะการเลือกของผู้ใช้รายเดียวตาม Checkbox
    /// </summary>
    private void ToggleSelection(string empId, object? isChecked)
    {
        if (isChecked is bool cb && cb)
            _selectedUserIds.Add(empId);
        else
            _selectedUserIds.Remove(empId);
            
        StateHasChanged();
    }

    /// <summary>
    /// สลับสถานะการเลือกเมื่อคลิกแถวในตาราง
    /// </summary>
    private void ToggleRowSelection(string empId)
    {
        ToggleSelection(empId, !_selectedUserIds.Contains(empId));
    }

    /// <summary>
    /// เลือกหรือยกเลิกการเลือกผู้ใช้ทั้งหมดในหน้าปัจจุบัน
    /// </summary>
    private void ToggleSelectAll(ChangeEventArgs e)
    {
        bool selectAll = (bool)(e.Value ?? false);
        if (selectAll && _displayUsers != null)
        {
            foreach (var user in _displayUsers) _selectedUserIds.Add(user.EmployeeId);
        }
        else if (_displayUsers != null)
        {
            foreach (var user in _displayUsers) _selectedUserIds.Remove(user.EmployeeId);
        }
        StateHasChanged();
    }

    /// <summary>
    /// พิมพ์ QR Code แบบ Batch สำหรับผู้ใช้ที่ถูกเลือกไว้ แล้วล้างการเลือกทั้งหมด
    /// </summary>
    private async Task PrintSelectedQRs()
    {
        if (!_selectedUserIds.Any()) return;

        var qrList = new List<object>();
        foreach (var id in _selectedUserIds)
        {
            var user = _allUsers.FirstOrDefault(u => u.EmployeeId == id);
            if (user == null) continue;
            var qr = QrCode.EncodeText(id, QrCode.Ecc.Medium);
            var svgStr = qr.ToSvgString(4, "#000000", "#ffffff");
            qrList.Add(new {
                svg = svgStr,
                id = id,
                name = user.Name,
                role = user.Role
            });
        }

        await JSRuntime.InvokeVoidAsync("printUserQRs", qrList);
        _selectedUserIds.Clear();
    }

    #endregion

    #region Lifecycle
    /// <summary>
    /// ลงทะเบียน Event Handler สำหรับการเปลี่ยนภาษา
    /// </summary>
    protected override void OnInitialized()
    {
        Lang.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        Lang.OnChange -= StateHasChanged;
    }

    /// <summary>
    /// ตรวจสอบสิทธิ์ผู้ใช้ (Admin เท่านั้น) หลัง Render ครั้งแรก แล้วโหลดข้อมูลผู้ใช้ทั้งหมด
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var auth = await Auth.GetAuthAsync();
            var role = auth.Role ?? "";
            _currentRole = role;

            // ตรวจสอบสิทธิ์ Admin
            if (string.IsNullOrEmpty(role) || role != "Admin")
            {
                NavManager.NavigateTo("/");
                return;
            }

            await LoadUsers();
            _isLoading = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Data Loading & Filtering
    /// <summary>
    /// โหลดรายชื่อผู้ใช้ทั้งหมดจาก API แล้วเรียก FilterData()
    /// </summary>
    private async Task LoadUsers()
    {
        try
        {
            _allUsers = await Api.GetFromJsonAsync<List<UserAccount>>("api/users") ?? new List<UserAccount>();
            FilterData();
        }
        catch (Exception)
        {
            _errorMessage = "โหลดข้อมูลผู้ใช้จาก API ไม่สำเร็จ";
        }
    }

    private void ToggleRoleFilter() { _showRoleFilter = !_showRoleFilter; }

    /// <summary>
    /// ตั้งค่าตัวกรอง Role แล้วกรองข้อมูลใหม่
    /// </summary>
    /// <param name="val">ค่า Role ที่ต้องการกรอง หรือ "All" เพื่อแสดงทั้งหมด</param>
    private void SetRoleFilter(string val)
    {
        _roleFilter = val;
        FilterData();
        _showRoleFilter = false;
    }

    private void ClearAll()
    {
        _searchQuery = "";
        _roleFilter = "All";
        _selectedUserIds.Clear();
        _showRoleFilter = false;
        FilterData();
    }

    /// <summary>
    /// กรองรายชื่อผู้ใช้ตามคำค้นหาและตัวกรอง Role พร้อมคำนวณ Pagination
    /// </summary>
    private void FilterData()
    {
        var lowerQuery = (_searchQuery ?? "").ToLowerInvariant();
        var query = _allUsers.Where(u => 
            (string.IsNullOrWhiteSpace(lowerQuery) || 
             u.EmployeeId.ToLowerInvariant().Contains(lowerQuery) ||
             u.Name.ToLowerInvariant().Contains(lowerQuery) ||
             u.Role.ToLowerInvariant().Contains(lowerQuery)) &&
            (_roleFilter == "All" || u.Role == _roleFilter)
        );

        _totalItems = query.Count();

        // ปรับหน้าปัจจุบันหากเกินขอบเขต
        int maxPage = (int)Math.Ceiling(_totalItems / (double)_pageSize);
        if (maxPage == 0) maxPage = 1;
        if (_currentPage > maxPage) _currentPage = maxPage;

        _displayUsers = query.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
    }

    #endregion

    #region CRUD Operations
    /// <summary>
    /// เปิด Modal สำหรับเพิ่มผู้ใช้ใหม่ — ตั้งค่าเริ่มต้นเป็น Operator
    /// </summary>
    private void OpenAddModal()
    {
        _editingUser = new UserAccount { Role = "Operator" };
        _isEditMode = false;
        _errorMessage = "";
        _showModal = true;
    }

    /// <summary>
    /// เปิด Modal สำหรับแก้ไขผู้ใช้ — สร้าง Copy ข้อมูลเพื่อป้องกันการแก้ไขต้นฉบับ
    /// </summary>
    /// <param name="user">ผู้ใช้ที่ต้องการแก้ไข</param>
    private void OpenEditModal(UserAccount user)
    {
        _editingUser = new UserAccount 
        { 
            EmployeeId = user.EmployeeId, 
            Name = user.Name, 
            Role = user.Role, 
            Password = user.Password 
        };
        _isEditMode = true;
        _errorMessage = "";
        _showModal = true;
    }

    private void CloseModal()
    {
        _showModal = false;
    }

    /// <summary>
    /// บันทึกข้อมูลผู้ใช้ (สร้างใหม่หรืออัปเดต) หลังยืนยันด้วย SweetAlert
    /// ตรวจสอบว่า EmployeeId และ Name ไม่ว่างก่อนบันทึก
    /// </summary>
    private async Task SaveUser()
    {
        var actionName = _isEditMode ? "Edit User" : "Add User";
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirmAction", actionName, "Are you sure you want to save this user?", "Yes, save", "question");
        if (!confirmed) return;

        if (string.IsNullOrWhiteSpace(_editingUser.EmployeeId) || string.IsNullOrWhiteSpace(_editingUser.Name))
        {
            _errorMessage = "ต้องระบุ EmployeeId และ Name";
            return;
        }

        HttpResponseMessage response;

        if (_isEditMode)
        {
            response = await Api.PutAsJsonAsync($"api/users/{_editingUser.EmployeeId}", _editingUser);
        }
        else
        {
            response = await Api.PostAsJsonAsync("api/users", _editingUser);
        }

        if (response.IsSuccessStatusCode)
        {
            await LoadUsers();
            _showModal = false;
            StateHasChanged();
        }
        else
        {
            _errorMessage = "บันทึกผู้ใช้ผ่าน API ไม่สำเร็จ";
        }
    }

    /// <summary>
    /// ลบผู้ใช้หลังยืนยันด้วย SweetAlert — รีโหลดข้อมูลหลังลบสำเร็จ
    /// </summary>
    /// <param name="user">ผู้ใช้ที่ต้องการลบ</param>
    private async Task DeleteUser(UserAccount user)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirmAction", "Delete User", $"Are you sure you want to remove user {user.Name}?", "Yes, delete", "error");
        if (!confirmed) return;

        var response = await Api.DeleteAsync($"api/users/{user.EmployeeId}");
        if (response.IsSuccessStatusCode)
        {
            await LoadUsers();
            StateHasChanged();
        }
        else
        {
            _errorMessage = "ลบผู้ใช้ผ่าน API ไม่สำเร็จ";
        }
    }
    #endregion
}