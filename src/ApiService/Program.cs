#if DEBUG
using ContentManagement;
#endif

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
// ADR-004 / ADR-008: Local CMS API is available only in Debug (local Aspire) builds.
// Allow any origin so the Blazor WASM client can call this API from its own dev-server port.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
#endif

var app = builder.Build();

#if DEBUG
app.UseCors();

// ADR-008 / ADR-010: Serve aggregated experience data from the local content directory.
// ContentRoot config key lets the Aspire AppHost override the default path.
app.MapGet("/experience", (IConfiguration config, IWebHostEnvironment env) =>
{
    var contentRoot = config["ContentRoot"]
        ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "content"));

    var payload = ContentAggregator.Aggregate(contentRoot);
    return Results.Ok(payload);
});

// ADR-004 / ADR-011: Admin aggregate read endpoint — returns the full CvDataSource graph
// for the local Admin UI. Bound exclusively to localhost via .NET Aspire service discovery.
app.MapGet("/api/admin/cv", (IConfiguration config, IWebHostEnvironment env) =>
{
    var contentRoot = config["ContentRoot"]
        ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "content"));

    var payload = ContentAggregator.Aggregate(contentRoot);
    return Results.Ok(payload);
});
#endif

app.Run();
