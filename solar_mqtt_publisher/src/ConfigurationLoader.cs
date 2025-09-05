using Microsoft.Extensions.Configuration;

public static class ConfigurationLoader
{
    // Build layered configuration and bind to Options POCO
    // Default path uses correct folder casing for Linux (case-sensitive FS)
    public static Options Load(string? basePath = null, string defaultOptions = "default/options.json", string supervisorOptions = "/data/options.json")
    {
        basePath ??= AppContext.BaseDirectory;

        // Resolve defaultOptions with case fallback (handles existing deployments using "data/...")
        string primary = Path.Combine(basePath, defaultOptions);
        string alt = defaultOptions.Contains("Data/")
            ? Path.Combine(basePath, defaultOptions.Replace("Data/", "data/"))
            : Path.Combine(basePath, defaultOptions.Replace("data/", "Data/"));

        string toLoad;
        if (File.Exists(primary)) toLoad = primary;
        else if (File.Exists(alt)) toLoad = alt;
        else throw new FileNotFoundException($"Configuration defaultOptions not found. Tried '{primary}' and '{alt}'");

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath);

        // Load embedded defaults FIRST
        LogHelper.Log(LogLevelSimple.Info, $"[CONFIG] Loading embedded defaults: {toLoad}");
        builder.AddJsonFile(toLoad, optional: false, reloadOnChange: false);

        // THEN layer Supervisor user configuration so it overrides defaults
        if (File.Exists(supervisorOptions))
        {
            LogHelper.Log(LogLevelSimple.Info, $"[CONFIG] Loading supervisor overrides: {supervisorOptions}");
            builder.AddJsonFile(supervisorOptions, optional: false, reloadOnChange: false);
        }
        else
        {
            LogHelper.Log(LogLevelSimple.Info, "[CONFIG] Supervisor options defaultOptions not found (expected /data/options.json) - using defaults only");
        }

        builder
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables(prefix: "SOLAR_") // hierarchical: SOLAR_MQTT__HOST etc.
            .AddEnvironmentVariables(); // allow legacy flat vars like MQTT_HOST

        var cfg = builder.Build();
        var opts = new Options();
        cfg.Bind(opts); // bind whatever matches new names

        // Backward compatibility: map legacy snake_case keys if present
        // We intentionally read raw configuration values rather than environment again.
        SetIfPresent(cfg, "Log_Level", v => opts.LogLevel = v);
        SetIfPresent(cfg, "Value_Eps", v => opts.ValueEps = ParseDouble(v));
        SetIfPresent(cfg, "Mqtt:Base_Topic", v => opts.Mqtt.BaseTopic = v);
        SetIfPresent(cfg, "Device:Unique_Prefix", v => opts.Device.UniquePrefix = v);
        SetIfPresent(cfg, "Api:Verify_Ssl", v => opts.Api.VerifySsl = ParseBool(v, opts.Api.VerifySsl));
        SetIfPresent(cfg, "Api:Timeout_Sec", v => opts.Api.TimeoutSec = ParseInt(v, opts.Api.TimeoutSec));
        SetIfPresent(cfg, "Api:Poll_Interval_Sec", v => opts.Api.PollIntervalSec = ParseInt(v, opts.Api.PollIntervalSec));

        // Legacy arrays names
        if (opts.ApiFields is null && cfg.GetSection("Api_Fields").Exists())
        {
            opts.ApiFields = cfg.GetSection("Api_Fields").Get<ApiField[]>();
        }
        if (opts.ApiHeaders is null && cfg.GetSection("Api_Headers").Exists())
        {
            opts.ApiHeaders = cfg.GetSection("Api_Headers").Get<ApiHeader[]>();
        }

        // Legacy flat env fallbacks
        MapFlat(cfg, "MQTT_HOST", v => opts.Mqtt.Host = v);
        MapFlat(cfg, "MQTT_PORT", v => { if (int.TryParse(v, out var p)) opts.Mqtt.Port = p; });
        MapFlat(cfg, "MQTT_USERNAME", v => opts.Mqtt.Username = v);
        MapFlat(cfg, "MQTT_PASSWORD", v => opts.Mqtt.Password = v);
        MapFlat(cfg, "MQTT_BASE_TOPIC", v => opts.Mqtt.BaseTopic = v);
        MapFlat(cfg, "API_URL", v => opts.Api.Url = v);
        MapFlat(cfg, "API_METHOD", v => opts.Api.Method = v);
        MapFlat(cfg, "API_TIMEOUT_SEC", v => { if (int.TryParse(v, out var p)) opts.Api.TimeoutSec = p; });
        MapFlat(cfg, "API_POLL_INTERVAL_SEC", v => { if (int.TryParse(v, out var p)) opts.Api.PollIntervalSec = p; });
        MapFlat(cfg, "API_KEY", v => opts.Api.Key = v);
        MapFlat(cfg, "API_VERIFY_SSL", v => { if (bool.TryParse(v, out var b)) opts.Api.VerifySsl = b; });
        MapFlat(cfg, "LOG_LEVEL", v => opts.LogLevel = v);
        MapFlat(cfg, "VALUE_EPS", v => { if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) opts.ValueEps = d; });

        // If api_fields provided but Fields missing, synthesize
        if (opts.Api.Fields is null && opts.ApiFields is { Length: > 0 })
        {
            var map = opts.ApiFields.ToDictionary(f => f.Name, f => f.Metric, StringComparer.OrdinalIgnoreCase);
            if (map.TryGetValue("solar_total_kwh", out var s) &&
                map.TryGetValue("grid_import_kwh", out var gi) &&
                map.TryGetValue("grid_export_kwh", out var ge))
            {
                opts.Api.Fields = new FieldsOptions
                {
                    SolarTotalKwh = s,
                    GridImportKwh = gi,
                    GridExportKwh = ge
                };
            }
        }

        return opts;

        static void MapFlat(IConfiguration config, string name, Action<string> apply)
        {
            var val = config[name];
            if (!string.IsNullOrWhiteSpace(val)) apply(val);
        }

        static void SetIfPresent(IConfiguration cfg, string key, Action<string> assign)
        {
            var v = cfg[key];
            if (!string.IsNullOrWhiteSpace(v)) assign(v);
        }
        static bool ParseBool(string raw, bool fallback) => bool.TryParse(raw, out var b) ? b : fallback;
        static int ParseInt(string raw, int fallback) => int.TryParse(raw, out var i) ? i : fallback;
        static double? ParseDouble(string raw) => double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
