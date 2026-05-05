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
/// หน้า Dashboard หลัก — แสดงสรุปภาพรวม, กราฟกิจกรรมประจำสัปดาห์, จิกที่ใช้บ่อยสุด, และธุรกรรมล่าสุด
/// เข้าถึงได้ทุก Role (ต้อง Login ก่อน)
/// </summary>
public partial class Dashboard : ComponentBase
{

    private string _role = "";
    private bool _isLoading = true;
    private int _totalJigs = 0;
    private int _availableJigs = 0;
    private int _inUseCount = 0;
    private int _actionRequiredCount = 0;
    private int _monthlyTxnCount = 0;
    private double _utilizationRate = 0;
    private double _repairRate = 0;
    private int[] _checkOutCounts = new int[6];
    private int[] _checkInCounts = new int[6];
    private int[] _cleaningCounts = new int[6];
    private DateTime _startOfWeek;
    
    private Dictionary<string, int> _topUsedJigs = new();
    private int _totalCheckouts = 0;
    
    private List<TransactionRow> _recentTxns = new();
    private List<TransactionRow> _allTxns = new();
    private List<Jig> _allJigs = new();


    /// <summary>อัปเดต UI และ Chart เมื่อเปลี่ยนภาษา</summary>
    private async void OnLanguageChanged()
    {
        await InvokeAsync(async () =>
        {
            StateHasChanged();
            await RenderChart();
        });
    }

    protected override void OnInitialized()
    {
        Lang.OnChange += OnLanguageChanged;
    }

    public void Dispose()
    {
        Lang.OnChange -= OnLanguageChanged;
    }

    /// <summary>ตรวจสอบสิทธิ์และโหลดข้อมูลหลัง Render ครั้งแรก</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var auth = await Auth.GetAuthAsync();
            _role = auth.Role ?? "";

            if (string.IsNullOrEmpty(_role))
            {
                NavManager.NavigateTo("/login");
                return;
            }

            await LoadData();
            _isLoading = false;
            StateHasChanged();
            
