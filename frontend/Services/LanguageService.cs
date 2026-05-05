using Microsoft.JSInterop;

namespace frontend.Services
{
    /// <summary>
    /// บริการจัดการภาษา (ไทย/อังกฤษ) — เก็บค่าภาษาใน localStorage และแจ้ง Component อัตโนมัติเมื่อเปลี่ยน
    /// </summary>
    public class LanguageService
    {
        private readonly IJSRuntime _js;
        /// <summary>ภาษาปัจจุบัน: "TH" หรือ "EN"</summary>
        public string Current { get; private set; } = "EN";
        /// <summary>Event ที่ยิงเมื่อภาษาเปลี่ยน — Component ที่ Subscribe จะ Re-render อัตโนมัติ</summary>
        public event Action? OnChange;

        public LanguageService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>โหลดค่าภาษาจาก localStorage เมื่อเปิดแอปครั้งแรก</summary>
        public async Task InitializeAsync()
        {
            try
            {
                var lang = await _js.InvokeAsync<string>("localStorage.getItem", "lang");
                if (!string.IsNullOrEmpty(lang) && (lang == "TH" || lang == "EN"))
                {
                    Current = lang;
                    NotifyStateChanged();
                }
            }
            catch { /* Ignore if JS not ready */ }
        }

        /// <summary>เปลี่ยนภาษาและบันทึกลง localStorage</summary>
        public async Task SetLanguage(string lang)
        {
            if (Current == lang) return;
            Current = lang;
            await _js.InvokeVoidAsync("localStorage.setItem", "lang", lang);
            NotifyStateChanged();
        }

        /// <summary>คืนข้อความตามภาษาที่เลือก — ใช้ใน Razor เพื่อแสดงข้อความ 2 ภาษา</summary>
        public string T(string th, string en) => Current == "TH" ? th : en;

        /// <summary>แจ้ง Component ที่ Subscribe ไว้ให้ Re-render</summary>
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
