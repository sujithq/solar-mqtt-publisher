public sealed class Options
{
    public MqttOptions Mqtt { get; set; } = new();
    public DeviceOptions Device { get; set; } = new();
    public ApiOptions Api { get; set; } = new();
    public string? Log_Level { get; set; }
    public double? Value_Eps { get; set; }
    public ApiField[]? Api_Fields { get; set; }
    public ApiHeader[]? Api_Headers { get; set; }
}

public sealed class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string Base_Topic { get; set; } = "solar";
}
public sealed class DeviceOptions { public string Name { get; set; } = "Device"; public string? Manufacturer { get; set; } public string? Model { get; set; } public string Identifiers { get; set; } = "device-1"; public string Unique_Prefix { get; set; } = "dev1_"; }
public sealed class ApiOptions
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; }
    public string? Key { get; set; }
    public bool Verify_Ssl { get; set; } = true;
    public int Timeout_Sec { get; set; } = 10;
    public int Poll_Interval_Sec { get; set; } = 900;
    public FieldsOptions? Fields { get; set; } // optional now (can be synthesized)
}
public sealed class FieldsOptions { public required string Solar_Total_Kwh { get; init; } public required string Grid_Import_Kwh { get; init; } public required string Grid_Export_Kwh { get; init; } }
public sealed class ApiField { public string Name { get; init; } = string.Empty; public string Metric { get; init; } = string.Empty; }
public sealed class ApiHeader { public string Key { get; init; } = string.Empty; public string? Value { get; init; } }
