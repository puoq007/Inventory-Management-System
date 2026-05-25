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
/// หน้าประวัติการใช้งานจิก — แสดงรายการธุรกรรมทั้งหมด, กรองตามประเภท/Role/ช่วงเวลา, ส่งออก CSV/PDF
/// เข้าถึงได้ทุก Role (ยกเว้น Guest)
/// </summary>
public partial class History : ComponentBase
{

    private string _role = "";
    private bool _isLoading = true;
    private string _searchQuery = "";
    private IEnumerable<TransactionRow>? _displayTransactions;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalItems = 0;
    private bool _hasTransactions = false;
    private List<TransactionRow> _allTransactions = new();
    private List<Jig> _allJigs = new();
    private List<UserAccount> _allUsers = new();
    private List<Locator> _allLocators = new();
    private Dictionary<string, string> _transactionSources = new();

    private string _actionFilter = "All";
    private string _roleFilter = "All";
    private bool _showActionFilter = false;
    private bool _showRoleFilter = false;
    private string? _cancellingId = null;

    private DateTime? _exportDateFrom = null;
    private DateTime? _exportDateTo = null;

    private void ToggleActionFilter() { _showActionFilter = !_showActionFilter; _showRoleFilter = false; }
    private void ToggleRoleFilter() { _showRoleFilter = !_showRoleFilter; _showActionFilter = false; }
    
    private void SetActionFilter(string val) { _actionFilter = val; _showActionFilter = false; FilterData(); }
    private void SetRoleFilter(string val) { _roleFilter = val; _showRoleFilter = false; FilterData(); }

    private async Task OpenDatePicker(string id)
    {
        await JSRuntime.InvokeVoidAsync("eval", $"document.getElementById('{id}').showPicker()");
    }

    /// <summary>จัดรูปแบบวันที่แสดงผลตามภาษา (ไทย/อังกฤษ)</summary>
    private string FormatDateDisplay(DateTime dt)
    {
        if (Lang.Current == "TH")
        {
            string[] thaiMonths = { "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
            return $"{dt.Day:D2} {thaiMonths[dt.Month]} {dt.Year + 543}";
        }
        return $"{dt.Day:D2}/{dt.Month:D2}/{dt.Year}";
    }

    protected override void OnInitialized()
    {
        Lang.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        Lang.OnChange -= StateHasChanged;
    }

    /// <summary>ตรวจสอบสิทธิ์และโหลดข้อมูลหลัง Render ครั้งแรก</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var auth = await Auth.GetAuthAsync();
            _role = auth.Role ?? "";

            if (string.IsNullOrEmpty(_role) || _role == "Guest")
            {
                // Guest ไม่มีสิทธิ์ดูประวัติ
                NavManager.NavigateTo("/");
                return;
            }

            await LoadData();
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>โหลดข้อมูลธุรกรรม, จิก, และผู้ใช้ทั้งหมดจาก API แบบ Parallel</summary>
    private async Task LoadData()
    {
        try
        {
            // โหลด Transactions, Jigs และ Locators (ทุก Role เข้าถึงได้)
            var txnsTask = Api.GetFromJsonAsync<TransactionPagedResponse>($"api/transactions?page=1&pageSize=500");
            var jigsTask = Api.GetFromJsonAsync<List<Jig>>("api/jigs");
            var locsTask = Api.GetFromJsonAsync<List<Locator>>("api/locators");

            await Task.WhenAll(txnsTask, jigsTask, locsTask);

            var txnResult = await txnsTask;
            _allTransactions = txnResult?.Items ?? new List<TransactionRow>();
            _transactionSources = txnResult?.Sources ?? new Dictionary<string, string>();
            _allJigs = await jigsTask ?? new List<Jig>();
            _allLocators = await locsTask ?? new List<Locator>();

            // โหลด Users แยก — เฉพาะ Admin เท่านั้นที่เข้าถึง api/users ได้
            try
            {
                _allUsers = await Api.GetFromJsonAsync<List<UserAccount>>("api/users") ?? new List<UserAccount>();
            }
            catch
            {
                _allUsers = new List<UserAccount>();
            }

            FilterData();
        }
        catch (Exception)
        {
            // จัดการ Error แบบเบา
        }
    }

    /// <summary>
    /// กรองรายการธุรกรรมตามคำค้นหา, ประเภทรายการ, และ Role พร้อมคำนวณ Pagination
    /// ค้นหาจาก JigId, JigUid, Action และ User
    /// </summary>
    private void FilterData()
    {
        _hasTransactions = _allTransactions.Any();
        
        var query = _allTransactions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            var lowerQuery = _searchQuery.ToLowerInvariant();
            query = query.Where(t => 
            {
                var jig = _allJigs.FirstOrDefault(j => j.Uid == t.JigUid);
                return (jig?.Id?.ToLowerInvariant().Contains(lowerQuery) == true) ||
                       (t.JigUid?.ToLowerInvariant().Contains(lowerQuery) == true) ||
                       t.Action.ToLowerInvariant().Contains(lowerQuery) ||
                       t.User.ToLowerInvariant().Contains(lowerQuery);
            });
        }

        if (_actionFilter != "All")
            query = query.Where(t => t.Action == _actionFilter);
            
        if (_roleFilter != "All")
            query = query.Where(t => {
                var userAcct = _allUsers.FirstOrDefault(u => u.Name == t.User);
                var r = userAcct != null ? userAcct.Role : "Unknown";
                return r == _roleFilter;
            });

        _totalItems = query.Count();

        // ปรับหน้าปัจจุบันหากเกินขอบเขตหลังกรอง
        int maxPage = (int)Math.Ceiling(_totalItems / (double)_pageSize);
        if (maxPage == 0) maxPage = 1;
        if (_currentPage > maxPage) _currentPage = maxPage;

        _displayTransactions = query.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
    }

