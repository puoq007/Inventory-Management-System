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
/// หน้าแสดงสถานะจิกทั้งหมด — รองรับการกรองตามสถานะ/สภาพ, สแกน QR ค้นหา, และแจ้งปัญหา
/// เข้าถึงได้ทุก Role
/// </summary>
public partial class CurrentStatus : ComponentBase
{

    [SupplyParameterFromQuery(Name = "filter")]
    public string? Filter { get; set; }

    [SupplyParameterFromQuery(Name = "status")]
    public string? StatusFromQuery { get; set; }

    [SupplyParameterFromQuery(Name = "condition")]
    public string? ConditionFromQuery { get; set; }

    private string _role = "";
    private string _name = "";
    private string _searchQuery = "";
    private System.Threading.CancellationTokenSource? _searchCts;

    /// <summary>รับค่าค้นหาแบบ Debounce 300ms — ลดการเรียก FilterData ซ้ำซ้อน</summary>
    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? "";
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        try
        {
            await Task.Delay(300, _searchCts.Token);
            FilterData();
        }
        catch (TaskCanceledException) { }
    }
    private string _statusFilter = "All";
    private string _conditionFilter = "All";
    private bool _showStatusFilter = false;
    private bool _showConditionFilter = false;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalItems = 0;
    
    // สถานะ Modal แจ้งปัญหา
    private bool _showReportModal = false;
    private bool _isSubmittingReport = false;
    private string _reportJigId = "";
    private string _reportType = "";
    private string _reportRemark = "";

    /// <summary>ตั้งค่าตัวกรองจาก Query String บน URL</summary>
    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(StatusFromQuery))
        {
            _statusFilter = StatusFromQuery;
        }
        if (!string.IsNullOrEmpty(ConditionFromQuery))
        {
            _conditionFilter = ConditionFromQuery;
        }
        if (!string.IsNullOrEmpty(Filter))
        {
            // แปลงค่า Filter เดิมเป็นตัวเลือกสภาพ
            if (Filter == "Issues")
            {
                _statusFilter = "All";
                _conditionFilter = "Issues";
            }
        }
        FilterData();
    }

    private void ToggleStatusFilter() { _showStatusFilter = !_showStatusFilter; _showConditionFilter = false; }
    private void ToggleConditionFilter() { _showConditionFilter = !_showConditionFilter; _showStatusFilter = false; }

    /// <summary>ตั้งค่าตัวกรอง Status และอัปเดต URL Query String</summary>
    private void SetStatusFilter(string val)
    {
        _statusFilter = val;
        _showStatusFilter = false;
        var uri = NavManager.GetUriWithQueryParameters(new Dictionary<string, object?> { { "status", val == "All" ? null : val }, { "filter", null } });
        NavManager.NavigateTo(uri, forceLoad: false);
    }

    /// <summary>ตั้งค่าตัวกรอง Condition และอัปเดต URL Query String</summary>
    private void SetConditionFilter(string condition)
    {
        _conditionFilter = condition;
        _showConditionFilter = false;
        var uri = NavManager.GetUriWithQueryParameters(new Dictionary<string, object?> { { "condition", condition == "All" ? null : condition }, { "filter", null } });
        NavManager.NavigateTo(uri, forceLoad: false);
    }

    /// <summary>แปลงสถานะ (English) เป็นภาษาไทย</summary>
    private string GetStatusThai(string status) => status switch
    {
        "Available" => "พร้อมใช้งาน",
        "InUse" => "กำลังใช้งาน",
        "InTransit" => "กำลังขนย้าย",
        "Cleaning" => "กำลังทำความสะอาด",
        "Evaluation" => "รอประเมินสภาพ",
        "Lost" => "สูญหาย",
        _ => status
    };

    /// <summary>แปลงสภาพ (English) เป็นภาษาไทย</summary>
    private string GetConditionThai(string condition) => condition switch
    {
        "Good" => "ใช้งานได้ดี",
        "NeedsCleaning" => "ต้องทำความสะอาด",
        "UnderRepair" => "กำลังซ่อมแซม",
        "Broken" => "ชำรุดเสียหาย",
        "Lost" => "สูญหาย",
        "Issues" => "พบปัญหาทั้งหมด",
        "Other" => "อื่นๆ",
        _ => condition
    };

    /// <summary>คืนค่า CSS Class สำหรับ Status Badge</summary>
    private string GetStatusBadgeClass(string status) => status switch
    {
        "Available" => "bg-emerald-900/40 text-emerald-400 border-emerald-800",
        "InUse" => "bg-blue-900/40 text-blue-400 border-blue-800",
        "InTransit" => "bg-orange-900/40 text-orange-400 border-orange-800",
        "Cleaning" => "bg-amber-900/40 text-amber-400 border-amber-800",
        "Evaluation" => "bg-purple-900/40 text-purple-400 border-purple-800",
        "Lost" => "bg-slate-700 text-slate-400 border-slate-600",
        _ => "bg-slate-700 text-slate-400 border-slate-600"
    };

    /// <summary>คืนค่า CSS Class สำหรับ Condition Badge</summary>
    private string GetConditionBadgeClass(string condition) => condition switch
    {
        "Good" => "bg-emerald-900/40 text-emerald-400 border-emerald-800",
        "NeedsCleaning" => "bg-amber-900/40 text-amber-400 border-amber-800",
        "UnderRepair" => "bg-blue-900/40 text-blue-400 border-blue-800",
        "Broken" => "bg-red-900/40 text-red-400 border-red-800",
        "Lost" => "bg-slate-700 text-slate-400 border-slate-600",
        _ => "bg-slate-700 text-slate-400 border-slate-600"
    };

    protected override void OnInitialized()
    {
        Lang.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        Lang.OnChange -= StateHasChanged;
    }

    private bool _isLoading = true;
    private List<Jig> _allJigs = new();
    private List<Jig> _displayJigs = new();
    private List<Locator> _allLocators = new();

    private bool _showScanModal = false;
    private DotNetObjectReference<CurrentStatus>? _dotNetRef;

    /// <summary>ตรวจสอบสิทธิ์และโหลดข้อมูลหลัง Render ครั้งแรก</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            var auth = await Auth.GetAuthAsync();
            _role = auth.Role ?? "";
            _name = auth.Name ?? "";

            if (string.IsNullOrEmpty(_role))
            {
                NavManager.NavigateTo("/login");
                return;
            }

            await LoadData();
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>โหลดข้อมูลจิกและตำแหน่งทั้งหมดจาก API แบบ Parallel</summary>
    private async Task LoadData()
    {
        try
        {
            var jigsTask = Api.GetFromJsonAsync<List<Jig>>("api/jigs");
            var locatorsTask = Api.GetFromJsonAsync<List<Locator>>("api/locators");

            await Task.WhenAll(jigsTask, locatorsTask);

            _allJigs = await jigsTask ?? new List<Jig>();
            _allLocators = await locatorsTask ?? new List<Locator>();

            FilterData();
        }
        catch (Exception)
        {
            // จัดการ Error แบบเบา
        }
    }

    private void HandleSearchKeyDown(KeyboardEventArgs e)
    {
        // รองรับการกด Enter เพื่อค้นหา (แม้ FilterData จะทำงานแบบ Reactive อยู่แล้ว)
        if (e.Key == "Enter") FilterData();
    }

    /// <summary>
    /// กรองรายการจิกตามคำค้นหา, สถานะ, และสภาพ พร้อมคำนวณ Pagination
    /// ค้นหาจาก Id, PartNumber และ SmartCodeName
    /// </summary>
    private void FilterData()
    {
        var lower = _searchQuery.ToLowerInvariant();
        var query = _allJigs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(lower))
        {
            query = query.Where(j => 
                (j.Id?.ToLowerInvariant().Contains(lower) == true) || 
                (j.PartNumber?.ToLowerInvariant().Contains(lower) == true) ||
                (j.SmartCodeName?.ToLowerInvariant().Contains(lower) == true)
            );
        }

        if (_statusFilter != "All")
        {
            query = query.Where(j => j.Status == _statusFilter);
        }

        if (_conditionFilter == "Issues")
        {
            query = query.Where(j => j.Condition == "NeedsCleaning" || j.Condition == "Broken" || j.Condition == "Lost" || j.Condition == "Other");
        }
        else if (_conditionFilter != "All")
        {
            query = query.Where(j => j.Condition == _conditionFilter);
        }

        _totalItems = query.Count();

        // ปรับหน้าปัจจุบันหากเกินขอบเขต
        int maxPage = (int)Math.Ceiling(_totalItems / (double)_pageSize);
        if (maxPage == 0) maxPage = 1;
        if (_currentPage > maxPage) _currentPage = maxPage;

        _displayJigs = query.OrderBy(j => j.Id).Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
    }

    /// <summary>แปลง Locator ID เป็นชื่อย่อสำหรับแสดงผล (ตัดคำซ้ำซ้อนออก)</summary>
    private string GetLocatorName(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "-";
        var loc = _allLocators.FirstOrDefault(l => l.Id == id);
        if (loc == null) return id;

        string name = loc.Name;
        if (name.IndexOf("Zone", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var cleaned = name.Replace("1 Cabinet Shelf", "").Replace("Cabinet Shelf", "")
                             .Replace("Cabinet", "").Replace("Shelf", "")
                             .Trim();
            if (cleaned.StartsWith("1 ")) cleaned = cleaned.Substring(2).Trim();
            return cleaned;
        }
        return name;
    }

    /// <summary>เปิด Modal สแกน QR Code และเริ่มกล้อง</summary>
    private void OpenScanModal()
    {
        _showScanModal = true;
        StateHasChanged();
        
        // รอให้ Modal Render เสร็จก่อน แล้วค่อยเริ่มกล้อง
        Task.Delay(300).ContinueWith(_ => 
        {
            InvokeAsync(async () => {
                await JSRuntime.InvokeVoidAsync("startQrScan", _dotNetRef, "qr-reader");
            });
        });
    }

    /// <summary>ปิด Modal สแกน QR และหยุดกล้อง</summary>
    private async Task CloseScanModal()
    {
        _showScanModal = false;
        await JSRuntime.InvokeVoidAsync("stopQrScan");
    }

    /// <summary>รับค่า QR Code ที่สแกนได้จาก JS แล้วค้นหาจิกอัตโนมัติ</summary>
    [JSInvokable]
    public async Task OnQrScanned(string decodedText)
    {
        await CloseScanModal();
        _searchQuery = decodedText;
        FilterData();
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        try { await JSRuntime.InvokeVoidAsync("stopQrScan"); } catch { }
    }

    /// <summary>เปิด Modal แจ้งปัญหาจิก</summary>
    private void OpenReportModal(string jigId)
    {
        _reportJigId = jigId;
        _reportType = "";
        _reportRemark = "";
        _showReportModal = true;
    }

    private void CloseReportModal()
    {
        _showReportModal = false;
        _isSubmittingReport = false;
    }

    /// <summary>
    /// ส่งข้อมูลแจ้งปัญหาไป API — รีโหลดข้อมูลหลังสำเร็จ
    /// </summary>
    private async Task SubmitReportIssue()
    {
        if (string.IsNullOrEmpty(_reportJigId) || string.IsNullOrEmpty(_reportType)) return;

        _isSubmittingReport = true;
        StateHasChanged();

        try
        {
            var req = new
            {
                JigId = _reportJigId,
                User = _name,
                IssueType = _reportType,
                Remark = _reportRemark
            };

            var res = await Api.PostAsJsonAsync("api/transactions/reportissue", req);
            if (res.IsSuccessStatusCode)
            {
                await LoadData();
                CloseReportModal();
            }
            else
            {
                var error = await res.Content.ReadAsStringAsync();
                await JSRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = "Error",
                    text = $"ไม่สามารถแจ้งปัญหาได้: {error}",
                    icon = "error"
                });
            }
        }
        catch
        {
            // จัดการ Error เบื้องต้น
        }
        finally
        {
            _isSubmittingReport = false;
            StateHasChanged();
        }
    }

}