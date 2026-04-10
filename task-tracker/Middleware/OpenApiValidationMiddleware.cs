using System.Text.Json;
using task_tracker.Models;

namespace task_tracker.Middleware;

/// <summary>
/// Spec-driven request validation middleware. Parses the OpenAPI YAML at
/// startup and validates incoming POST/PATCH request bodies against the
/// schemas defined in the specification.
///
/// The schema information is extracted from the YAML at startup and cached.
/// On each POST/PATCH request, the body is validated against the matching
/// schema's required fields, property types, and enum values.
/// </summary>
public class OpenApiValidationMiddleware
{
    private readonly RequestDelegate _next;

    private readonly Dictionary<string, SchemaInfo> _schemas;

    private readonly Dictionary<(string method, string path), string> _bodySchemaMap;

    public OpenApiValidationMiddleware(RequestDelegate next, string specPath)
    {
        _next = next;
        _schemas = new Dictionary<string, SchemaInfo>();
        _bodySchemaMap = new Dictionary<(string, string), string>();

        ParseSpec(specPath);
    }

    /// <summary>
    /// Parses the OpenAPI YAML spec to extract schema info for validation.
    /// This is a lightweight parser that reads the specific structure we need
    /// rather than a full YAML parser, keeping dependencies minimal.
    /// </summary>
    private void ParseSpec(string specPath)
    {
        var lines = File.ReadAllLines(specPath);

        ParseSchemas(lines);

        ParsePaths(lines);
    }