    /// <summary>แปลงประเภทรายการ (English) เป็นภาษาไทย</summary>
    private string GetActionThai(string action) => action switch {
        "CheckOut" => "เบิกออก",
        "CheckIn" => "คืนเข้าตู้",
        "CheckInToCleaning" => "ส่งล้าง",
        "ReturnToStore" => "เก็บเข้าคลัง",
        "ReportIssue" => "แจ้งพบปัญหา",
        "Scrapped" => "จำหน่ายออก",
        "Transfer" => "โอนย้าย",
        "CancelTransaction" => "ยกเลิกรายการ",
        _ => action
    };

    /// <summary>ดึงแค่ ID แบบสั้น (GeneratedId) มาแสดง</summary>
    private string GetLocatorDisplayName(string? destinationOrId)
    {
        if (string.IsNullOrEmpty(destinationOrId)) return "";
        
        // พยายามหาจาก ID หรือ Name (เพราะข้อมูลเก่าใน DB อาจเก็บเป็นชื่อเต็ม)
        var loc = _allLocators.FirstOrDefault(l => 
            l.Id == destinationOrId || 
            l.Name == destinationOrId || 
            l.GetGeneratedId() == destinationOrId);
            
        if (loc != null) return loc.GetGeneratedId();
        
        // ถ้าไม่ตรงกับตาราง Locators เลย ให้แสดงค่าเดิมไปก่อน
        return destinationOrId;
    }

    /// <summary>สร้างข้อความแสดงการเปลี่ยนตำแหน่ง เช่น "MBK1-ZONE1 → MBK1-ZONE2"</summary>
    private string GetLocationChangeText(TransactionRow txn)
    {
        if (txn.Action == "CancelTransaction")
        {
            // ตัวอย่าง: "Cancelled: CheckOut → MBK1 Cabinet Shelf ZONE3"
            var dest = txn.Destination;
            string separator = dest.Contains("→") ? "→" : (dest.Contains("->") ? "->" : null);
            
            if (separator != null)
            {
                var parts = dest.Split(separator);
                var actionPart = parts[0].Trim(); 
                var locPart = parts[1].Trim();
                
                var shortLoc = GetLocatorDisplayName(locPart);
                return $"{actionPart} → {shortLoc}";
            }
            return dest;
        }

        var source = _transactionSources.TryGetValue(txn.Id, out var src) ? src : "";
        var sourceName = GetLocatorDisplayName(source);
        var destName = GetLocatorDisplayName(txn.Destination);

        // ถ้า Destination ไม่ใช่ Locator ID → ใช้ค่าเดิม (เช่น "Storage", "Cleaning Station")
        if (string.IsNullOrEmpty(destName)) destName = txn.Destination;

        if (!string.IsNullOrEmpty(sourceName) && !string.IsNullOrEmpty(destName))
            return $"{sourceName} → {destName}";
        if (!string.IsNullOrEmpty(destName))
            return destName;
        return "";
    }

