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

        // REMOVED: All legacy mappings - enforce PascalCase schema

        return opts;
    }
}
