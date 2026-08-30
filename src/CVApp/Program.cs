using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CVApp;
using CVApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#if DEBUG
// ADR-004 / ADR-010 / ADR-011: In Debug (local Aspire) mode, route API requests to ApiService.
var apiBaseAddress = builder.Configuration["services:apiservice:https:0"]
    ?? builder.Configuration["services:apiservice:http:0"]
    ?? builder.Configuration["services:apiservice:default:0"]
    ?? builder.Configuration["ApiService:BaseUrl"]
    ?? builder.Configuration["ApiBaseUrl"]
    ?? "http://localhost:5112";

var apiUri = new Uri(apiBaseAddress.TrimEnd('/') + "/");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = apiUri });
builder.Services.AddScoped<ExperienceDataService>();
builder.Services.AddSingleton<SkillHighlightService>();
builder.Services.AddSingleton<PrintConfigurationService>();

// Admin CMS service is only registered in Debug (local Aspire) builds.
builder.Services.AddScoped<ICvAdminStoreService, CvAdminStoreService>();
#else
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ExperienceDataService>();
builder.Services.AddSingleton<SkillHighlightService>();
builder.Services.AddSingleton<PrintConfigurationService>();
#endif

await builder.Build().RunAsync();
