#!/usr/bin/env dotnet run
#:package NJsonSchema@11.5.2
#:package YamlDotNet@16.3.0
#:package Microsoft.Extensions.FileSystemGlobbing@10.0.0

using Microsoft.Extensions.FileSystemGlobbing;
using NJsonSchema;
using YamlDotNet.Serialization;

return await JsonSchemaValidatorApp.RunAsync(args);

internal static class JsonSchemaValidatorApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryParseArguments(args, out var options, out var parseError))
        {
            Console.Error.WriteLine(parseError);
            WriteUsage();
            return 1;
        }

        if (options.ShowHelp)
        {
            WriteUsage();
            return 0;
        }

        var schemaPath = Path.GetFullPath(options.SchemaPath!, options.BaseDirectory);
        if (!File.Exists(schemaPath))
        {
            Console.Error.WriteLine($"Schema file not found: {schemaPath}");
            return 1;
        }

        var files = ResolveFiles(options);
        if (files.Count == 0)
        {
            Console.Error.WriteLine("No files matched the provided --glob/--file arguments.");
            return 1;
        }

        JsonSchema schema;
        try
        {
            schema = await JsonSchema.FromFileAsync(schemaPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load schema '{schemaPath}': {ex.Message}");
            return 1;
        }

        var yamlDeserializer = new DeserializerBuilder().Build();
        var yamlJsonSerializer = new SerializerBuilder().JsonCompatible().Build();

        var failureCount = 0;

        foreach (var file in files)
        {
            string jsonPayload;

            try
            {
                var text = await File.ReadAllTextAsync(file);
                var extension = Path.GetExtension(file);

                jsonPayload = extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) ||
                              extension.Equals(".yml", StringComparison.OrdinalIgnoreCase)
                    ? yamlJsonSerializer.Serialize(yamlDeserializer.Deserialize<object>(text))
                    : text;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FAIL] {file}");
                Console.Error.WriteLine($"  Unable to parse file: {ex.Message}");
                failureCount++;
                continue;
            }

            ICollection<NJsonSchema.Validation.ValidationError> errors;
            try
            {
                errors = schema.Validate(jsonPayload);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FAIL] {file}");
                Console.Error.WriteLine($"  Validation execution failed: {ex.Message}");
                failureCount++;
                continue;
            }

            if (errors.Count == 0)
            {
                Console.WriteLine($"[PASS] {file}");
                continue;
            }

            Console.Error.WriteLine($"[FAIL] {file}");
            foreach (var error in errors)
            {
                var path = string.IsNullOrWhiteSpace(error.Path) ? "$" : error.Path;
                Console.Error.WriteLine($"  - {path}: {error}");
            }

            failureCount++;
        }

        Console.WriteLine($"Validated {files.Count} file(s) against schema '{schemaPath}'.");
        if (failureCount == 0)
        {
            Console.WriteLine("All files are valid.");
            return 0;
        }

        Console.Error.WriteLine($"Validation failed for {failureCount} file(s).");
        return 1;
    }

    private static HashSet<string> ResolveFiles(ValidatorOptions options)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileArg in options.Files)
        {
            var fullPath = Path.GetFullPath(fileArg, options.BaseDirectory);
            if (File.Exists(fullPath))
            {
                files.Add(fullPath);
            }
        }

        if (options.Globs.Count == 0)
        {
            return files;
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var pattern in options.Globs)
        {
            matcher.AddInclude(pattern.Replace('\\', '/'));
        }

        foreach (var matchedPath in matcher.GetResultsInFullPath(options.BaseDirectory))
        {
            if (File.Exists(matchedPath))
            {
                files.Add(Path.GetFullPath(matchedPath));
            }
        }

        return files;
    }

    private static bool TryParseArguments(string[] args, out ValidatorOptions options, out string error)
    {
        options = new ValidatorOptions
        {
            BaseDirectory = Directory.GetCurrentDirectory()
        };

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                case "--schema":
                    if (!TryGetValue(args, ref i, out var schemaPath))
                    {
                        error = "Missing value for --schema.";
                        return false;
                    }

                    options.SchemaPath = schemaPath;
                    break;
                case "--base-dir":
                    if (!TryGetValue(args, ref i, out var baseDir))
                    {
                        error = "Missing value for --base-dir.";
                        return false;
                    }

                    options.BaseDirectory = Path.GetFullPath(baseDir);
                    break;
                case "--glob":
                    if (!TryGetValue(args, ref i, out var globPattern))
                    {
                        error = "Missing value for --glob.";
                        return false;
                    }

                    options.Globs.Add(globPattern);
                    break;
                case "--file":
                    if (!TryGetValue(args, ref i, out var filePath))
                    {
                        error = "Missing value for --file.";
                        return false;
                    }

                    options.Files.Add(filePath);
                    break;
                default:
                    error = $"Unknown argument: {arg}";
                    return false;
            }
        }

        if (options.ShowHelp)
        {
            error = string.Empty;
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.SchemaPath))
        {
            error = "The --schema argument is required.";
            return false;
        }

        if (options.Globs.Count == 0 && options.Files.Count == 0)
        {
            error = "At least one --glob or --file argument is required.";
            return false;
        }

        if (!Directory.Exists(options.BaseDirectory))
        {
            error = $"Base directory does not exist: {options.BaseDirectory}";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryGetValue(string[] args, ref int index, out string value)
    {
        if (index + 1 >= args.Length)
        {
            value = string.Empty;
            return false;
        }

        value = args[++index];
        return true;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("JSON Schema Validator");
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run scripts/JsonSchemaValidator.cs -- --schema <path> [--base-dir <path>] [--glob <pattern> ...] [--file <path> ...]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run scripts/JsonSchemaValidator.cs -- --schema schemas/content/employer.schema.json --base-dir content --glob employment/**/employer.yaml");
        Console.WriteLine("  dotnet run scripts/JsonSchemaValidator.cs -- --schema schemas/compiled/experience.schema.json --base-dir src/CVApp/wwwroot/data --glob experience.json");
    }

    private sealed class ValidatorOptions
    {
        public string? SchemaPath { get; set; }
        public string BaseDirectory { get; set; } = string.Empty;
        public bool ShowHelp { get; set; }
        public List<string> Globs { get; } = [];
        public List<string> Files { get; } = [];
    }
}
