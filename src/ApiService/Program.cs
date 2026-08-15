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

var cvDataSourceStore = new CvDataSourceFileStore();

// ADR-008 / ADR-010: Serve aggregated experience data from the local content directory.
// ContentRoot config key lets the Aspire AppHost override the default path.
app.MapGet("/experience", (IConfiguration config, IWebHostEnvironment env) =>
{
    var payload = ContentAggregator.Aggregate(ResolveContentRoot(config, env));
    return Results.Ok(payload);
});

// ADR-004 / ADR-011: Admin aggregate read endpoint — returns the full CvDataSource graph
// for the local Admin UI. Bound exclusively to localhost via .NET Aspire service discovery.
app.MapGet("/api/admin/cv", async (IConfiguration config, IWebHostEnvironment env) =>
{
    var cvDataSource = await cvDataSourceStore.LoadAsync(
        ResolveCvDataSourcePath(config, env),
        () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)));
    return Results.Ok(cvDataSource);
});

// ADR-004 / ADR-011: Admin profile write endpoint — updates only the Profile graph inside
// wwwroot/data/cv-datasource.json without disturbing other root keys (SkillMatrix, Timeline).
app.MapPut("/api/admin/profile", async (Profile profile, IConfiguration config, IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.UpdateProfileAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            profile));
});

app.MapPost("/api/admin/skills/categories", async (CreateSkillCategoryRequest request, IConfiguration config, IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.AddSkillCategoryAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            request.Name));
});

app.MapPut("/api/admin/skills/categories/{categoryName}", async (
    string categoryName,
    RenameSkillCategoryRequest request,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.RenameSkillCategoryAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            categoryName,
            request.NewName));
});

app.MapDelete("/api/admin/skills/categories/{categoryName}", async (
    string categoryName,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.DeleteSkillCategoryAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            categoryName));
});

app.MapPost("/api/admin/skills", async (CreateSkillRequest request, IConfiguration config, IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.AddSkillAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            request.CategoryName,
            new Skill(request.Id, request.Name, request.Url)));
});

app.MapPut("/api/admin/skills/{skillId}", async (
    string skillId,
    UpdateSkillRequest request,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.UpdateSkillAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            skillId,
            request.Name,
            request.Url));
});

app.MapDelete("/api/admin/skills/{skillId}", async (
    string skillId,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.DeleteSkillAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            skillId));
});

// ADR-004 / ADR-011: Timeline CRUD endpoints — manage TimelineEntry items and nested
// Project cards without disturbing Profile or SkillMatrix root keys.
app.MapPost("/api/admin/timeline", async (CreateTimelineEntryRequest request, IConfiguration config, IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.AddTimelineEntryAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            request.Company,
            request.Period,
            request.Location,
            request.RoleTitle,
            request.Start,
            request.End));
});

app.MapPut("/api/admin/timeline/{entryId}", async (
    string entryId,
    UpdateTimelineEntryRequest request,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.UpdateTimelineEntryAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            entryId,
            request.Company,
            request.Period,
            request.Location,
            request.RoleTitle,
            request.Start,
            request.End));
});

app.MapDelete("/api/admin/timeline/{entryId}", async (
    string entryId,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.DeleteTimelineEntryAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            entryId));
});

app.MapPost("/api/admin/timeline/{entryId}/projects", async (
    string entryId,
    CreateProjectRequest request,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.AddProjectAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            entryId,
            request.Name,
            request.BriefSummary,
            request.Narrative,
            request.AppliedSkillIds));
});

app.MapPut("/api/admin/timeline/{entryId}/projects/{projectId}", async (
    string entryId,
    string projectId,
    UpdateProjectRequest request,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.UpdateProjectAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            entryId,
            projectId,
            request.Name,
            request.BriefSummary,
            request.Narrative,
            request.AppliedSkillIds));
});

app.MapDelete("/api/admin/timeline/{entryId}/projects/{projectId}", async (
    string entryId,
    string projectId,
    IConfiguration config,
    IWebHostEnvironment env) =>
{
    return await ExecuteMutationAsync(() =>
        cvDataSourceStore.DeleteProjectAsync(
            ResolveCvDataSourcePath(config, env),
            () => ContentAggregator.Aggregate(ResolveContentRoot(config, env)),
            entryId,
            projectId));
});

static string ResolveContentRoot(IConfiguration config, IWebHostEnvironment env) =>
    config["ContentRoot"]
    ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "content"));

static string ResolveCvDataSourcePath(IConfiguration config, IWebHostEnvironment env) =>
    config["CvDataSourcePath"]
    ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "CVApp", "wwwroot", "data", "cv-datasource.json"));

static async Task<IResult> ExecuteMutationAsync(Func<Task> action)
{
    try
    {
        await action();
        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
}

#endif

app.Run();
