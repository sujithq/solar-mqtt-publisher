using System.Text.Json;
using System.Text.Json.Nodes;

public sealed class Options
{
    public required MqttOptions Mqtt { get; init; }
    public required DeviceOptions Device { get; init; }
    public required ApiOptions Api { get; init; }
    public string? Log_Level { get; init; }
    public double? Value_Eps { get; init; }

    public static Options Load(string path = "data/options.json")
    {
        var json = JsonNode.Parse(File.ReadAllText(path))!;
        return json.Deserialize<Options>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}

public sealed class MqttOptions { public required string Host { get; init; } public int Port { get; init; } = 1883; public string? Username { get; init; } public string? Password { get; init; } public required string Base_Topic { get; init; } }
public sealed class DeviceOptions { public required string Name { get; init; } public string? Manufacturer { get; init; } public string? Model { get; init; } public required string Identifiers { get; init; } public required string Unique_Prefix { get; init; } }
public sealed class ApiOptions
{
    public required string Url { get; init; }
    public string Method { get; init; } = "GET";
    public Dictionary<string, string>? Headers { get; init; }
    public string? Key { get; init; }
    public bool Verify_Ssl { get; init; } = true;
    public int Timeout_Sec { get; init; } = 10;
    public int Poll_Interval_Sec { get; init; } = 900;
    public required FieldsOptions Fields { get; init; }
}
public sealed class FieldsOptions { public required string Solar_Total_Kwh { get; init; } public required string Grid_Import_Kwh { get; init; } public required string Grid_Export_Kwh { get; init; } }
