using System.Text.Json;
using System.Text.Json.Serialization;
using ContentManagement;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Aggregation <contentRoot> <outputFile>");
    return 1;
}

var contentRoot = args[0];
var outputFile = args[1];

if (!Directory.Exists(contentRoot))
{
    Console.Error.WriteLine($"Content directory not found: {contentRoot}");
    return 1;
}

var payload = ContentAggregator.Aggregate(contentRoot);

var outputDir = Path.GetDirectoryName(outputFile);
if (!string.IsNullOrEmpty(outputDir))
    Directory.CreateDirectory(outputDir);

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};
File.WriteAllText(outputFile, JsonSerializer.Serialize(payload, options));
Console.WriteLine($"Aggregated experience data written to: {outputFile}");
return 0;
