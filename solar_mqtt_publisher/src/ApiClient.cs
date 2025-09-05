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
        if (api.Headers is not null) // merged from api.headers + api_headers[] (if provided)
            foreach (var kv in api.Headers) req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

    LogHelper.Log(LogLevelSimple.Info, $"HTTP {api.Method ?? "GET"} {api.Url} timeout={api.Timeout_Sec}s verify_ssl={api.Verify_Ssl} headers={(api.Headers?.Count ?? 0)} auth={(string.IsNullOrWhiteSpace(api.Key) ? "none" : "bearer")}");
    var sw = System.Diagnostics.Stopwatch.StartNew();

        var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
    sw.Stop();
    LogHelper.Log(LogLevelSimple.Info, $"HTTP {(int)res.StatusCode} {api.Url} in {sw.ElapsedMilliseconds} ms bytes={json.Length}");
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
            if (node is not JsonObject rootObj || rootObj.Count == 0) return false;

            double sSum = 0, iSum = 0, eSumWh = 0; // eSumWh in Wh, convert later
            int recordCount = 0;

            foreach (var yearProp in rootObj)
            {
                if (yearProp.Value is not JsonArray arr) continue;
                foreach (var dayNode in arr)
                {
                    if (dayNode is not JsonObject dayObj) continue;
                    // Extract P (solar kWh), U (import kWh), I (export Wh)
                    if (TryGetNumber(dayObj, "P", out var p)) sSum += p;
                    if (TryGetNumber(dayObj, "U", out var u)) iSum += u;
                    if (TryGetNumber(dayObj, "I", out var i)) eSumWh += i; // still Wh
                    recordCount++;
                }
            }

            if (recordCount == 0) return false;

            solarKwh = sSum;
            importKwh = iSum;
            exportKwh = eSumWh / 1000.0; // convert Wh -> kWh
            LogHelper.Log(LogLevelSimple.Trace, $"Auto-detect totals: solar={solarKwh:F4}kWh import={importKwh:F4}kWh export={exportKwh:F4}kWh records={recordCount}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Log(LogLevelSimple.Warn, $"Auto-detect totals failed: {ex.Message}");
            return false;
        }
    }

    private static bool TryGetNumber(JsonObject obj, string key, out double value)
    {
        value = 0;
        if (!obj.TryGetPropertyValue(key, out var n) || n is null) return false;
        try
        {
            switch (n)
            {
                case JsonValue jv:
                    if (jv.TryGetValue<double>(out var d)) { value = d; return true; }
                    // Try string -> double
                    if (jv.TryGetValue<string>(out var s) && double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ds)) { value = ds; return true; }
                    break;
                default:
                    if (double.TryParse(n.ToJsonString().Trim('"'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dx)) { value = dx; return true; }
                    break;
            }
        }
        catch { }
        return false;
    }
}

// Minimal models � only what we need
public sealed class BarChartData
{
    [JsonPropertyName("D")] public int D { get; set; }
    [JsonPropertyName("P")] public double P { get; set; } // kWh
    [JsonPropertyName("U")] public double U { get; set; } // kWh
    [JsonPropertyName("I")] public double I { get; set; } // Wh (export)
                                                          // Q and others are present but ignored here
}