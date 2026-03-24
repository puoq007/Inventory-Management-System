using Microsoft.JSInterop;

namespace frontend.Services
{
    public class LanguageService
    {
        private readonly IJSRuntime _js;
        public string Current { get; private set; } = "EN";
        public event Action? OnChange;

        public LanguageService(IJSRuntime js)
        {
            _js = js;
        }

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

        public async Task SetLanguage(string lang)
        {
            if (Current == lang) return;
            Current = lang;
            await _js.InvokeVoidAsync("localStorage.setItem", "lang", lang);
            NotifyStateChanged();
        }

        public string T(string th, string en) => Current == "TH" ? th : en;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
