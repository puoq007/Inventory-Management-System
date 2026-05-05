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
/// หน้าจัดการตำแหน่งจัดเก็บ (Locators Management) — เพิ่ม แก้ไข ลบ นำเข้า Excel และส่งออก CSV/PDF
/// เข้าถึงได้เฉพาะ Role: Admin, Engineer, ProdLead
/// </summary>
public partial class Locators : ComponentBase
{

    #region State Fields
    private string _currentRole = "";
    private bool _isLoading = true;
    private bool _isImporting = false;
    private bool _showModal = false;
    private bool _isEditMode = false;

    // สถานะตัวกรองการส่งออก
    private bool _showExportFilter = false;
    private string _exportType = "";
    private string _filterSite = "";
    private string _filterType = "";
    private string _filterCabinet = "";
    private Locator _editingLocator = new();
    private string? _originalLocatorId = null;
    private bool _hasActions => true; // ทุกคนมีสิทธิ์ใช้ QR เป็นอย่างน้อย
    private string _errorMessage = "";
    private string _searchQuery = "";
    private string _zoneFilter = "All";
    private bool _showZoneFilter = false;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalItems = 0;
    private HashSet<string> _selectedLocatorIds = new();

    private List<Locator> _allLocators = new();
    private List<Locator> _displayLocators = new();

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
    /// ตรวจสอบสิทธิ์ผู้ใช้ (Admin/Engineer/ProdLead) แล้วโหลดข้อมูลตำแหน่งทั้งหมด
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var auth = await Auth.GetAuthAsync();
        _currentRole = auth.Role ?? "";
        
        if (_currentRole != "Admin" && _currentRole != "Engineer" && _currentRole != "ProdLead")
        {
            NavManager.NavigateTo("/");
            return;
        }

        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    #endregion

    #region Data Loading & Filtering
    /// <summary>
    /// โหลดรายการตำแหน่งทั้งหมดจาก API แล้วเรียก FilterData()
    /// </summary>
    private async Task LoadData()
    {
        try
        {
            _allLocators = await Api.GetFromJsonAsync<List<Locator>>("api/locators") ?? new List<Locator>();
            FilterData();
        }
        catch (Exception)
        {
            _errorMessage = "โหลดข้อมูลตำแหน่งจาก API ไม่สำเร็จ";
        }
    }

    private void ToggleZoneFilter() { _showZoneFilter = !_showZoneFilter; }

    /// <summary>
    /// ตั้งค่าตัวกรอง Zone แล้วกรองข้อมูลใหม่
    /// </summary>
    /// <param name="val">ประเภทโซน: "Production", "Cleaning", "Store" หรือ "All"</param>
    private void SetZoneFilter(string val)
    {
        _zoneFilter = val;
        FilterData();
        _showZoneFilter = false;
    }

    private void ClearAll()
    {
        _searchQuery = "";
        _zoneFilter = "All";
        _selectedLocatorIds.Clear();
        _showZoneFilter = false;
        FilterData();
    }

    /// <summary>
    /// กรองรายการตำแหน่งตามคำค้นหาและตัวกรอง Zone พร้อมคำนวณ Pagination
    /// ค้นหาจาก Id, Site, Cabinet และ Shelf
    /// </summary>
    private void FilterData()
    {
        var lowerQuery = (_searchQuery ?? "").ToLowerInvariant();
        var query = _allLocators.Where(l => 
            (string.IsNullOrWhiteSpace(lowerQuery) || 
             (l.Id ?? "").ToLowerInvariant().Contains(lowerQuery) ||
             (l.Site ?? "").ToLowerInvariant().Contains(lowerQuery) ||
             (l.Cabinet ?? "").ToLowerInvariant().Contains(lowerQuery) ||
             (l.Shelf ?? "").ToLowerInvariant().Contains(lowerQuery)) &&
            (_zoneFilter == "All" || l.Type == _zoneFilter)
        );

        _totalItems = query.Count();

        // ปรับหน้าปัจจุบันหากเกินขอบเขต
        int maxPage = (int)Math.Ceiling(_totalItems / (double)_pageSize);
        if (maxPage == 0) maxPage = 1;
        if (_currentPage > maxPage) _currentPage = maxPage;

        _displayLocators = query.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
    }

    #endregion

    #region Export (CSV / PDF)
    /// <summary>
    /// เปิด Modal ตัวกรองการส่งออก (CSV หรือ PDF)
    /// </summary>
    /// <param name="type">ประเภทการส่งออก: "csv" หรือ "pdf"</param>
    private void OpenExportFilter(string type)
    {
        _exportType = type;
        _showExportFilter = true;
    }

