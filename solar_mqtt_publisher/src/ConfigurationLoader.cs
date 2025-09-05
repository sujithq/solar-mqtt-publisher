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
        cfg.Bind(opts);

        // Legacy flat env fallbacks
        MapFlat(cfg, "MQTT_HOST", v => opts.Mqtt.Host = v);
        MapFlat(cfg, "MQTT_PORT", v => { if (int.TryParse(v, out var p)) opts.Mqtt.Port = p; });
        MapFlat(cfg, "MQTT_USERNAME", v => opts.Mqtt.Username = v);
        MapFlat(cfg, "MQTT_PASSWORD", v => opts.Mqtt.Password = v);
        MapFlat(cfg, "MQTT_BASE_TOPIC", v => opts.Mqtt.Base_Topic = v);
        MapFlat(cfg, "API_URL", v => opts.Api.Url = v);
        MapFlat(cfg, "API_METHOD", v => opts.Api.Method = v);
        MapFlat(cfg, "API_TIMEOUT_SEC", v => { if (int.TryParse(v, out var p)) opts.Api.Timeout_Sec = p; });
        MapFlat(cfg, "API_POLL_INTERVAL_SEC", v => { if (int.TryParse(v, out var p)) opts.Api.Poll_Interval_Sec = p; });
        MapFlat(cfg, "API_KEY", v => opts.Api.Key = v);
        MapFlat(cfg, "API_VERIFY_SSL", v => { if (bool.TryParse(v, out var b)) opts.Api.Verify_Ssl = b; });
        MapFlat(cfg, "LOG_LEVEL", v => opts.Log_Level = v);
        MapFlat(cfg, "VALUE_EPS", v => { if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) opts.Value_Eps = d; });

        // If api_fields provided but Fields missing, synthesize
        if (opts.Api.Fields is null && opts.Api_Fields is { Length: > 0 })
        {
            var map = opts.Api_Fields.ToDictionary(f => f.Name, f => f.Metric, StringComparer.OrdinalIgnoreCase);
            if (map.TryGetValue("solar_total_kwh", out var s) &&
                map.TryGetValue("grid_import_kwh", out var gi) &&
                map.TryGetValue("grid_export_kwh", out var ge))
            {
                opts.Api.Fields = new FieldsOptions
                {
                    Solar_Total_Kwh = s,
                    Grid_Import_Kwh = gi,
                    Grid_Export_Kwh = ge
                };
            }
        }

        return opts;

        static void MapFlat(IConfiguration config, string name, Action<string> apply)
        {
            var val = config[name];
            if (!string.IsNullOrWhiteSpace(val)) apply(val);
        }
    }
}
