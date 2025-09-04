using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public static class ApiClient
{
    public static async Task<JsonNode> FetchAsync(ApiOptions api, CancellationToken ct)
    {
        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => api.Verify_Ssl };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(api.Timeout_Sec) };
        using var req = new HttpRequestMessage(new HttpMethod(api.Method ?? "GET"), api.Url);
        if (!string.IsNullOrWhiteSpace(api.Key))
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api.Key);
        if (api.Headers is not null)
            foreach (var kv in api.Headers) req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

        var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonNode.Parse(json)!;
    }

    // --- Your original dotted-path helper (kept as fallback) ---
    public static double GetDouble(JsonNode node, string dottedPath)
    {
        var cur = node;
        foreach (var part in dottedPath.Split('.'))
        {
            cur = cur?[part] ?? throw new KeyNotFoundException($"Path '{dottedPath}' not found.");
        }
        return cur!.GetValue<double>();
    }

    // --- NEW: Auto-detect & sum your Dictionary<int, List<BarChartData>> schema ---
    public static bool TryComputeMyEnergyTotals(JsonNode node, out double solarKwh, out double importKwh, out double exportKwh)
    {
        solarKwh = importKwh = exportKwh = 0d;
        try
        {
            // The JSON object has year keys, so deserialize to Dictionary<string, List<BarChartData>>
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var dict = node.Deserialize<Dictionary<string, List<BarChartData>>>(opts);
            if (dict is null || dict.Count == 0) return false;

            var allDays = dict.Values.SelectMany(x => x ?? Enumerable.Empty<BarChartData>());

            // P and U are kWh; I is Wh (convert to kWh)
            solarKwh = allDays.Where(d => d is not null).Sum(d => d.P);
            importKwh = allDays.Where(d => d is not null).Sum(d => d.U);
            exportKwh = allDays.Where(d => d is not null).Sum(d => d.I) / 1000.0;

            // If everything is zero and there were no entries, consider it a miss
            return dict.Values.Any(v => v is { Count: > 0 });
        }
        catch
        {
            return false;
        }
    }
}

// Minimal models – only what we need
public sealed class BarChartData
{
    [JsonPropertyName("D")] public int D { get; set; }
    [JsonPropertyName("P")] public double P { get; set; } // kWh
    [JsonPropertyName("U")] public double U { get; set; } // kWh
    [JsonPropertyName("I")] public double I { get; set; } // Wh (export)
                                                          // Q and others are present but ignored here
}