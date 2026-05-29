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
/// หน้าสแกนเบิก/คืน/ล้างจิก — เลือกตำแหน่งปลายทางแล้วสแกน QR/กรอกรหัสจิก
/// ตัดสินประเภทธุรกรรมอัตโนมัติจาก Type ของ Locator (Production/Cleaning/Store)
/// เข้าถึงได้ Admin, ProdLead, Operator (ไม่รวม Engineer/Guest)
/// </summary>
public partial class ScanOut : ComponentBase
{

    private bool _isProcessing = false;
    private string _errorMessage = "";
    private string _successMessage = "";
    private string _manualJigId = "";
    private string _currentUser = "";

    private ElementReference _locationSearchRef;
    private ElementReference _jigInputRef;

    private string _selectedLocatorId = "";
    private List<Locator>? _locators;
    private List<ScanSessionItem> _sessionHistory = new();

    // --- Transfer Cart Mode ---
    private bool _isTransferMode = false;
    private List<TransferCartItem> _transferCart = new();
    private bool _isConfirmingTransfer = false;

    private bool IsInputDisabled => _isProcessing || (_isTransferMode ? false : string.IsNullOrEmpty(_selectedLocatorId));

    private string InputPlaceholder => _isTransferMode
        ? Lang.T("สแกนจิกเข้าตะกร้าขนย้าย...", "Scan Jig into transfer cart...")
        : (string.IsNullOrEmpty(_selectedLocatorId) ? Lang.T("เลือกสถานที่ก่อน...", "Select Location first...") : Lang.T("สแกนคิวอาร์โค้ดที่นี่...", "Scan QR Code here..."));

    /// <summary>โมเดลบันทึกการสแกนแต่ละรายการใน Session ปัจจุบัน</summary>
    public class ScanSessionItem
    {
        public string JigId { get; set; } = "";
        public string Action { get; set; } = "";
        public string Location { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string? TransactionId { get; set; }
        public bool IsCancelled { get; set; } = false;
    }

    /// <summary>โมเดลรายการในตะกร้าขนย้าย</summary>
    public class TransferCartItem
    {
        public string JigId { get; set; } = "";
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    /// <summary>ตรวจสอบสิทธิ์และโหลดรายการตำแหน่ง</summary>
    protected override async Task OnInitializedAsync()
    {
        var authObj = await Auth.GetAuthAsync();
        var role = authObj.Role ?? "";
        
        if (role == "Engineer" || role == "Guest" || string.IsNullOrEmpty(role))
        {
            NavManager.NavigateTo("/");
            return;
        }

        _currentUser = authObj.Name ?? "Unknown";
        _locators = await Api.GetFromJsonAsync<List<Locator>>("api/locators");
        
        // ไม่โหลดประวัติล่าสุดตอนเปิดหน้า — แสดงเฉพาะประวัติใน Session นี้เท่านั้น
    }

    protected override void OnInitialized()
    {
        Lang.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        Lang.OnChange -= StateHasChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // ตั้งค่าเริ่มต้นและ Focus ช่องกรอก
            await Task.Delay(100); 
            await SetFocusAsync();
            StateHasChanged();
        }
    }



    /// <summary>ล้างประวัติการสแกนใน Session นี้</summary>
    private void ClearSessionHistory()
    {
        _sessionHistory.Clear();
        StateHasChanged();
    }

    /// <summary>สลับโหมดปกติ ↔ ขนย้าย</summary>
    private void ToggleTransferMode()
    {
        _isTransferMode = !_isTransferMode;
        _errorMessage = "";
        _successMessage = "";
        _manualJigId = "";
        if (!_isTransferMode)
        {
            _transferCart.Clear();
        }
        StateHasChanged();
    }

    /// <summary>ลบรายการออกจากตะกร้าขนย้าย</summary>
    private void RemoveFromCart(TransferCartItem item)
    {
        _transferCart.Remove(item);
        StateHasChanged();
    }