    /// <summary>
    /// รีเซ็ตค่าตัวกรองการส่งออกทั้งหมด
    /// </summary>
    private void ResetExportFilter()
    {
        _filterSite = "";
        _filterType = "";
        _filterCabinet = "";
    }

    /// <summary>
    /// กรองข้อมูลตำแหน่งตามเงื่อนไขที่ตั้งไว้ใน Export Filter (Site, Type, Cabinet)
    /// </summary>
    /// <returns>รายการตำแหน่งที่ผ่านตัวกรอง เรียงตาม Id</returns>
    private IEnumerable<Locator> GetFilteredExportData()
    {
        var q = _allLocators.AsEnumerable();
        if (!string.IsNullOrEmpty(_filterSite))    q = q.Where(l => l.Site == _filterSite);
        if (!string.IsNullOrEmpty(_filterType))    q = q.Where(l => l.Type == _filterType);
        if (!string.IsNullOrEmpty(_filterCabinet)) q = q.Where(l => l.Cabinet == _filterCabinet);
        return q.OrderBy(l => l.Id);
    }

    private async Task RunExport()
    {
        _showExportFilter = false;
        if (_exportType == "csv") await ExportCsv();
        else await ExportPdf();
    }

    /// <summary>
    /// ส่งออกข้อมูลตำแหน่ง (ที่ผ่านตัวกรอง) เป็นไฟล์ CSV รองรับภาษาไทย/อังกฤษ
    /// </summary>
    private async Task ExportCsv()
    {
        var isThai = Lang.Current == "TH";
        var headers = isThai
            ? "รหัส,Site,ตู้,ชั้น,ประเภท"
            : "ID,Site,Cabinet,Shelf,Type";

        var data = GetFilteredExportData();
        var rows = data.Select(l =>
            $"\"{l.Id}\",\"{l.Site}\",\"{l.Cabinet}\",\"{l.Shelf}\",\"{l.Type}\""
        );

        var csv = headers + "\n" + string.Join("\n", rows);
        await JSRuntime.InvokeVoidAsync("downloadTextFile", $"locators_{DateTime.Now:yyyyMMdd_HHmmss}.csv", csv);
    }

