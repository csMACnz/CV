using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CVApp;
using CVApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ExperienceDataService>();
builder.Services.AddSingleton<SkillHighlightService>();
builder.Services.AddSingleton<PrintConfigurationService>();

#if DEBUG
// ADR-004 / ADR-011: Admin CMS service is only registered in Debug (local Aspire) builds.
builder.Services.AddScoped<ICvAdminStoreService, CvAdminStoreService>();
#endif

await builder.Build().RunAsync();
