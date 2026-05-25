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
/// หน้าจัดการจิก (Jigs Management) — รองรับการลงทะเบียน แก้ไข ลบ นำเข้า Excel และส่งออก CSV/PDF
/// เข้าถึงได้เฉพาะ Role: Admin, Engineer, ProdLead
/// </summary>
public partial class RegisterJig : ComponentBase
{

    #region State Fields
    private bool _isLoading = true;
    private string _searchQuery = "";
    private string _globalError = "";
    private List<Jig> _allJigs = new();
    private List<Locator> _allLocators = new();
    private IEnumerable<Jig> _displayJigs = Array.Empty<Jig>();
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalItems = 0;

    private bool _showModal = false;
    private bool _isEditMode = false;
    private bool _isSubmitting = false;
    private string _modalError = "";
    private Jig _editingJig = new Jig { Status = "Available", Condition = "Good" };
    private HashSet<string> _selectedJigIds = new();
    
    private List<shared.Models.PartMaster> _compatiblePartsModel = new();
    private string _newPartInput = "";
    private string _userRole = "";
    
    // สถานะการอัปโหลดรูปภาพ
    private IBrowserFile? _selectedImageFile;
    private string? _imagePreview;
    private bool _isUploadingImage = false;
    private bool _removeExistingImage = false;

    // สถานะตัวกรองการส่งออก
    private bool _showExportFilter = false;
    private string _exportType = ""; // "csv" or "pdf"
    private string _filterToolNo = "";
    private string _filterJigType = "";
    private string _filterPartType = "";
    private string _filterStatus = "";
    private DateTime? _filterDateFrom = null;
    private DateTime? _filterDateTo = null;

    // ตัวเลือก Dropdown จากภาพอ้างอิง
    private readonly string[] _stepPrintOptions = { "1 : L", "2 : R", "3 : Hood", "4 : Roof", "5 : Re.Hood", "6 : Front", "7 : Rear", "8 : Under", "9 : Re.L", "10 : Re.R" };

    // แมปชื่อ Step แบบข้อความเก่าเป็นตัวเลข
    private static readonly Dictionary<string, string> _stepNameToNumber = new(StringComparer.OrdinalIgnoreCase)
    {
        ["L"] = "1", ["R"] = "2", ["Hood"] = "3", ["Roof"] = "4",
        ["Re.Hood"] = "5", ["Front"] = "6", ["Rear"] = "7", ["Under"] = "8",
        ["Re.L"] = "9", ["Re.R"] = "10",
        
        // เก็บ mapping ชื่อเก่าที่พิมพ์ผิดไว้เพื่อรองรับข้อมูลเดิมในฐานข้อมูล
        ["Read Hood"] = "5", ["Read"] = "7"
    };

    #endregion

    #region Lifecycle
    /// <summary>
    /// แปลงหมายเลข Step (เช่น "1") กลับเป็น Label แบบเต็ม (เช่น "1 : L")
    /// </summary>
    /// <param name="num">หมายเลข Step ที่ต้องการแปลง</param>
    /// <returns>Label แบบเต็มหรือคืนค่าเดิมหากไม่พบ</returns>
    private string StepNumberToLabel(string num)
    {
        var match = _stepPrintOptions.FirstOrDefault(o => o.StartsWith(num + " "));
        return match ?? num;
    }
    private readonly string[] _partTypeOptions = { "Body", "Wing", "Window", "Interior", "Chassis", "Bumper", "Wheel", "Face", "Grill", "Teeth", "Tail", "Spoiler", "Tender" };
    private readonly string[] _jigTypeOptions = { "Plywood", "Stack", "Flip", "Spoiler", "Hybrid", "BBQ", "Plywood Flag", "JIG Wheel" };
    private readonly string[] _processOptions = { "PIM", "PL", "VUM", "HSP", "RB", "LQ", "TP", "AS" };
    private readonly string[] _heightJigOptions = { "40", "45", "50", "75", "80", "90", "HMR Plywood", "MDF Plywood" };

    // รายการ Step Print ที่เลือกไว้ (เลือกได้หลายค่า)
    private List<string> _selectedSteps = new();

    // ตัวช่วยจัดการวันที่สำหรับปฏิทิน
    private DateTime? _selectedDate; 
    private DateTime? SelectedDate { 
        get => _selectedDate; 
        set {
            _selectedDate = value;
            if (value.HasValue) _editingJig.Date = value.Value.ToString("dd/MM/yy");
            else _editingJig.Date = null;
        }
    }

    protected override void OnInitialized() { Lang.OnChange += StateHasChanged; }

    /// <summary>
    /// ตรวจสอบสิทธิ์ผู้ใช้หลัง Render ครั้งแรก — Redirect ไป Dashboard หาก Role ไม่ผ่านเงื่อนไข
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var auth = await Auth.GetAuthAsync();
            var role = auth.Role ?? "";
            _userRole = role;
            
            if (role != "Admin" && role != "Engineer" && role != "ProdLead")
            {
                NavManager.NavigateTo("/");
                return;
            }
            
