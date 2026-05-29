using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using frontend;
using frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
var apiBase = baseUri.Port == 5200 ? $"{baseUri.Scheme}://{baseUri.Host}:5062/" : baseUri.ToString();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiClientService>();
builder.Services.AddScoped<LanguageService>();
builder.Services.AddScoped<ThemeService>();


await builder.Build().RunAsync();