    /// <summary>
    /// สร้าง HTML Report สำหรับตำแหน่ง (พร้อม Badge ประเภทโซน) แล้วเรียก JS Export เป็น PDF
    /// </summary>
    private async Task ExportPdf()
    {
        var isThai = Lang.Current == "TH";
        var date = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var title    = isThai ? "รายงานตำแหน่งจัดเก็บ" : "Locators Report";
        var generated = isThai ? "สร้างเมื่อ" : "Generated";
        var totalRec  = isThai ? "จำนวนทั้งหมด" : "Total";
        var colId     = isThai ? "รหัส"         : "ID";
        var colSite   = isThai ? "Site"          : "Site";
        var colCab    = isThai ? "ตู้"           : "Cabinet";
        var colShelf  = isThai ? "ชั้น"          : "Shelf";
        var colType   = isThai ? "ประเภท"        : "Type";
        var footer    = isThai
            ? $"แมทเทล แบงคอก จำกัด &nbsp;|&nbsp; ระบบจัดการจิก &nbsp;|&nbsp; {date}"
            : $"Mattel Bangkok Limited &nbsp;|&nbsp; Jig Inventory Management System &nbsp;|&nbsp; {date}";

        var data = GetFilteredExportData().ToList();
        var rows = string.Join("", data.Select(l => {
            var typeBadge = l.Type switch {
                "Store"      => "badge-green",
                "Production" => "badge-blue",
                "Cleaning"   => "badge-amber",
                _            => "badge-gray"
            };
            var typeLabel = isThai ? l.Type switch {
                "Store" => "จัดเก็บ", "Production" => "ผลิต", "Cleaning" => "ล้าง", _ => l.Type
            } : l.Type;
            return $"<tr><td><strong>{l.Id}</strong></td><td>{l.Site}</td><td>{l.Cabinet}</td><td>{l.Shelf}</td><td><span class='badge {typeBadge}'>{typeLabel}</span></td></tr>";
        }));

        var html = $@"
<div class='pdf-header'>
  <div class='logo-text'>MATTEL</div>
  <div class='meta'>
    <div><strong>{title}</strong></div>
    <div>{generated}: {date}</div>
    <div>{totalRec}: {data.Count}</div>
  </div>
</div>
<h2>{title}</h2>
<table>
  <thead><tr><th>{colId}</th><th>{colSite}</th><th>{colCab}</th><th>{colShelf}</th><th>{colType}</th></tr></thead>
  <tbody>{rows}</tbody>
</table>
<div class='pdf-footer'>{footer}</div>";

        await JSRuntime.InvokeVoidAsync("exportPdf", title, html, $"locators_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    #endregion

    #region CRUD Operations
    /// <summary>
    /// เปิด Modal สำหรับเพิ่มตำแหน่งใหม่
    /// </summary>
    private void OpenAddModal()
    {
        _isEditMode = false;
        _originalLocatorId = null;
        _editingLocator = new Locator();
        _errorMessage = "";
        _showModal = true;
    }

    /// <summary>
    /// เปิด Modal สำหรับแก้ไขตำแหน่ง — สร้าง Copy ข้อมูลเพื่อป้องกันการแก้ไขต้นฉบับ
    /// </summary>
    /// <param name="loc">ตำแหน่งที่ต้องการแก้ไข</param>
    private void OpenEditModal(Locator loc)
    {
        _isEditMode = true;
        _originalLocatorId = loc.Id;
        _editingLocator = new Locator
        {
            Id       = loc.Id,
            Site     = loc.Site,
            Cabinet  = loc.Cabinet,
            Shelf    = loc.Shelf,
            Type     = loc.Type
        };
        _errorMessage = "";
        _showModal = true;
    }

    private void CloseModal()
    {
        _showModal = false;
    }

    /// <summary>
    /// บันทึกข้อมูลตำแหน่ง (สร้างใหม่หรืออัปเดต) — รองรับการเปลี่ยน ID (ใช้ rename endpoint)
    /// ตรวจสอบค่าซ้ำก่อนบันทึก
    /// </summary>
    private async Task SaveLocator()
    {
        _editingLocator.Id = _editingLocator.GetGeneratedId();
        
        if (string.IsNullOrWhiteSpace(_editingLocator.Id))
        {
            _errorMessage = "ต้องระบุรหัสตำแหน่ง";
            return;
        }

        HttpResponseMessage response;

        if (_isEditMode)
        {
            // ใช้ rename endpoint เสมอ — รองรับทั้งการเปลี่ยน ID และอัปเดตข้อมูลปกติ
            response = await Api.PostAsJsonAsync($"api/locators/{_originalLocatorId}/rename", _editingLocator);
        }
        else
        {
            if (_allLocators.Any(l => l.Id == _editingLocator.Id))
            {
                _errorMessage = "ตำแหน่งนี้มีอยู่แล้วในระบบ";
                return;
            }
            response = await Api.PostAsJsonAsync("api/locators", _editingLocator);
        }

        if (response.IsSuccessStatusCode)
        {
            await LoadData();
            _showModal = false;
            _errorMessage = "";
            StateHasChanged();
        }
        else
        {
            var body = await response.Content.ReadAsStringAsync();
            _errorMessage = $"บันทึกไม่สำเร็จ: {body}";
        }
    }

    /// <summary>
    /// ลบตำแหน่งหลังยืนยันด้วย SweetAlert — รีโหลดข้อมูลหลังลบสำเร็จ
    /// </summary>
    /// <param name="loc">ตำแหน่งที่ต้องการลบ</param>
    private async Task DeleteLocator(Locator loc)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirmAction", "Delete Locator", $"Are you sure you want to delete locator {loc.Id}?", "Yes, delete", "error");
        if (!confirmed) return;

        var response = await Api.DeleteAsync($"api/locators/{loc.Id}");
        if (response.IsSuccessStatusCode)
        {
            _selectedLocatorIds.Remove(loc.Id);
            await LoadData();
            StateHasChanged();
        }
        else
        {
            _errorMessage = "ลบตำแหน่งจาก API ไม่สำเร็จ";
        }
    }

    #endregion

    #region QR Code Printing
    /// <summary>
    /// สร้าง QR Code จากรหัสตำแหน่งแล้วเรียก JS เพื่อพิมพ์สติกเกอร์ QR ตำแหน่งจัดเก็บ
    /// </summary>
    /// <param name="loc">ตำแหน่งที่ต้องการพิมพ์ QR</param>
    private async Task PrintQR(Locator loc)
    {
        var qr = QrCode.EncodeText(loc.Id, QrCode.Ecc.Medium);
        var svgStr = qr.ToSvgString(4, "#000000", "#ffffff");
        var data = new {
            id = loc.Id,
            site = loc.Site,
            cabinet = loc.Cabinet,
            shelf = loc.Shelf,
            type = loc.Type
        };
        await JSRuntime.InvokeVoidAsync("printLocatorQR", svgStr, data);
    }

    private void ToggleSelection(string locId, object? isChecked)
    {
        if (isChecked is bool cb && cb)
            _selectedLocatorIds.Add(locId);
        else
            _selectedLocatorIds.Remove(locId);
            
        StateHasChanged();
    }

    private void ToggleRowSelection(string locId)
    {
        ToggleSelection(locId, !_selectedLocatorIds.Contains(locId));
    }

    private void ToggleSelectAll(ChangeEventArgs e)
    {
        bool selectAll = (bool)(e.Value ?? false);
        if (selectAll)
        {
            foreach (var loc in _displayLocators) _selectedLocatorIds.Add(loc.Id);
        }
        else
        {
            foreach (var loc in _displayLocators) _selectedLocatorIds.Remove(loc.Id);
        }
        StateHasChanged();
    }

    /// <summary>
    /// พิมพ์ QR Code แบบ Batch สำหรับตำแหน่งที่ถูกเลือกไว้ แล้วล้างการเลือกทั้งหมด
    /// </summary>
    private async Task PrintSelectedQRs()
    {
        if (!_selectedLocatorIds.Any()) return;

        var qrList = new List<object>();
        foreach (var id in _selectedLocatorIds)
        {
            var loc = _allLocators.FirstOrDefault(l => l.Id == id);
            if (loc == null) continue;
            var qr = QrCode.EncodeText(id, QrCode.Ecc.Medium);
            var svgStr = qr.ToSvgString(4, "#000000", "#ffffff");
            qrList.Add(new {
                svg = svgStr,
                id = id,
                site = loc.Site,
                cabinet = loc.Cabinet,
                shelf = loc.Shelf,
                type = loc.Type
            });
        }

        await JSRuntime.InvokeVoidAsync("printLocatorQRs", qrList);
        _selectedLocatorIds.Clear();
    }

    #endregion

    #region Excel Import
    /// <summary>
    /// โมเดลสำหรับรับผลลัพธ์การนำเข้า Excel
    /// </summary>
    private class ImportResponse
    {
        public int inserted { get; set; }
        public int updated { get; set; }
        public List<string> errors { get; set; } = new();
    }

    /// <summary>
    /// นำเข้าข้อมูลตำแหน่งจากไฟล์ Excel (.xlsx / .xls) — ตรวจสอบนามสกุลและขนาด (10MB) ก่อนส่งไป API
    /// แสดงผลลัพธ์ (inserted/updated/errors) ผ่าน alert
    /// </summary>
    private async Task ImportExcel(InputFileChangeEventArgs e)
    {
        _isImporting = true;
        StateHasChanged();

        try
        {
            var file = e.File;
            var ext = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                await JSRuntime.InvokeVoidAsync("alert", Lang.T("ไฟล์ไม่ถูกต้อง กรุณาเลือกไฟล์ .xlsx หรือ .xls เท่านั้น", "Invalid file. Please select a .xlsx or .xls file."));
                _isImporting = false;
                StateHasChanged();
                return;
            }
            if (file.Size > 10_000_000)
            {
                await JSRuntime.InvokeVoidAsync("alert", Lang.T("ไฟล์มีขนาดใหญ่เกินไป (สูงสุด 10MB)", "File is too large (max 10MB)."));
                _isImporting = false;
                StateHasChanged();
                return;
            }

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(10_000_000));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await Api.PostAsync("api/locators/upload", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ImportResponse>();
                if (result != null)
                {
                    var msg = $"นำเข้าสำเร็จ: เพิ่ม {result.inserted} รายการ, อัปเดต {result.updated} รายการ";
                    if (result.errors != null && result.errors.Any())
                    {
                        msg += $"\nข้อผิดพลาด:\n{string.Join("\n", result.errors)}";
                        await JSRuntime.InvokeVoidAsync("alert", msg);
                    }
                    else
                    {
                        await JSRuntime.InvokeVoidAsync("alert", msg);
                    }
                }
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", $"นำเข้า API ล้มเหลว: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"นำเข้าล้มเหลว: {ex.Message}");
        }
        finally
        {
            _isImporting = false;
            await LoadData();
            StateHasChanged();
        }
    }
    #endregion
}