public sealed class Options
{
    public MqttOptions Mqtt { get; set; } = new();
    public DeviceOptions Device { get; set; } = new();
    public ApiOptions Api { get; set; } = new();

    // Logging & change detection
    public string? LogLevel { get; set; }
    public double? ValueEps { get; set; }

    // New array-based configuration (preferred)
    public ApiField[]? ApiFields { get; set; }
    public ApiHeader[]? ApiHeaders { get; set; }
}

public sealed class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string BaseTopic { get; set; } = "solar";
}

public sealed class DeviceOptions
{
    public string Name { get; set; } = "Device";
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string Identifiers { get; set; } = "device-1";
    public string UniquePrefix { get; set; } = "dev1_";
}

public sealed class ApiOptions
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; }
    public string? Key { get; set; }
    public bool VerifySsl { get; set; } = true;
    public int TimeoutSec { get; set; } = 10;
    public int PollIntervalSec { get; set; } = 900;
    public FieldsOptions? Fields { get; set; }
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