    private void ParseSchemas(string[] lines)
    {
        bool inComponents = false;
        bool inSchemas = false;
        string? currentSchema = null;
        bool inProperties = false;
        string? currentProperty = null;
        bool inEnum = false;
        bool inRequired = false;

        var schemaInfo = new SchemaInfo();
        var propInfo = new PropertyInfo();
        var requiredFields = new List<string>();
        var enumValues = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();

            if (line == "components:")
            {
                inComponents = true;
                continue;
            }

            if (inComponents && line == "  schemas:")
            {
                inSchemas = true;
                continue;
            }

            if (!inSchemas) continue;

            if (line.Length > 4 && line[0..4] == "    " && line[4] != ' ' && trimmed.EndsWith(":"))
            {
                if (currentSchema != null)
                {
                    if (currentProperty != null)
                    {
                        propInfo.EnumValues = new List<string>(enumValues);
                        schemaInfo.Properties[currentProperty] = propInfo;
                    }
                    schemaInfo.Required = new HashSet<string>(requiredFields);
                    _schemas[currentSchema] = schemaInfo;
                }

                currentSchema = trimmed.TrimEnd(':');
                schemaInfo = new SchemaInfo();
                inProperties = false;
                inRequired = false;
                inEnum = false;
                currentProperty = null;
                requiredFields = new List<string>();
                enumValues = new List<string>();
                propInfo = new PropertyInfo();
                continue;
            }

            if (currentSchema == null) continue;

            if (line.StartsWith("      required:"))
            {
                var match = line.IndexOf('[');
                if (match >= 0)
                {
                    var end = line.IndexOf(']');
                    if (end > match)
                    {
                        var items = line[(match + 1)..end].Split(',')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s));
                        requiredFields.AddRange(items);
                    }
                    inRequired = false;
                }
                else
                {
                    inRequired = true;
                }
                continue;
            }

            if (inRequired && trimmed.StartsWith("- "))
            {
                requiredFields.Add(trimmed[2..].Trim());
                continue;
            }
            else if (inRequired && !trimmed.StartsWith("- "))
            {
                inRequired = false;
            }

            if (line == "      properties:")
            {
                inProperties = true;
                continue;
            }

            if (!inProperties) continue;

            if (line.Length > 8 && line[0..8] == "        " && line[8] != ' ' && trimmed.Contains(":"))
            {
                if (currentProperty != null)
                {
                    propInfo.EnumValues = new List<string>(enumValues);
                    schemaInfo.Properties[currentProperty] = propInfo;
                }

                var colonIdx = trimmed.IndexOf(':');
                currentProperty = trimmed[..colonIdx].Trim();
                propInfo = new PropertyInfo();
                enumValues = new List<string>();
                inEnum = false;

                var rest = trimmed[(colonIdx + 1)..].Trim();
                if (rest.StartsWith("{"))
                {
                    ParseInlineProperty(rest, propInfo, enumValues);
                    propInfo.EnumValues = new List<string>(enumValues);
                }
                continue;
            }

            if (currentProperty != null && line.Length > 10 && line[0..10] == "          ")
            {
                if (trimmed.StartsWith("type:"))
                {
                    propInfo.Type = trimmed[5..].Trim();
                }
                else if (trimmed.StartsWith("enum:"))
                {
                    var bracket = trimmed.IndexOf('[');
                    if (bracket >= 0)
                    {
                        var end = trimmed.IndexOf(']');
                        if (end > bracket)
                        {
                            var items = trimmed[(bracket + 1)..end].Split(',')
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s));
                            enumValues.AddRange(items);
                        }
                    }
                    else
                    {
                        inEnum = true;
                    }
                }
                else if (inEnum && trimmed.StartsWith("- "))
                {
                    enumValues.Add(trimmed[2..].Trim());
                }
                else if (inEnum && !trimmed.StartsWith("- "))
                {
                    inEnum = false;
                }
            }
        }

        if (currentSchema != null)
        {
            if (currentProperty != null)
            {
                propInfo.EnumValues = new List<string>(enumValues);
                schemaInfo.Properties[currentProperty] = propInfo;
            }
            schemaInfo.Required = new HashSet<string>(requiredFields);
            _schemas[currentSchema] = schemaInfo;
        }
    }

    private void ParseInlineProperty(string inline, PropertyInfo propInfo, List<string> enumValues)
    {
        var content = inline.Trim('{', '}', ' ');

        var typeMatch = System.Text.RegularExpressions.Regex.Match(content, @"type:\s*(\w+)");
        if (typeMatch.Success)
        {
            propInfo.Type = typeMatch.Groups[1].Value;
        }

        var enumMatch = System.Text.RegularExpressions.Regex.Match(content, @"enum:\s*\[([^\]]+)\]");
        if (enumMatch.Success)
        {
            var items = enumMatch.Groups[1].Value.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s));
            enumValues.AddRange(items);
        }

        var formatMatch = System.Text.RegularExpressions.Regex.Match(content, @"format:\s*(\S+?)(?:,|$)");
        if (formatMatch.Success)
        {
            propInfo.Format = formatMatch.Groups[1].Value.Trim();
        }
    }

    private void ParsePaths(string[] lines)
    {
        string? currentPath = null;
        string? currentMethod = null;
        bool inRequestBody = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();

            if (line.Length > 2 && line[0..2] == "  " && line[2] != ' ' && !line.StartsWith("  -"))
            {
                var candidate = trimmed.TrimEnd(':');
                if (candidate == "/" || candidate.StartsWith("/"))
                {
                    currentPath = candidate;
                    currentMethod = null;
                    inRequestBody = false;
                }
            }

            if (currentPath != null && line.Length > 4 && line[0..4] == "    " && line[4] != ' ')
            {
                var method = trimmed.TrimEnd(':').ToUpper();
                if (method == "GET" || method == "POST" || method == "PATCH" ||
                    method == "PUT" || method == "DELETE")
                {
                    currentMethod = method;
                    inRequestBody = false;
                }
            }

            if (currentMethod != null && trimmed == "requestBody:")
            {
                inRequestBody = true;
            }

            if (inRequestBody && currentPath != null && currentMethod != null &&
                trimmed.Contains("$ref:") && trimmed.Contains("schemas/"))
            {
                var refIdx = trimmed.IndexOf("schemas/");
                if (refIdx >= 0)
                {
                    var schemaName = trimmed[(refIdx + 8)..].Trim().Trim('"', '\'');
                    _bodySchemaMap[(currentMethod, currentPath)] = schemaName;
                    inRequestBody = false;
                }
            }
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method.ToUpper();
        var path = context.Request.Path.Value ?? "/";

        if (method != "POST" && method != "PATCH")
        {
            await _next(context);
            return;
        }

        var matchedPath = MatchPath(path);
        if (matchedPath == null)
        {
            await _next(context);
            return;
        }

        if (!_bodySchemaMap.TryGetValue((method, matchedPath), out var schemaName) ||
            !_schemas.TryGetValue(schemaName, out var schemaInfo))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();
        string bodyText;
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            bodyText = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(bodyText))
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiError
            {
                Message = "Request body is required."
            });
            return;
        }

        JsonElement json;
        try
        {
            json = JsonSerializer.Deserialize<JsonElement>(bodyText);
        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiError
            {
                Message = "Request body is not valid JSON."
            });
            return;
        }

        var errors = new List<string>();

        foreach (var required in schemaInfo.Required)
        {
            var camel = ToCamelCase(required);
            if (!json.TryGetProperty(required, out _) &&
                !json.TryGetProperty(camel, out _))
            {
                errors.Add($"Missing required field: '{required}'.");
            }
        }

        foreach (var (propName, propInfo) in schemaInfo.Properties)
        {
            var camel = ToCamelCase(propName);
            JsonElement propValue;
            bool found = json.TryGetProperty(propName, out propValue) ||
                         json.TryGetProperty(camel, out propValue);

            if (!found) continue;

            switch (propInfo.Type.ToLower())
            {
                case "integer":
                    if (propValue.ValueKind != JsonValueKind.Number ||
                        !propValue.TryGetInt32(out _))
                    {
                        errors.Add($"Field '{propName}' must be an integer.");
                    }
                    break;

                case "number":
                    if (propValue.ValueKind != JsonValueKind.Number)
                    {
                        errors.Add($"Field '{propName}' must be a number.");
                    }
                    break;

                case "string":
                    if (propValue.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"Field '{propName}' must be a string.");
                    }
                    break;
            }

            if (propInfo.EnumValues.Count > 0 &&
                propValue.ValueKind == JsonValueKind.String)
            {
                var val = propValue.GetString();
                if (val != null && !propInfo.EnumValues.Contains(val))
                {
                    errors.Add($"Field '{propName}' must be one of: {string.Join(", ", propInfo.EnumValues)}. Got '{val}'.");
                }
            }
        }

        if (errors.Count > 0)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiError
            {
                Message = string.Join(" ", errors)
            });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Matches an actual request path to a spec path template.
    /// Tries exact match first, then parameterized patterns.
    /// </summary>
    private string? MatchPath(string actualPath)
    {
        var normalized = actualPath.TrimEnd('/');
        if (string.IsNullOrEmpty(normalized)) normalized = "/";

        foreach (var specPath in _bodySchemaMap.Keys.Select(k => k.path).Distinct())
        {
            if (string.Equals(specPath, normalized, StringComparison.OrdinalIgnoreCase))
                return specPath;
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 1)
        {
            foreach (var specPath in _bodySchemaMap.Keys.Select(k => k.path).Distinct())
            {
                if (specPath.Contains('{') &&
                    specPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length == 1)
                    return specPath;
            }
        }

        return null;
    }

    /// <summary>Converts "Title" to "title" (first char lowercase).</summary>
    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    private class SchemaInfo
    {
        public HashSet<string> Required { get; set; } = new();
        public Dictionary<string, PropertyInfo> Properties { get; set; } = new();
    }

    private class PropertyInfo
    {
        public string Type { get; set; } = "string";
        public string Format { get; set; } = "";
        public List<string> EnumValues { get; set; } = new();
    }
}
