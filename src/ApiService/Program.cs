#if DEBUG
using System.Text.Json;
using System.Text.Json.Nodes;
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

var camelCaseOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var indentedCamelCaseOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

// ADR-008 / ADR-010: Serve aggregated experience data from the local content directory.
// ContentRoot config key lets the Aspire AppHost override the default path.
app.MapGet("/experience", (IConfiguration config, IWebHostEnvironment env) =>
{
    var payload = ContentAggregator.Aggregate(ResolveContentRoot(config, env));
    return Results.Ok(payload);
});

// ADR-004 / ADR-011: Admin aggregate read endpoint — returns the full CvDataSource graph
// for the local Admin UI. Bound exclusively to localhost via .NET Aspire service discovery.
app.MapGet("/api/admin/cv", (IConfiguration config, IWebHostEnvironment env) =>
{
    var payload = ContentAggregator.Aggregate(ResolveContentRoot(config, env));
    return Results.Ok(payload);
});

// ADR-004 / ADR-011: Admin profile write endpoint — updates only the Profile graph inside
// wwwroot/data/cv-datasource.json without disturbing other root keys (SkillMatrix, Timeline).
app.MapPut("/api/admin/profile", async (Profile profile, IConfiguration config, IWebHostEnvironment env) =>
{
    var path = ResolveCvDataSourcePath(config, env);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

    JsonObject root;
    try
    {
        var existing = await File.ReadAllTextAsync(path);
        root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
    }
    catch (FileNotFoundException)
    {
        root = new JsonObject();
    }

    root["profile"] = JsonSerializer.SerializeToNode(profile, camelCaseOptions);

    var updated = root.ToJsonString(indentedCamelCaseOptions);
    await File.WriteAllTextAsync(path, updated);

    return Results.Ok();
});

static string ResolveContentRoot(IConfiguration config, IWebHostEnvironment env) =>
    config["ContentRoot"]
    ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "content"));

static string ResolveCvDataSourcePath(IConfiguration config, IWebHostEnvironment env) =>
    config["CvDataSourcePath"]
    ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "CVApp", "wwwroot", "data", "cv-datasource.json"));
#endif

app.Run();