            await LoadData();
        }
    }

    #endregion

    #region Data Loading & Filtering
    /// <summary>
    /// โหลดข้อมูลจิกและตำแหน่งจัดเก็บทั้งหมดจาก API แบบ Parallel แล้วเรียก FilterData()
    /// </summary>
    private async Task LoadData()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            var jigsTask = Api.GetFromJsonAsync<List<Jig>>("api/jigs");
            var locsTask = Api.GetFromJsonAsync<List<Locator>>("api/locators");
            
            _allJigs = await jigsTask ?? new();
            _allLocators = await locsTask ?? new();
            
            FilterData();
        }
        catch (Exception ex) { _globalError = ex.Message; }
        _isLoading = false;
        StateHasChanged();
    }

    /// <summary>
    /// กรองรายการจิกตาม _searchQuery แล้วคำนวณ Pagination เพื่อแสดงผลบนตาราง
    /// ค้นหาจาก Id, SmartCodeName, PartNumber และ ToolNo
    /// </summary>
    private void FilterData()
    {
        var lower = _searchQuery.ToLowerInvariant();
        var query = string.IsNullOrWhiteSpace(lower) 
            ? _allJigs 
            : _allJigs.Where(j => 
                (j.Id?.ToLowerInvariant().Contains(lower) == true) || 
                (j.SmartCodeName?.ToLowerInvariant().Contains(lower) == true) ||
                (j.PartNumber?.ToLowerInvariant().Contains(lower) == true) ||
                (j.ToolNo?.ToLowerInvariant().Contains(lower) == true)
            );
            
        _totalItems = query.Count();

        // ปรับหน้าปัจจุบันหากเกินขอบเขต
        int maxPage = (int)Math.Ceiling(_totalItems / (double)_pageSize);
        if (maxPage == 0) maxPage = 1;
        if (_currentPage > maxPage) _currentPage = maxPage;

        _displayJigs = query.OrderBy(j => j.Id).Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
    }

    #endregion

    #region Export (CSV / PDF)
    /// <summary>
    /// ส่งออกข้อมูลจิก (ที่ผ่านตัวกรอง) เป็นไฟล์ CSV — รองรับทั้งภาษาไทยและอังกฤษ
    /// ใช้ Tab-prefix เพื่อป้องกัน Excel แปลงวันที่อัตโนมัติ
    /// </summary>
    private async Task ExportCsv()
    {
        var isThai = Lang.Current == "TH";
        var headers = isThai
            ? "รหัสจิก,Smart Code,Tool No,Step Print,Part Number,Rev,JigType,Process,HeightJig,Feed,Scan,QtyPrint,สถานะ,สภาพ,ตำแหน่ง,วันที่สร้าง"
            : "Jig ID,Smart Code,Tool No,Step Print,Part Number,Rev,JigType,Process,HeightJig,Feed,Scan,QtyPrint,Status,Condition,Locator,Created At";

        // ใส่ Tab นำหน้าเพื่อบังคับให้ Excel อ่านเป็นข้อความ (ป้องกันแปลงวันที่อัตโนมัติ)
        static string T(string? v) => string.IsNullOrEmpty(v) ? "" : $"\t{v}";

        var data = GetFilteredExportData();
        var rows = data.Select(j =>
            $"\"{j.Id}\",\"{j.SmartCodeName}\",\"{T(j.ToolNo)}\",\"{T(j.StepPrint)}\",\"{T(j.PartNumber)}\",\"{j.Rev}\",\"{j.JigType}\",\"{j.Process}\",\"{j.HeightJig}\",\"{T(j.Feed)}\",\"{T(j.Scan)}\",\"{j.QtyPrint}\",\"{j.Status}\",\"{j.Condition}\",\"{j.LocatorId}\",\"{j.CreatedAt:dd/MM/yyyy}\""
        );

        var csv = headers + "\n" + string.Join("\n", rows);
        var fileName = $"jigs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        await JSRuntime.InvokeVoidAsync("downloadTextFile", fileName, csv);
    }

    /// <summary>
    /// สร้าง HTML Report สำหรับจิก (พร้อม Badge สถานะ/สภาพ) แล้วเรียก JS เพื่อ Export เป็น PDF
    /// </summary>
    private async Task ExportPdf()
    {
        var isThai = Lang.Current == "TH";
        var date = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var title = isThai ? "รายงานข้อมูลจิก" : "Jigs Report";
        var generated = isThai ? "สร้างเมื่อ" : "Generated";
        var totalRec = isThai ? "จำนวนทั้งหมด" : "Total";
        var footer = isThai
            ? $"แมทเทล แบงคอก จำกัด &nbsp;|&nbsp; ระบบจัดการจิก &nbsp;|&nbsp; {date}"
            : $"Mattel Bangkok Limited &nbsp;|&nbsp; Jig Inventory Management System &nbsp;|&nbsp; {date}";

        var colId      = isThai ? "รหัสจิก"       : "Jig ID";
        var colSmart   = isThai ? "Smart Code"     : "Smart Code";
        var colTool    = isThai ? "Tool No"        : "Tool No";
        var colStep    = isThai ? "Step Print"     : "Step Print";
        var colPart    = isThai ? "Part Number"    : "Part Number";
        var colStatus  = isThai ? "สถานะ"          : "Status";
        var colCond    = isThai ? "สภาพ"           : "Condition";
        var colLocator = isThai ? "ตำแหน่ง"        : "Locator";

        var rows = string.Join("", GetFilteredExportData().Select(j => {
            var statusBadge = j.Status switch {
                "Available" => "badge-green",
                "InUse"     => "badge-blue",
                "Scrapped"  => "badge-red",
                _           => "badge-gray"
            };
            var condBadge = j.Condition switch {
                "Good"          => "badge-green",
                "NeedsCleaning" => "badge-amber",
                "Broken"        => "badge-red",
                _               => "badge-gray"
            };
            var statusLabel = isThai ? j.Status switch {
                "Available" => "พร้อมใช้", "InUse" => "กำลังใช้", "Scrapped" => "จำหน่ายออก", _ => j.Status
            } : j.Status;
            var condLabel = isThai ? j.Condition switch {
                "Good" => "ดี", "NeedsCleaning" => "ต้องล้าง", "Broken" => "ชำรุด", _ => j.Condition
            } : j.Condition;
            return $"<tr><td><strong>{j.Id}</strong></td><td>{j.SmartCodeName}</td><td>{j.ToolNo}</td><td>{j.StepPrint}</td><td>{j.PartNumber}</td><td><span class='badge {statusBadge}'>{statusLabel}</span></td><td><span class='badge {condBadge}'>{condLabel}</span></td><td>{j.LocatorId}</td></tr>";
        }));

        var html = $@"
<div class='pdf-header'>
  <div class='logo-text'>MATTEL</div>
  <div class='meta'>
    <div><strong>{title}</strong></div>
    <div>{generated}: {date}</div>
    <div>{totalRec}: {GetFilteredExportData().Count()}</div>
  </div>
</div>
<h2>{title}</h2>
<table>
  <thead>
    <tr><th>{colId}</th><th>{colSmart}</th><th>{colTool}</th><th>{colStep}</th><th>{colPart}</th><th>{colStatus}</th><th>{colCond}</th><th>{colLocator}</th></tr>
  </thead>
  <tbody>{rows}</tbody>
</table>
<div class='pdf-footer'>{footer}</div>";

        await JSRuntime.InvokeVoidAsync("exportPdf", title, html, $"jigs_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

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
    /// รีเซ็ตค่าตัวกรองการส่งออกทั้งหมดกลับเป็นค่าเริ่มต้น
    /// </summary>
    private void ResetExportFilter()
    {
        _filterToolNo = "";
        _filterJigType = "";
        _filterPartType = "";
        _filterStatus = "";
        _filterDateFrom = null;
        _filterDateTo = null;
    }

    /// <summary>
    /// กรองข้อมูลจิกตามเงื่อนไขที่ตั้งไว้ใน Export Filter (ToolNo, JigType, PartType, Status, วันที่)
    /// </summary>
    /// <returns>รายการจิกที่ผ่านตัวกรอง เรียงตาม Id</returns>
    private IEnumerable<Jig> GetFilteredExportData()
    {
        var q = _allJigs.AsEnumerable();
        if (!string.IsNullOrEmpty(_filterToolNo))   q = q.Where(j => j.ToolNo == _filterToolNo);
        if (!string.IsNullOrEmpty(_filterJigType))  q = q.Where(j => j.JigType == _filterJigType);
        if (!string.IsNullOrEmpty(_filterPartType)) q = q.Where(j => j.PartType == _filterPartType);
        if (!string.IsNullOrEmpty(_filterStatus))   q = q.Where(j => j.Status == _filterStatus);
        if (_filterDateFrom.HasValue) q = q.Where(j => j.CreatedAt.Date >= _filterDateFrom.Value.Date);
        if (_filterDateTo.HasValue)   q = q.Where(j => j.CreatedAt.Date <= _filterDateTo.Value.Date);
        return q.OrderBy(j => j.Id);
    }

    /// <summary>
    /// ดำเนินการส่งออกตามประเภทที่เลือก (csv/pdf) แล้วปิด Modal
    /// </summary>
    private async Task RunExport()
    {
        _showExportFilter = false;
        if (_exportType == "csv") await ExportCsv();
        else await ExportPdf();
    }

    #endregion

    #region CRUD Operations
    /// <summary>
    /// เปิด Modal สำหรับลงทะเบียนจิกใหม่ — รีเซ็ตฟอร์มทั้งหมดและตั้งค่าเริ่มต้น
    /// </summary>
    private void OpenAddModal()
    {
        _isEditMode = false;
        _editingJig = new Jig { Status = "Available", Condition = "Good" };
        _modalError = "";
        _compatiblePartsModel.Clear();
        _newPartInput = "";
        _selectedImageFile = null;
        _imagePreview = null;
        _selectedDate = null;
        _selectedSteps = new();
        _removeExistingImage = false;
        _showModal = true;
    }

    private void OpenEditModal(Jig jig)
    {
        _isEditMode = true;
        _editingJig = new Jig { 
            Uid = jig.Uid,
            Id = jig.Id, 
            SmartCodeName = jig.SmartCodeName, 
            ToolNo = jig.ToolNo, 
            StepPrint = jig.StepPrint, 
            PartType = jig.PartType, 
            Date = jig.Date, 
            Feed = jig.Feed, 
            Scan = jig.Scan, 
            QtyPrint = jig.QtyPrint, 
            HeightJig = jig.HeightJig, 
            JigType = jig.JigType, 
            Process = jig.Process, 
            PartNumber = jig.PartNumber, 
            Rev = jig.Rev, 
            Status = jig.Status, 
            Condition = jig.Condition, 
            LocatorId = jig.LocatorId,
            ImageUrl = jig.ImageUrl
        };
        _modalError = "";
        _compatiblePartsModel.Clear();
        _newPartInput = "";
        _selectedImageFile = null;
        _imagePreview = null;
        _removeExistingImage = false;
        _selectedDate = null;

        // แปลงวันที่เพื่อแสดงบนปฏิทิน
        if (!string.IsNullOrEmpty(_editingJig.Date))
        {
            if (DateTime.TryParseExact(_editingJig.Date, "dd/MM/yy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                _selectedDate = d;
        }

        _showModal = true;

        // แปลง Step Print สำหรับ Multi-select
        // ฐานข้อมูลเก็บเป็นตัวเลข เช่น "1-2-3" ต้องแปลงกลับเป็น Label เต็มเพื่อแสดงผล
        if (!string.IsNullOrEmpty(_editingJig.StepPrint))
        {
            _selectedSteps = _editingJig.StepPrint
                .Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Select(s => {
                    // ถ้าเป็น Label เต็มอยู่แล้ว เช่น "1 : L" ก็ใช้ค่าเดิม
                    if (s.Contains(':')) return s;
                    // ถ้าเป็นตัวเลข ให้แปลงเป็น Label เต็ม
                    if (int.TryParse(s, out _)) return StepNumberToLabel(s);
                    // รูปแบบข้อความเก่า เช่น "L", "R" → แปลงเป็น Label เต็มผ่าน mapping
                    if (_stepNameToNumber.TryGetValue(s, out var num)) return StepNumberToLabel(num);
                    return s;
                })
                .ToList();
        }
        else _selectedSteps = new();
        
        // โหลดข้อมูล Part ที่ผูกไว้แบบไม่รอผลลัพธ์
        _ = LoadPartsForJig(_editingJig.ToolNo ?? "");
    }

    /// <summary>
    /// เพิ่ม Step Print ที่เลือกจาก Dropdown เข้าไปในรายการ (ไม่ซ้ำ)
    /// </summary>
    private void AddStep(ChangeEventArgs e)
    {
        var val = e.Value?.ToString();
        if (!string.IsNullOrEmpty(val) && !_selectedSteps.Contains(val))
        {
            _selectedSteps.Add(val);
        }
    }

    /// <summary>
    /// ลบ Step Print ออกจากรายการที่เลือก
    /// </summary>
    private void RemoveStep(string step)
    {
        _selectedSteps.Remove(step);
    }

    private void HandlePartInputChanged(string value)
    {
        _newPartInput = value;
    }

    private void HandleDateChanged(DateTime? date)
    {
        SelectedDate = date;
    }

    /// <summary>
    /// โหลดรายการ Part Number ที่ผูกกับ ToolNo จาก API (ใช้ใน Edit Modal)
    /// </summary>
    /// <param name="toolNo">รหัสเครื่องมือที่ต้องการค้นหา Part</param>
    private async Task LoadPartsForJig(string toolNo)
    {
        if (string.IsNullOrWhiteSpace(toolNo)) return;
        try
        {
            var parts = await Api.GetFromJsonAsync<List<PartMaster>>($"api/jigs/parts/{toolNo}");
            if (parts != null) 
            {
                _compatiblePartsModel = parts;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch { /* ignore */ }
    }



    /// <summary>
    /// เพิ่ม Part Number ใหม่เข้ารายการ (ลบ Whitespace ก่อนเพิ่ม, ไม่อนุญาตค่าซ้ำ)
    /// </summary>
    private void AddPartNumber()
    {
        var p = CleanAllSpaces(_newPartInput) ?? "";
        if (!string.IsNullOrWhiteSpace(p) && !_compatiblePartsModel.Any(x => x.PartNumber.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            _compatiblePartsModel.Add(new PartMaster { PartNumber = p });
        }
        _newPartInput = "";
    }

    /// <summary>
    /// ลบ Part Number ออกจากรายการที่ผูกไว้
    /// </summary>
    private void RemovePartNumber(string part)
    {
        _compatiblePartsModel.RemoveAll(x => x.PartNumber == part);
    }

    private void HandlePartKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") AddPartNumber();
    }

    private void CloseModal() => _showModal = false;

    private string GetImageSrc(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return "";
        // ถ้าเป็น path สัมพัทธ์ เช่น /uploads/xxx.jpg ให้เติม URL ของ Backend นำหน้า
        if (imageUrl.StartsWith("/"))
            return $"http://localhost:5105{imageUrl}";
        return imageUrl;
    }

    private async Task OnImageSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file.Size > 5_000_000)
        {
            _modalError = "Image is too large. Max 5MB.";
            return;
        }

        _selectedImageFile = file;
        _removeExistingImage = false;

        // สร้างตัวอย่างรูปภาพ
        using var stream = file.OpenReadStream(5_000_000);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var buffer = ms.ToArray();
        _imagePreview = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer)}";
        StateHasChanged();
    }

    private void RemoveImage()
    {
        _selectedImageFile = null;
        _imagePreview = null;
        _removeExistingImage = true;
        _editingJig.ImageUrl = null;
        StateHasChanged();
    }

    /// <summary>
    /// สร้าง Smart Code Preview จากข้อมูลในฟอร์ม (ToolNo + Steps + PartType + Date + Feed/Scan + ...)
    /// ใช้แสดงตัวอย่างแบบ Real-time ขณะกรอกข้อมูล
    /// </summary>
    /// <returns>String ที่ประกอบจากฟิลด์ต่างๆ คั่นด้วยช่องว่าง</returns>
    private string GenerateSmartCodePreview()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_editingJig.ToolNo)) parts.Add(_editingJig.ToolNo);
        
        var steps = ExtractStepNumber(string.Join("-", _selectedSteps));
        if (steps != "-") parts.Add(steps);

        if (!string.IsNullOrWhiteSpace(_editingJig.PartType)) parts.Add(_editingJig.PartType);
        if (!string.IsNullOrWhiteSpace(_editingJig.Date)) parts.Add(_editingJig.Date);
        if (!string.IsNullOrWhiteSpace(_editingJig.Feed) && !string.IsNullOrWhiteSpace(_editingJig.Scan)) parts.Add($"{_editingJig.Feed}/{_editingJig.Scan}");
        else if (!string.IsNullOrWhiteSpace(_editingJig.Feed)) parts.Add(_editingJig.Feed);
        else if (!string.IsNullOrWhiteSpace(_editingJig.Scan)) parts.Add(_editingJig.Scan);
        if (!string.IsNullOrWhiteSpace(_editingJig.QtyPrint)) parts.Add(_editingJig.QtyPrint);
        if (!string.IsNullOrWhiteSpace(_editingJig.HeightJig)) parts.Add(_editingJig.HeightJig);
        if (!string.IsNullOrWhiteSpace(_editingJig.JigType)) parts.Add(_editingJig.JigType);
        if (!string.IsNullOrWhiteSpace(_editingJig.Process)) parts.Add(_editingJig.Process);
        return string.Join(" ", parts);
    }

    /// <summary>
    /// บันทึกข้อมูลจิก (สร้างใหม่หรืออัปเดต) พร้อม:
    /// 1. Sanitize ข้อมูลทุกฟิลด์ (CleanAllSpaces / NormalizeSpaces)
    /// 2. แปลง Step Print เป็นรูปแบบตัวเลข ("1-2-3")
    /// 3. อัปโหลด/ลบรูปภาพ (ถ้ามีการเปลี่ยนแปลง)
    /// 4. บันทึก Part Number Mappings แยกต่างหาก
    /// </summary>
    private async Task SaveJig()
    {
        // ทำความสะอาดข้อมูลทุกฟิลด์
        _editingJig.Id = CleanAllSpaces(_editingJig.Id) ?? "";
        _editingJig.ToolNo = CleanAllSpaces(_editingJig.ToolNo) ?? "";
        _editingJig.PartNumber = CleanAllSpaces(_editingJig.PartNumber) ?? "";
        _editingJig.LocatorId = CleanAllSpaces(_editingJig.LocatorId) ?? "";
        _editingJig.Rev = CleanAllSpaces(_editingJig.Rev);
        _editingJig.Date = CleanAllSpaces(_editingJig.Date);
        _editingJig.Feed = CleanAllSpaces(_editingJig.Feed);
        _editingJig.Scan = CleanAllSpaces(_editingJig.Scan);
        _editingJig.QtyPrint = CleanAllSpaces(_editingJig.QtyPrint);
        _editingJig.HeightJig = CleanAllSpaces(_editingJig.HeightJig);

        _editingJig.PartType = CleanAllSpaces(_editingJig.PartType);
        _editingJig.StepPrint = NormalizeSpaces(_editingJig.StepPrint);
        _editingJig.JigType = NormalizeSpaces(_editingJig.JigType);
        _editingJig.Process = NormalizeSpaces(_editingJig.Process);
        _editingJig.Status = CleanAllSpaces(_editingJig.Status) ?? "Available";
        _editingJig.Condition = CleanAllSpaces(_editingJig.Condition) ?? "Good";

        // ดึงเฉพาะตัวเลขจาก Step ที่เลือก
        var joinedSteps = string.Join("-", _selectedSteps);
        var extractedStr = ExtractStepNumber(joinedSteps);
        _editingJig.StepPrint = extractedStr == "-" ? "" : extractedStr;

        if (string.IsNullOrWhiteSpace(_editingJig.ToolNo)) { _modalError = Lang.T("กรุณากรอก Tool No. (ใช้สร้างรหัส JIG อัตโนมัติ)", "Tool No is required (used to auto-generate Jig ID)"); return; }
        
        // เติมค่า Part Number อัตโนมัติจาก mapping ตัวแรก (สำหรับแสดงผลบนตาราง)
        _editingJig.PartNumber = _compatiblePartsModel.FirstOrDefault()?.PartNumber ?? "";

        _isSubmitting = true; _modalError = "";
        try
        {
            _editingJig.SmartCodeName = GenerateSmartCodePreview();
            var response = _isEditMode 
                ? await Api.PutAsJsonAsync($"api/jigs/{_editingJig.Uid}", _editingJig) 
                : await Api.PostAsJsonAsync("api/jigs", _editingJig);
                
            if (response.IsSuccessStatusCode) 
            { 
                // สำหรับจิกใหม่ ดึง Uid จากผลลัพธ์ที่ API ส่งกลับ
                if (!_isEditMode)
                {
                    var created = await response.Content.ReadFromJsonAsync<Jig>();
                    if (created != null) _editingJig.Uid = created.Uid;
                }

                // อัปโหลดรูปภาพถ้ามีการเลือกไว้
                if (_selectedImageFile != null)
                {
                    _isUploadingImage = true;
                    StateHasChanged();
                    try
                    {
                        using var imgContent = new MultipartFormDataContent();
                        var imgStream = new StreamContent(_selectedImageFile.OpenReadStream(5_000_000));
                        imgStream.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(_selectedImageFile.ContentType);
                        imgContent.Add(imgStream, "file", _selectedImageFile.Name);
                        await Api.PostAsync($"api/jigs/{_editingJig.Uid}/image", imgContent);
                    }
                    catch { /* อัปโหลดรูปล้มเหลว แต่ข้อมูลจิกบันทึกสำเร็จแล้ว */ }
                    finally { _isUploadingImage = false; }
                }
                else if (_removeExistingImage)
                {
                    await Api.DeleteAsync($"api/jigs/{_editingJig.Uid}/image");
                }

                // บันทึก Part Number Mappings แยกต่างหาก
                await Api.PostAsJsonAsync($"api/jigs/parts/{_editingJig.ToolNo}", _compatiblePartsModel);

                await LoadData(); 
                _showModal = false; 
            }
            else { _modalError = await response.Content.ReadAsStringAsync(); }
        }
        catch (Exception ex) { _modalError = ex.Message; }
        finally { _isSubmitting = false; StateHasChanged(); }
    }

    /// <summary>
    /// ลบจิกหลังจากยืนยันด้วย SweetAlert — รีโหลดข้อมูลหลังลบสำเร็จ
    /// </summary>
    /// <param name="jig">จิกที่ต้องการลบ</param>
    private async Task DeleteJig(Jig jig)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirmAction", 
            Lang.T("ยืนยันการลบ", "Confirm Delete"), 
            Lang.T($"คุณต้องการลบจิก '{jig.Id}' ใช่หรือไม่? การดำเนินการนี้ไม่สามารถย้อนกลับได้", $"Delete jig '{jig.Id}'? This action cannot be undone."), 
            Lang.T("ใช่, ลบเลย", "Yes, delete"), "warning",
            Lang.T("ยกเลิก", "Cancel"));
        if (!confirmed) return;
        if ((await Api.DeleteAsync($"api/jigs/{jig.Uid}")).IsSuccessStatusCode) await LoadData();
    }

    /// <summary>
    /// นำเข้าข้อมูลจิกจากไฟล์ Excel (.xlsx / .xls) — ตรวจสอบนามสกุลและขนาดไฟล์ก่อนส่งไป API
    /// จำกัดขนาดสูงสุด 15MB
    /// </summary>
    private async Task UploadFiles(Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs e)
    {
        _isLoading = true;
        _globalError = "";
        StateHasChanged();

        try
        {
            var file = e.File;

            // ตรวจสอบไฟล์ฝั่ง Frontend
            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var ext = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                _globalError = Lang.T("ไฟล์ไม่ถูกต้อง กรุณาเลือกไฟล์ .xlsx หรือ .xls เท่านั้น", "Invalid file. Please select a .xlsx or .xls file.");
                _isLoading = false;
                StateHasChanged();
                return;
            }
            if (file.Size > 15 * 1024 * 1024)
            {
                _globalError = Lang.T("ไฟล์มีขนาดใหญ่เกินไป (สูงสุด 15MB)", "File is too large (max 15MB).");
                _isLoading = false;
                StateHasChanged();
                return;
            }

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(15 * 1024 * 1024));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await Api.PostAsync("api/jigs/upload", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadResult>();
                _globalError = Lang.T($"นำเข้าสำเร็จ: เพิ่ม {result?.Inserted} รายการ, อัปเดต {result?.Updated} รายการ", 
                                      $"Successfully imported: {result?.Inserted} inserted, {result?.Updated} updated.");
                await LoadData();
            }
            else
            {
                var t = await response.Content.ReadAsStringAsync();
                _globalError = $"Upload failed. Status: {response.StatusCode}. Details: {t}";
            }
        }
        catch (Exception ex)
        {
            _globalError = "Error uploading file: " + ex.Message;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Utility Methods
    /// <summary>
    /// โมเดลสำหรับรับผลลัพธ์การนำเข้า Excel จาก API
    /// </summary>
    public class UploadResult
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// ลบ Whitespace ทั้งหมดออกจากข้อความ (ใช้กับ ID, ToolNo, PartNumber ฯลฯ)
    /// </summary>
    /// <param name="val">ข้อความต้นฉบับ</param>
    /// <returns>ข้อความที่ไม่มี Whitespace หรือ null หากเป็นค่าว่าง</returns>
    private string? CleanAllSpaces(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
        return new string(val.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    /// <summary>
    /// ยุบ Whitespace ติดกันหลายตัวให้เหลือช่องว่างเดียว (ใช้กับ JigType, Process ที่มีช่องว่างตามปกติ)
    /// </summary>
    /// <param name="val">ข้อความต้นฉบับ</param>
    /// <returns>ข้อความที่ถูก Normalize หรือ null หากเป็นค่าว่าง</returns>
    private string? NormalizeSpaces(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
        return System.Text.RegularExpressions.Regex.Replace(val.Trim(), @"\s+", " ");
    }

    private void ToggleSelection(string jigId, object? isChecked)
    {
        if (isChecked is bool cb && cb) _selectedJigIds.Add(jigId);
        else _selectedJigIds.Remove(jigId);
    }

    private void ToggleRowSelection(string jigId)
    {
        ToggleSelection(jigId, !_selectedJigIds.Contains(jigId));
        StateHasChanged();
    }

    private void ToggleSelectAll(ChangeEventArgs e)
    {
        if ((bool)(e.Value ?? false)) foreach (var j in _displayJigs) _selectedJigIds.Add(j.Uid);
        else foreach (var j in _displayJigs) _selectedJigIds.Remove(j.Uid);
    }

    private void ClearSelection() => _selectedJigIds.Clear();

    private void ClearAll()
    {
        _searchQuery = "";
        _selectedJigIds.Clear();
        FilterData();
    }

    /// <summary>
    /// แปลง Step Print จากรูปแบบต่างๆ ("1 : L", "L", "1") ให้เป็นรูปแบบตัวเลข ("1-2-3")
    /// รองรับทั้งข้อมูลเก่า (text-based) และใหม่ (number-based) จากฐานข้อมูล
    /// </summary>
    /// <param name="stepStr">ข้อความ Step Print ที่ต้องการแปลง</param>
    /// <returns>ตัวเลข Step คั่นด้วย "-" หรือ "-" หากไม่มีข้อมูล</returns>
    private string ExtractStepNumber(string? stepStr)
    {
        if (string.IsNullOrEmpty(stepStr)) return "-";
        
        var items = stepStr.Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var resultNums = new List<string>();
        
        foreach (var item in items)
        {
            var trimmed = item.Trim();
            
            if (trimmed.Contains(':')) 
            {
                var prefix = trimmed.Split(':')[0].Trim();
                if (int.TryParse(prefix, out _)) { resultNums.Add(prefix); continue; }
            }
            
            if (int.TryParse(trimmed, out _)) { resultNums.Add(trimmed); continue; }
            
            // จัดการคำผสมเฉพาะก่อน
            var lower = trimmed.ToLowerInvariant();
            if (lower.Contains("re.hood") || lower.Contains("read hood")) { resultNums.Add("5"); continue; }
            if (lower.Contains("re.l")) { resultNums.Add("9"); continue; }
            if (lower.Contains("re.r")) { resultNums.Add("10"); continue; }
            
            // แยกคำด้วยเครื่องหมายวรรคตอน/ช่องว่าง เพื่อจับคู่แบบตรงทั้งคำ เช่น "L" โดยไม่ติด "WHEEL"
            var tokens = trimmed.Split(new[] { ' ', '.', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (_stepNameToNumber.TryGetValue(token, out var num))
                {
                    resultNums.Add(num);
                }
            }
        }
        
        var finalNums = resultNums.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        return finalNums.Any() ? string.Join("-", finalNums) : "-";
    }

    #endregion

    #region QR Code Printing
    /// <summary>
    /// สร้าง QR Code จาก Jig ID แล้วเรียก JS เพื่อเปิดหน้าต่างพิมพ์สติกเกอร์ QR
    /// </summary>
    /// <param name="jig">จิกที่ต้องการพิมพ์ QR Code</param>
    private async Task PrintQR(Jig jig)
    {
        var qr = QrCode.EncodeText(jig.Id, QrCode.Ecc.Low); // Low for small stickers
        var svgStr = qr.ToSvgString(4, "#000000", "#ffffff");
        var data = new { 
            id = jig.Id, 
            date = jig.Date, 
            toolNo = jig.ToolNo,
            stepPrint = ExtractStepNumber(jig.StepPrint),
            heightJig = jig.HeightJig,
            feed = jig.Feed,
            scan = jig.Scan,
            jigType = jig.JigType
        };
        await JSRuntime.InvokeVoidAsync("printQR", svgStr, data);
    }

    /// <summary>
    /// Export ป้าย QR Code เดี่ยวของจิกเป็นไฟล์ SVG (100×40mm) — ตั้งชื่อว่า label_[ID].svg
    /// </summary>
    /// <param name="jig">จิกที่ต้องการ export ป้าย SVG</param>
    private async Task ExportSingleSVG(Jig jig)
    {
        var qr = QrCode.EncodeText(jig.Id, QrCode.Ecc.Low);
        var svgStr = qr.ToSvgString(4, "#000000", "#ffffff");
        var data = new {
            id        = jig.Id,
            date      = jig.Date,
            toolNo    = jig.ToolNo,
            stepPrint = ExtractStepNumber(jig.StepPrint),
            heightJig = jig.HeightJig,
            feed      = jig.Feed,
            scan      = jig.Scan,
            jigType   = jig.JigType
        };
        await JSRuntime.InvokeVoidAsync("exportSingleSVG", svgStr, data);
    }

    /// <summary>
    /// พิมพ์ QR Code แบบ Batch สำหรับจิกที่ถูก Checkbox เลือกไว้ แล้วล้างการเลือกทั้งหมด
    /// </summary>
    private async Task PrintSelectedQRs()
    {
        if (!_selectedJigIds.Any()) return;
        var qrList = new List<object>();
        foreach (var uid in _selectedJigIds)
        {
            var jig = _allJigs.FirstOrDefault(j => j.Uid == uid);
            if (jig == null) continue;
            var qr = QrCode.EncodeText(jig.Id, QrCode.Ecc.Low);
            var svgStr = qr.ToSvgString(4, "#000000", "#ffffff");
            qrList.Add(new { 
                id = jig.Id, 
                svg = svgStr, 
                date = jig.Date, 
                toolNo = jig.ToolNo,
                stepPrint = ExtractStepNumber(jig.StepPrint),
                heightJig = jig.HeightJig,
                feed = jig.Feed,
                scan = jig.Scan,
                jigType = jig.JigType
            });
        }
        await JSRuntime.InvokeVoidAsync("printQRs", qrList);
        _selectedJigIds.Clear();
    }

    /// <summary>
    /// Export ป้าย QR Code ที่เลือกเป็นไฟล์ SVG — รองรับ Mimaki UJF-3042 MkII (bed 300mm) และ UJF-6042 MkII (bed 600mm)
    /// Label ขนาด 100×40mm | 3042 → 30 ใบ/sheet | 6042 → 60 ใบ/sheet
    /// </summary>
    /// <param name="bedWidth">ความกว้าง Print Bed: 300 สำหรับ 3042, 600 สำหรับ 6042</param>
    private async Task ExportSVGSheets(int bedWidth = 300)
    {
        if (!_selectedJigIds.Any()) return;
        var qrList = new List<object>();
        foreach (var uid in _selectedJigIds)
        {
            var jig = _allJigs.FirstOrDefault(j => j.Uid == uid);
            if (jig == null) continue;
            var qr = QrCode.EncodeText(jig.Id, QrCode.Ecc.Low);
            var svgStr = qr.ToSvgString(4, "#000000", "#ffffff");
            qrList.Add(new {
                id        = jig.Id,
                svg       = svgStr,
                date      = jig.Date,
                stepPrint = ExtractStepNumber(jig.StepPrint),
                heightJig = jig.HeightJig,
                feed      = jig.Feed,
                scan      = jig.Scan,
                jigType   = jig.JigType
            });
        }
        await JSRuntime.InvokeVoidAsync("exportSVGSheets", qrList, bedWidth);
        _selectedJigIds.Clear();
    }

    #endregion

    #region Display Helpers
    /// <summary>
    /// แปลง Locator ID เป็นชื่อตำแหน่งสำหรับแสดงผลบนตาราง
    /// </summary>
    /// <param name="id">รหัสตำแหน่งจัดเก็บ</param>
    /// <returns>ชื่อตำแหน่ง หรือ "-" หากไม่พบ</returns>
    private string GetLocatorName(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "-";
        var loc = _allLocators.FirstOrDefault(l => l.Id == id);
        return loc?.Name ?? id;
    }

    /// <summary>
    /// แปลงสถานะจิก (English) เป็นภาษาไทย
    /// </summary>
    private string GetStatusThai(string? status) => status switch
    {
        "Available" => "พร้อมใช้งาน",
        "InUse" => "กำลังใช้งาน",
        "Cleaning" => "กำลังทำความสะอาด",
        "Evaluation" => "รอประเมินสภาพ",
        "Lost" => "สูญหาย",
        _ => status ?? "-"
    };

    /// <summary>
    /// คืนค่า CSS Class สำหรับ Status Badge ตามสถานะจิก
    /// </summary>
    private string GetStatusBadgeClass(string? status) => status switch
    {
        "Available" => "bg-emerald-50 text-emerald-700 border-emerald-200",
        "InUse" => "bg-blue-50 text-blue-700 border-blue-200",
        "Cleaning" => "bg-amber-50 text-amber-700 border-amber-200",
        "Evaluation" => "bg-purple-50 text-purple-700 border-purple-200",
        "Lost" => "bg-slate-900 text-slate-400 border-slate-700",
        _ => "bg-slate-900 text-slate-300 border-slate-700"
    };
    #endregion
}