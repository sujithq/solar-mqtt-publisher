public sealed class Options
{
    public MqttOptions Mqtt { get; set; } = new();
    public DeviceOptions Device { get; set; } = new();
    public ApiOptions Api { get; set; } = new();

    // Logging & change detection
    public string? LogLevel { get; set; }
    public double? ValueEps { get; set; }

    // New array-based configuration (preferred)
    public ApiField[]? ApiFields { get; set; } =
    [
        new() { Name = "solar_total_kwh", Metric = "solar_energy_total_kwh" },
        new() { Name = "grid_import_kwh", Metric = "grid_import_total_kwh" },
        new() { Name = "grid_export_kwh", Metric = "grid_export_total_kwh" }
    ];
    public ApiHeader[]? ApiHeaders { get; set; } =
    [
        new() { Key = "Accept", Value = "application/json" }
    ];
}

public sealed class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string? Username { get; set; } = "mqtt_solar";
    public string? Password { get; set; }
    public string BaseTopic { get; set; } = "solar";
}

public sealed class DeviceOptions
{
    public string Name { get; set; } = "Rooftop PV";
    public string? Manufacturer { get; set; } = "Custom";
    public string? Model { get; set; } = "API";
    public string Identifiers { get; set; } = "pv-system-1";
    public string UniquePrefix { get; set; } = "pv1_";
}

public sealed class ApiOptions
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; } = new Dictionary<string, string>() { { "Accept","application/json" } };
    public string? Key { get; set; }
    public bool VerifySsl { get; set; } = true;
    public int TimeoutSec { get; set; } = 15;
    public int PollIntervalSec { get; set; } = 600;
    public FieldsOptions? Fields { get; set; } = new FieldsOptions
    {
        SolarTotalKwh = "solar_energy_total_kwh",
        GridImportKwh = "grid_import_total_kwh",
        GridExportKwh = "grid_export_total_kwh"
    };
}

public sealed class FieldsOptions
{
    public required string SolarTotalKwh { get; init; }
    public required string GridImportKwh { get; init; }
    public required string GridExportKwh { get; init; }
}

public sealed class ApiField
{
    public string Name { get; init; } = string.Empty;
    public string Metric { get; init; } = string.Empty;
}

public sealed class ApiHeader
{
    public string Key { get; init; } = string.Empty;
    public string? Value { get; init; }
}
