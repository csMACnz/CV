var builder = DistributedApplication.CreateBuilder(args);

var contentDir = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "content"));
var cvDataSourcePath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "CVApp", "wwwroot", "data", "cv-datasource.json"));

var apiService = builder.AddProject<Projects.ApiService>("apiservice")
    .WithEnvironment("ContentDirectory", contentDir)
    .WithEnvironment("CvDataSourcePath", cvDataSourcePath);

builder.AddProject<Projects.CVApp>("cvapp")
    .WithReference(apiService);

builder.Build().Run();