    /// <summary>ตรวจสอบว่า Transaction นี้เป็นรายการล่าสุดของจิกตัวนั้นและสแกนไม่เกิน 30 นาที (ข้าม CancelTransaction)</summary>
    private bool IsLatestTransaction(TransactionRow txn)
    {
        if (txn.Action == "CancelTransaction" || txn.Action == "Scrapped") return false;

        // ตรวจสอบว่ารายการนี้สแกนไม่เกิน 30 นาที
        if ((DateTime.Now - txn.Timestamp).TotalMinutes > 30) return false;

        var latest = _allTransactions
            .Where(t => t.JigUid == txn.JigUid && t.Action != "CancelTransaction")
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefault();
        return latest != null && latest.Id == txn.Id;
    }

    /// <summary>ยกเลิกรายการธุรกรรม — เรียก API แล้วรีโหลดข้อมูล</summary>
    private async Task CancelTransaction(string transactionId)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirmAction",
            Lang.T("ยืนยันการยกเลิก?", "Confirm Cancel?"),
            Lang.T("ต้องการยกเลิกรายการนี้และกลับสถานะจิกไปยังสถานะก่อนหน้าหรือไม่?", "Do you want to cancel this transaction and revert the jig to its previous state?"),
            Lang.T("ยืนยัน ยกเลิกรายการ", "Yes, Cancel It"),
            "warning",
            Lang.T("ไม่", "Cancel")
        );

        if (!confirmed) return;

        try
        {
            _cancellingId = transactionId;
            StateHasChanged();

            var response = await Api.PostAsJsonAsync($"api/transactions/cancel/{transactionId}", new { });

            if (response.IsSuccessStatusCode)
            {
                await JSRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = Lang.T("สำเร็จ!", "Success!"),
                    text = Lang.T("ยกเลิกรายการและกลับสถานะจิกสำเร็จ", "Transaction cancelled and jig status reverted successfully"),
                    icon = "success",
                    timer = 2500,
                    showConfirmButton = false
                });

                // รีโหลดข้อมูลทั้งหมด
                await LoadData();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await JSRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = Lang.T("ไม่สำเร็จ!", "Error!"),
                    text = error,
                    icon = "error",
                    confirmButtonColor = "#ef4444"
                });
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("Swal.fire", new
            {
                title = "Error",
                text = ex.Message,
                icon = "error"
            });
        }
        finally
        {
            _cancellingId = null;
            StateHasChanged();
        }
    }

    /// <summary>สร้าง HTML Report แล้ว Export เป็น PDF — รองรับตัวกรองช่วงวันที่</summary>
    private async Task ExportPdf()
    {
        var isThai = Lang.Current == "TH";
        var date = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        var exportData = _allTransactions.AsEnumerable();

        if (_exportDateFrom.HasValue)
            exportData = exportData.Where(t => t.Timestamp.Date >= _exportDateFrom.Value.Date);
        if (_exportDateTo.HasValue)
            exportData = exportData.Where(t => t.Timestamp.Date <= _exportDateTo.Value.Date);

        var exportList = exportData.ToList();

        var dateRangeLabel = "";
        if (_exportDateFrom.HasValue || _exportDateTo.HasValue)
        {
            var from = _exportDateFrom.HasValue ? FormatDateDisplay(_exportDateFrom.Value) : "...";
            var to   = _exportDateTo.HasValue   ? FormatDateDisplay(_exportDateTo.Value)   : "...";
            var rangeTitle = isThai ? "ช่วงวันที่" : "Date Range";
            dateRangeLabel = $"<div>{rangeTitle}: {from} – {to}</div>";
        }

        var rows = string.Join("", exportList.Select(txn => {
            var jig      = _allJigs.FirstOrDefault(j => j.Uid == txn.JigUid);
            var jigId    = jig?.Id ?? txn.JigUid;
            var specName = jig?.SmartCodeName ?? (isThai ? "ไม่ทราบ" : "Unknown");
            var timeStr  = GetFormattedDate(txn.Timestamp) + " " + txn.Timestamp.ToString("HH:mm");
            var actionLabel = isThai ? GetActionThai(txn.Action) : txn.Action;
            var badgeClass = txn.Action switch {
                "CheckOut"          => "badge-blue",
                "CheckIn"           => "badge-green",
                "ReturnToStore"     => "badge-green",
                "CheckInToCleaning" => "badge-amber",
                "ReportIssue"       => "badge-red",
                "Transfer"          => "badge-purple",
                _                   => "badge-gray"
            };
            var locText = GetLocationChangeText(txn);
            return $"<tr><td>{timeStr}</td><td><strong>{jigId}</strong></td><td>{specName}</td><td><span class='badge {badgeClass}'>{actionLabel}</span></td><td>{locText}</td><td>{txn.User}</td></tr>";
        }));

        // ค่า Label ตามภาษา
        var title       = isThai ? "รายงานประวัติการใช้งาน" : "Transaction History Report";
        var generated   = isThai ? "สร้างเมื่อ" : "Generated";
        var totalRec    = isThai ? "จำนวนรายการ" : "Total Records";
        var colDate     = isThai ? "วันเวลา"     : "Timestamp";
        var colJigId    = isThai ? "รหัสจิก"     : "Jig ID";
        var colSpec     = isThai ? "ชื่อ Smart Code" : "Smart Code Name";
        var colAction   = isThai ? "การดำเนินการ" : "Action";
        var colLoc      = isThai ? "ตำแหน่ง"     : "Location";
        var colUser     = isThai ? "ผู้ใช้งาน"   : "User";
        var footer      = isThai
            ? $"แมทเทล กรุงเทพ จำกัด &nbsp;|&nbsp; ระบบจัดการ JIG &nbsp;|&nbsp; {date}"
            : $"Mattel Bangkok Limited &nbsp;|&nbsp; Jig Inventory Management System &nbsp;|&nbsp; {date}";

        var html = $@"
<div class='pdf-header'>
  <div class='logo-text'>MATTEL</div>
  <div class='meta'>
    <div><strong>{title}</strong></div>
    <div>{generated}: {date}</div>
    {dateRangeLabel}
    <div>{totalRec}: {exportList.Count}</div>
  </div>
</div>

<h2>{title}</h2>
<table>
  <thead>
    <tr>
      <th>{colDate}</th>
      <th>{colJigId}</th>
      <th>{colSpec}</th>
      <th>{colAction}</th>
      <th>{colLoc}</th>
      <th>{colUser}</th>
    </tr>
  </thead>
  <tbody>{rows}</tbody>
</table>

<div class='pdf-footer'>{footer}</div>";

        await JSRuntime.InvokeVoidAsync("exportPdf", title, html, $"history_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    /// <summary>ส่งออกข้อมูลธุรกรรมเป็นไฟล์ CSV — รองรับตัวกรองช่วงวันที่</summary>
    private async Task ExportCsv()
    {
        var isThai = Lang.Current == "TH";
        var headers = isThai
            ? "วันเวลา,รหัสจิก,Smart Code,การดำเนินการ,ตำแหน่ง,ผู้ใช้งาน"
            : "Timestamp,Jig ID,Smart Code,Action,Location,User";

        var exportData = _allTransactions.AsEnumerable();
        if (_exportDateFrom.HasValue) exportData = exportData.Where(t => t.Timestamp.Date >= _exportDateFrom.Value.Date);
        if (_exportDateTo.HasValue)   exportData = exportData.Where(t => t.Timestamp.Date <= _exportDateTo.Value.Date);

        static string T(string? v) => string.IsNullOrEmpty(v) ? "" : $"\t{v}";

        var rows = exportData.Select(txn => {
            var jig = _allJigs.FirstOrDefault(j => j.Uid == txn.JigUid);
            var jigId = jig?.Id ?? txn.JigUid;
            var specName = jig?.SmartCodeName ?? "";
            var actionLabel = isThai ? GetActionThai(txn.Action) : txn.Action;
            var locText = GetLocationChangeText(txn);
            return $"\"{txn.Timestamp:dd/MM/yyyy HH:mm:ss}\",\"{T(jigId)}\",\"{specName}\",\"{actionLabel}\",\"{locText}\",\"{txn.User}\"";
        });

        var csv = headers + "\n" + string.Join("\n", rows);
        await JSRuntime.InvokeVoidAsync("downloadTextFile", $"history_{DateTime.Now:yyyyMMdd_HHmmss}.csv", csv);
    }

    /// <summary>จัดรูปแบบวันที่ตามภาษาที่เลือก (ไทย/อังกฤษ)</summary>
    private string GetFormattedDate(DateTime dt)
    {
        if (Lang.Current == "TH")
        {
            string[] thaiMonths = { "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
            return $"{dt.Day:D2} {thaiMonths[dt.Month]} {dt.Year}";
        }
        else
        {
            string[] engMonths = { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            return $"{dt.Day:D2} {engMonths[dt.Month]} {dt.Year}";
        }
    }

    /// <summary>โมเดลสำหรับรับผลลัพธ์รายการธุรกรรมแบบแบ่งหน้า พร้อม Source</summary>
    private class TransactionPagedResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<TransactionRow> Items { get; set; } = new();
        public Dictionary<string, string> Sources { get; set; } = new();
    }

}