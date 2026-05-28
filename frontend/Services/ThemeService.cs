using Microsoft.JSInterop;

namespace frontend.Services
{
    /// <summary>
    /// บริการจัดการธีมของระบบ (Dark/Light) — เก็บค่าธีมใน localStorage และแจ้ง Component อัตโนมัติเมื่อเปลี่ยน
    /// </summary>
    public class ThemeService
    {
        private readonly IJSRuntime _js;

        /// <summary>ธีมปัจจุบัน: "Dark" หรือ "Light"</summary>
        public string Current { get; private set; } = "Light";

        /// <summary>true เมื่อธีมปัจจุบันเป็น Light — ใช้ใน Razor template แทน Current == "Light"</summary>
        public bool IsLight => Current == "Light";

        /// <summary>Event ที่ยิงเมื่อธีมเปลี่ยน — Component ที่ Subscribe จะ Re-render อัตโนมัติ</summary>
        public event Action? OnChange;

        public ThemeService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>โหลดค่าธีมจาก localStorage เมื่อเปิดแอปครั้งแรก และ apply class ไปที่ document body</summary>
        public async Task InitializeAsync()
        {
            try
            {
                var theme = await _js.InvokeAsync<string>("localStorage.getItem", "theme");
                if (!string.IsNullOrEmpty(theme) && (theme == "Dark" || theme == "Light"))
                {
                    Current = theme;
                }
                await ApplyThemeToDocumentAsync();
                NotifyStateChanged();
            }
            catch { /* Ignore if JS not ready */ }
        }

        /// <summary>เปลี่ยนธีมและบันทึกลง localStorage พร้อม apply class ไปที่ document</summary>
        public async Task SetTheme(string theme)
        {
            if (Current == theme) return;
            Current = theme;
            await _js.InvokeVoidAsync("localStorage.setItem", "theme", theme);
            await ApplyThemeToDocumentAsync();
            NotifyStateChanged();
        }

        /// <summary>สลับธีมระหว่าง Dark และ Light</summary>
        public async Task ToggleTheme()
        {
            await SetTheme(Current == "Dark" ? "Light" : "Dark");
        }

        /// <summary>Apply หรือ Remove class "theme-light" บน document.documentElement</summary>
        private async Task ApplyThemeToDocumentAsync()
        {
            try
            {
                if (Current == "Light")
                    await _js.InvokeVoidAsync("document.documentElement.classList.add", "theme-light");
                else
                    await _js.InvokeVoidAsync("document.documentElement.classList.remove", "theme-light");
            }
            catch { }
        }

        /// <summary>แจ้ง Component ที่ Subscribe ไว้ให้ Re-render</summary>
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