    /// <summary>ล้างตะกร้าขนย้ายทั้งหมด</summary>
    private void ClearTransferCart()
    {
        _transferCart.Clear();
        _errorMessage = "";
        _successMessage = "";
        StateHasChanged();
    }

    /// <summary>ยืนยันขนย้ายทั้งหมด — เรียก API transfer-out แบบ Batch</summary>
    private async Task ConfirmTransfer()
    {
        if (_transferCart.Count == 0) return;

        var confirmed = await JSRuntime.InvokeAsync<bool>("confirmAction",
            Lang.T("ยืนยันการขนย้าย?", "Confirm Transfer?"),
            Lang.T($"ต้องการขนย้ายจิก {_transferCart.Count} รายการหรือไม่?", $"Transfer {_transferCart.Count} jig(s)?"),
            Lang.T("ยืนยัน", "Yes, Transfer"),
            "warning",
            Lang.T("ยกเลิก", "Cancel")
        );

        if (!confirmed) return;

        try
        {
            _isConfirmingTransfer = true;
            StateHasChanged();

            var request = new
            {
                JigIds = _transferCart.Select(c => c.JigId).ToArray(),
                User = _currentUser
            };

            var response = await Api.PostAsJsonAsync("api/transactions/transfer-out", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                var successCount = result.GetProperty("successCount").GetInt32();

                // เพิ่มเข้าประวัติ Session
                foreach (var item in _transferCart)
                {
                    _sessionHistory.Insert(0, new ScanSessionItem
                    {
                        JigId = item.JigId,
                        Action = "TransferOut",
                        Location = "InTransit",
                        Timestamp = DateTime.Now,
                        IsError = false
                    });
                }

                _transferCart.Clear();
                _successMessage = ""; // Clear banner, rely on Swal

                // แสดง failed items ถ้ามี
                if (result.TryGetProperty("failedIds", out var failedArr) && failedArr.GetArrayLength() > 0)
                {
                    var failedList = string.Join(", ", failedArr.EnumerateArray().Select(f => f.GetString()));
                    _errorMessage = Lang.T($"ไม่สำเร็จ: {failedList}", $"Failed: {failedList}");
                }

                _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = Lang.T("ขนย้ายสำเร็จ!", "Transfer Complete!"),
                    text = Lang.T($"ขนย้ายสำเร็จ {successCount} รายการ", $"{successCount} jig(s) transferred"),
                    icon = "success",
                    timer = 2500,
                    showConfirmButton = false
                });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _errorMessage = $"ไม่สำเร็จ: {error}";

                _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = "Error!",
                    text = error,
                    icon = "error",
                    confirmButtonColor = "#ef4444"
                });
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isConfirmingTransfer = false;
            StateHasChanged();
        }
    }

    private async Task HandleLocationKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrEmpty(_selectedLocatorId))
        {
            await SetFocusAsync(); // This will focus _jigInputRef since _selectedLocatorId is not empty
        }
    }

    /// <summary>ตั้งค่า Focus ตามลำดับ — ถ้ายังไม่เลือกตำแหน่ง (และไม่ใช่โหมดขนย้าย) จะ Focus ที่ช่องค้นหา, มิฉะนั้นจะ Focus ที่ช่องสแกนจิก</summary>
    private async Task SetFocusAsync()
    {
        try
        {
            if (!_isTransferMode && string.IsNullOrEmpty(_selectedLocatorId))
            {
                if (_locationSearchRef.Context != null)
                {
                    await _locationSearchRef.FocusAsync();
                }
            }
            else
            {
                if (_jigInputRef.Context != null)
                {
                    await _jigInputRef.FocusAsync();
                }
            }
        }
        catch { /* ไม่สนใจถ้า Focus ล้มเหลว */ }
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await ProcessManualEntry();
    }

    /// <summary>ประมวลผลรหัสจิกที่กรอกด้วยมือ</summary>
    private async Task ProcessManualEntry()
    {
        if (string.IsNullOrWhiteSpace(_manualJigId))
        {
            _errorMessage = "Please enter a Jig ID";
            return;
        }

        if (_isTransferMode)
        {
            await AddToTransferCart(_manualJigId.Trim());
        }
        else
        {
            await ProcessScan(_manualJigId.Trim());
        }
    }

    /// <summary>เพิ่มจิกเข้าตะกร้าขนย้าย — ตรวจสอบซ้ำและตรวจสอบว่าจิกมีอยู่จริง</summary>
    private async Task AddToTransferCart(string jigId)
    {
        jigId = CleanAllSpaces(jigId)?.ToUpperInvariant() ?? "";
        if (string.IsNullOrEmpty(jigId)) return;

        // ตรวจสอบซ้ำ
        if (_transferCart.Any(c => c.JigId == jigId))
        {
            _errorMessage = Lang.T($"จิก {jigId} อยู่ในตะกร้าแล้ว", $"Jig {jigId} is already in the cart");
            _manualJigId = "";
            StateHasChanged();
            await Task.Delay(50);
            await SetFocusAsync();

            _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
            {
                title = Lang.T("ซ้ำ!", "Duplicate!"),
                text = Lang.T($"จิก {jigId} อยู่ในตะกร้าแล้ว", $"Jig {jigId} is already in the cart"),
                icon = "warning",
                timer = 2000,
                showConfirmButton = false,
                toast = true,
                position = "top-end"
            });
            return;
        }

        // ตรวจสอบว่าจิกมีอยู่จริงผ่าน API
        try
        {
            var jigs = await Api.GetFromJsonAsync<List<shared.Models.Jig>>("api/jigs");
            var jig = jigs?.FirstOrDefault(j => string.Equals(CleanAllSpaces(j.Id), jigId, StringComparison.OrdinalIgnoreCase));

            if (jig == null)
            {
                _errorMessage = Lang.T($"ไม่พบจิก {jigId} ในระบบ", $"Jig {jigId} not found");
                _manualJigId = "";
                StateHasChanged();
                await Task.Delay(50);
                await SetFocusAsync();
                return;
            }

            if (jig.Status == "InTransit")
            {
                _errorMessage = Lang.T($"จิก {jigId} กำลังขนย้ายอยู่แล้ว", $"Jig {jigId} is already InTransit");
                _manualJigId = "";
                StateHasChanged();
                await Task.Delay(50);
                await SetFocusAsync();
                return;
            }

            if (jig.Status == "Evaluation" || jig.Status == "Lost")
            {
                _errorMessage = Lang.T($"ไม่สามารถขนย้ายจิก {jigId} ได้ (สถานะ: {jig.Status})", $"Cannot transfer jig {jigId} (Status: {jig.Status})");
                _manualJigId = "";
                StateHasChanged();
                await Task.Delay(50);
                await SetFocusAsync();
                return;
            }

            _transferCart.Add(new TransferCartItem { JigId = jigId, AddedAt = DateTime.Now });
            _errorMessage = "";
            _successMessage = ""; // Clear banner, rely on Swal toast

            _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
            {
                title = Lang.T("เพิ่มแล้ว!", "Added!"),
                text = $"{jigId}",
                icon = "success",
                timer = 1500,
                showConfirmButton = false,
                toast = true,
                position = "top-end"
            });
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
        }

        _manualJigId = "";
        StateHasChanged();
        await Task.Delay(50);
        await SetFocusAsync();
    }

    /// <summary>
    /// ประมวลผลการสแกน — ตัดสินประเภทธุรกรรมจาก Type ของ Locator:
    /// Production → CheckOut, Cleaning → CheckIn, Store → ReturnToStore
    /// </summary>
    private async Task ProcessScan(string jigId)
    {
        jigId = CleanAllSpaces(jigId)?.ToUpperInvariant() ?? "";
        _selectedLocatorId = CleanAllSpaces(_selectedLocatorId) ?? "";
        try
        {
            _isProcessing = true;
            _errorMessage = "";
            _successMessage = "";

            HttpResponseMessage response;

            // แปลงค่าที่พิมพ์ (ID หรือชื่อ) เป็นรหัส Locator จริง
            var actualLocatorId = _selectedLocatorId;
            var destName = _selectedLocatorId;
            
            var matchedLoc = _locators?.FirstOrDefault(l => 
                string.Equals(CleanAllSpaces(l.Id), _selectedLocatorId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(CleanAllSpaces(l.Name), _selectedLocatorId, StringComparison.OrdinalIgnoreCase));

            if (matchedLoc != null)
            {
                actualLocatorId = matchedLoc.Id;
                destName = matchedLoc.Name;
            }
            else
            {
                var errMsg = "ไม่พบข้อมูล Locator นี้ในระบบ โปรดตรวจสอบอีกครั้ง";
                _errorMessage = errMsg;
                
                // บันทึกการสแกนที่ล้มเหลวเข้าประวัติ
                _sessionHistory.Insert(0, new ScanSessionItem {
                    JigId = jigId,
                    Action = "Invalid Loc",
                    Location = _selectedLocatorId,
                    Timestamp = DateTime.Now,
                    IsError = true,
                    ErrorMessage = errMsg
                });

                _isProcessing = false;
                StateHasChanged();
                return;
            }

            string modeString = "out";
            
            if (matchedLoc.Type == "Production")
            {
                modeString = "Out to";
                var request = new { JigId = jigId, User = _currentUser, Destination = destName, LocatorId = actualLocatorId };
                response = await Api.PostAsJsonAsync("api/transactions/checkout", request);
            }
            else if (matchedLoc.Type == "Cleaning")
            {
                modeString = "Clean";
                var request = new { JigId = jigId, User = _currentUser, LocatorId = actualLocatorId };
                response = await Api.PostAsJsonAsync("api/transactions/checkin", request);
            }
            else // Store
            {
                modeString = "Store";
                var request = new { JigId = jigId, User = _currentUser, LocatorId = actualLocatorId };
                response = await Api.PostAsJsonAsync("api/transactions/returntostore", request);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                string actionTypeReturned = "";
                if (responseContent.TryGetProperty("actionType", out var at)) {
                    actionTypeReturned = at.GetString() ?? "";
                }

                var actionText = "เบิกออกสำเร็จ";
                if (actionTypeReturned == "Transfer") actionText = "โอนย้ายสำเร็จ";
                else if (matchedLoc.Type == "Cleaning") actionText = "ส่งล้างสำเร็จ";
                else if (matchedLoc.Type != "Production") actionText = "เก็บเข้าตู้สำเร็จ";

                _successMessage = $"{actionText}: {jigId}";
                
                // เพิ่มเข้าประวัติ Session พร้อม TransactionId
                string? txnId = null;
                if (responseContent.TryGetProperty("txnId", out var txnIdProp))
                    txnId = txnIdProp.GetString();

                _sessionHistory.Insert(0, new ScanSessionItem
                {
                    JigId = jigId,
                    Action = (actionTypeReturned == "Transfer") ? "Transfer" : modeString,
                    Location = destName,
                    Timestamp = DateTime.Now,
                    IsError = false,
                    TransactionId = txnId
                });

                _manualJigId = "";
                _isProcessing = false;
                StateHasChanged();
                await Task.Delay(50);
                await SetFocusAsync();

                _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = "Success!",
                    text = $"{actionText}\nJig: {jigId}",
                    icon = "success",
                    timer = 2000,
                    showConfirmButton = false,
                    toast = true,
                    position = "top-end"
                });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _errorMessage = $"ไม่สำเร็จ: {error}";
                
                // เพิ่มการสแกนที่ล้มเหลวเข้าประวัติ
                _sessionHistory.Insert(0, new ScanSessionItem
                {
                    JigId = jigId,
                    Action = modeString,
                    Location = destName,
                    Timestamp = DateTime.Now,
                    IsError = true,
                    ErrorMessage = error
                });

                _isProcessing = false;
                StateHasChanged();
                await Task.Delay(50);
                await SetFocusAsync();

                _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = "Error!",
                    text = error,
                    icon = "error",
                    confirmButtonColor = "#ef4444",
                    toast = true,
                    position = "top-end",
                    timer = 3000,
                    showConfirmButton = false
                });
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
            
            _sessionHistory.Insert(0, new ScanSessionItem
            {
                JigId = jigId,
                Action = "Error",
                Location = _selectedLocatorId,
                Timestamp = DateTime.Now,
                IsError = true,
                ErrorMessage = ex.Message
            });

            _isProcessing = false;
            StateHasChanged();
            await Task.Delay(50);
            await SetFocusAsync();
        }
    }

    /// <summary>แปลงชื่อตำแหน่งให้อ่านง่ายขึ้น — ตัดคำซ้ำซ้อนออกและแปลภาษาตามค่าที่เลือก</summary>
    private string TranslateLocation(string loc)
    {
        if (string.IsNullOrEmpty(loc)) return loc;

        if (loc.IndexOf("Zone", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var cleaned = loc.Replace("1 Cabinet Shelf", "").Replace("Cabinet Shelf", "")
                             .Replace("Cabinet", "").Replace("Shelf", "")
                             .Trim();
            if (cleaned.StartsWith("1 ")) cleaned = cleaned.Substring(2).Trim();
            return cleaned;
        }

        return loc switch {
            "Storage" => Lang.T("คลังสินค้า / ที่เก็บ (Storage)", "Storage"),
            "Production" => Lang.T("สายการผลิต (Production)", "Production"),
            "Cleaning" => Lang.T("สถานีล้าง (Cleaning)", "Cleaning Station"),
            _ when loc.Contains("Cabinet") => loc.Replace("Cabinet", Lang.T("ตู้", "Cabinet")).Replace("Shelf", Lang.T("ชั้น", "Shelf")),
            _ => loc
        };
    }

    /// <summary>ลบ Whitespace ทั้งหมดออกจากค่า — ใช้สำหรับเทียบค่าที่สแกน/กรอก</summary>
    private string? CleanAllSpaces(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
        return new string(val.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private string? _cancellingId = null;

    /// <summary>ยกเลิกรายการที่เพิ่งสแกนจากประวัติ Session</summary>
    private async Task CancelScanTransaction(ScanSessionItem item)
    {
        if (string.IsNullOrEmpty(item.TransactionId) || item.IsCancelled) return;

        var confirmed = await JSRuntime.InvokeAsync<bool>("confirmAction",
            Lang.T("ยืนยันการยกเลิก?", "Confirm Cancel?"),
            Lang.T($"ต้องการยกเลิกรายการ {item.JigId} หรือไม่?", $"Cancel transaction for {item.JigId}?"),
            Lang.T("ยืนยัน", "Yes, Cancel It"),
            "warning",
            Lang.T("ไม่", "Cancel")
        );

        if (!confirmed) return;

        try
        {
            _cancellingId = item.TransactionId;
            StateHasChanged();

            var response = await Api.PostAsJsonAsync($"api/transactions/cancel/{item.TransactionId}", new { });

            if (response.IsSuccessStatusCode)
            {
                item.IsCancelled = true;
                _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = Lang.T("ยกเลิกสำเร็จ!", "Cancelled!"),
                    text = Lang.T($"ยกเลิกรายการ {item.JigId} และกลับสถานะสำเร็จ", $"Transaction for {item.JigId} cancelled and status reverted"),
                    icon = "success",
                    timer = 2500,
                    showConfirmButton = false
                });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
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
            _ = JSRuntime.InvokeVoidAsync("Swal.fire", new
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

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

}