            await Task.Delay(100);
            await RenderChart();
        }
    }

    /// <summary>โหลดข้อมูลธุรกรรมและจิกทั้งหมดจาก API แบบ Parallel</summary>
    private async Task LoadData()
    {
        try
        {
            var txnsTask = Api.GetFromJsonAsync<TransactionPagedResponse>($"api/transactions?page=1&pageSize=500");
            var jigsTask = Api.GetFromJsonAsync<List<Jig>>("api/jigs");

            await Task.WhenAll(txnsTask, jigsTask);

            var txnResult = await txnsTask;
            _allTxns = txnResult?.Items ?? new List<TransactionRow>();
            _allJigs = await jigsTask ?? new List<Jig>();

            CalculateStats();
        }
        catch (Exception)
        {
            // จัดการ Error แบบเบา
        }
    }

    /// <summary>คำนวณสถิติทั้งหมด: จำนวนจิก, อัตราการใช้งาน, กราฟกิจกรรมรายสัปดาห์</summary>
    private void CalculateStats()
    {
        _recentTxns = _allTxns.OrderByDescending(t => t.Timestamp).Take(5).ToList();
        
        _totalJigs = _allJigs.Count;
        _availableJigs = _allJigs.Count(j => j.Status == "Available");
        _inUseCount = _allJigs.Count(j => j.Status == "InUse");
        _actionRequiredCount = _allJigs.Count(j => j.Condition == "NeedsCleaning" || j.Condition == "Broken" || j.Condition == "Lost");
        
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        _monthlyTxnCount = _allTxns.Count(t => t.Timestamp >= thirtyDaysAgo);

        if (_totalJigs > 0)
        {
            _utilizationRate = Math.Round((double)_inUseCount * 100 / _totalJigs, 1);
            _repairRate = Math.Round((double)_actionRequiredCount * 100 / _totalJigs, 1);
        }
        else
        {
            _utilizationRate = 0;
            _repairRate = 0;
        }

        // คำนวณข้อมูลวิเคราะห์
        var checkouts = _allTxns.Where(t => t.Action == "CheckOut").ToList();
        _totalCheckouts = checkouts.Count;
        _topUsedJigs = checkouts
            .GroupBy(t => {
                var jig = _allJigs.FirstOrDefault(j => j.Uid == t.JigUid);
                return jig?.Id ?? t.JigUid ?? "UNKNOWN";
            })
            .Select(g => new { JigId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToDictionary(x => x.JigId, x => x.Count);

        // คำนวณกิจกรรมประจำสัปดาห์นี้ (จันทร์-เสาร์)
        DateTime today = DateTime.Now.Date;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        _startOfWeek = today.AddDays(-1 * diff).Date;
        _checkOutCounts = new int[6]; // Reset
        _checkInCounts = new int[6];
        _cleaningCounts = new int[6];
        
        foreach (var txn in _allTxns)
        {
            if (txn.Timestamp >= _startOfWeek)
            {
                int dayDiff = (int)(txn.Timestamp.Date - _startOfWeek).TotalDays;
                // ไม่รวมวันอาทิตย์ (dayDiff == 6) และวันที่เกินขอบเขต
                if (dayDiff >= 0 && dayDiff < 6)
                {
                    if (txn.Action == "CheckOut")
                        _checkOutCounts[dayDiff]++;
                    else if (txn.Action == "CheckInToCleaning")
                        _cleaningCounts[dayDiff]++;
                    else if (txn.Action == "CheckIn" || txn.Action == "ReturnToStore" || txn.Action == "ForceCheckIn")
                        _checkInCounts[dayDiff]++;
                }
            }
        }
    }

    /// <summary>ส่งออกรายงานวิเคราะห์เป็น PDF</summary>
    private async Task ExportAnalytics()
    {
        var isThai = Lang.Current == "TH";
        var date = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var fileName = $"analytics_report_{DateTime.Now:yyyyMMdd}.pdf";

        // ค่า Label ตามภาษา
        var title        = isThai ? "รายงานภาพรวมระบบ" : "Analytics Report";
        var generated    = isThai ? "สร้างเมื่อ"        : "Generated";
        var secStatus    = isThai ? "สถานะภาพรวม"       : "Overall Status";
        var secTopJigs   = isThai ? "จิกที่ถูกใช้งานสูงสุด" : "Top Used Jigs";
        var lblTotal     = isThai ? "จิกทั้งหมด"        : "Total Jigs";
        var lblAvail     = isThai ? "พร้อมใช้งาน"       : "Available";
        var lblInUse     = isThai ? "กำลังใช้งาน"       : "In Use";
        var lblIssues    = isThai ? "พบปัญหา"           : "Issues";
        var colNo        = isThai ? "ลำดับ"             : "#";
        var colJigId     = isThai ? "รหัสจิก"           : "Jig ID";
        var colCheckouts = isThai ? "จำนวนการเบิก"      : "Checkouts";
        var footer       = isThai
            ? $"แมทเทล แบงคอก จำกัด &nbsp;|&nbsp; ระบบจัดการจิก &nbsp;|&nbsp; {date}"
            : $"Mattel Bangkok Limited &nbsp;|&nbsp; Jig Inventory Management System &nbsp;|&nbsp; {date}";

        var topJigsRows = string.Join("", _topUsedJigs.Select((kv, i) =>
            $"<tr><td>{i + 1}</td><td><strong>{kv.Key}</strong></td><td>{kv.Value}</td></tr>"));

        var html = $@"
<div class='pdf-header'>
  <div class='logo-text'>MATTEL</div>
  <div class='meta'>
    <div><strong>{title}</strong></div>
    <div>{generated}: {date}</div>
  </div>
</div>

<h2>{secStatus}</h2>
<div class='stat-grid'>
  <div class='stat-card'><div class='label'>{lblTotal}</div><div class='value'>{_totalJigs}</div></div>
  <div class='stat-card'><div class='label'>{lblAvail}</div><div class='value' style='color:#166534'>{_availableJigs}</div></div>
  <div class='stat-card'><div class='label'>{lblInUse}</div><div class='value' style='color:#1e40af'>{_inUseCount}</div></div>
  <div class='stat-card'><div class='label'>{lblIssues}</div><div class='value' style='color:#cc0000'>{_actionRequiredCount}</div></div>
</div>

<h2>{secTopJigs}</h2>
<table>
  <thead><tr><th>{colNo}</th><th>{colJigId}</th><th>{colCheckouts}</th></tr></thead>
  <tbody>{topJigsRows}</tbody>
</table>

<div class='pdf-footer'>{footer}</div>";

        await JSRuntime.InvokeVoidAsync("exportPdf", title, html, fileName);
    }

    /// <summary>แปลงสภาพ (English) เป็นภาษาไทย</summary>
    private string GetConditionThai(string? condition) => condition switch
    {
        "Good" => "ใช้งานได้ดี",
        "NeedsCleaning" => "ต้องทำความสะอาด",
        "UnderRepair" => "กำลังซ่อมแซม",
        "Broken" => "ชำรุดเสียหาย",
        "Lost" => "สูญหาย",
        _ => condition ?? "-"
    };

    /// <summary>สร้างกราฟกิจกรรมรายสัปดาห์ผ่าน Chart.js</summary>
    private async Task RenderChart()
    {
        var labels = new string[6];
        bool isThai = Lang.Current == "TH";
        string[] thaiDays = { "อา.", "จ.", "อ.", "พ.", "พฤ.", "ศ.", "ส." };
        string[] thaiMonths = { "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };

        for (int i = 0; i < 6; i++)
        {
            var d = _startOfWeek.AddDays(i);
            if (isThai)
            {
                labels[i] = $"{thaiDays[(int)d.DayOfWeek]} {d.Day} {thaiMonths[d.Month]}";
            }
            else
            {
                labels[i] = d.ToString("ddd, d MMM", new System.Globalization.CultureInfo("en-US"));
            }
        }
        
        var labelOut = Lang.T("ยืมออก", "Check Outs");
        var labelIn = Lang.T("คืนเข้าตู้", "Store");
        var labelClean = Lang.T("ส่งล้าง", "Cleaning");

        await JSRuntime.InvokeVoidAsync("initActivityChart", "activityChart", labels, _checkOutCounts, _checkInCounts, _cleaningCounts, labelOut, labelIn, labelClean);
    }

    /// <summary>จัดรูปแบบวันที่ตามภาษาที่เลือก (ไทย/อังกฤษ)</summary>
    private string FormatDate(DateTime date)
    {
        if (Lang.Current == "TH")
        {
            string[] thaiMonths = { "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
            return $"{date.Day} {thaiMonths[date.Month]} {date:HH:mm}";
        }
        return date.ToString("MMM dd, HH:mm", new System.Globalization.CultureInfo("en-US"));
    }

    /// <summary>โมเดลสำหรับรับผลลัพธ์รายการธุรกรรมแบบแบ่งหน้า</summary>
    private class TransactionPagedResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<TransactionRow> Items { get; set; } = new();
    }

}