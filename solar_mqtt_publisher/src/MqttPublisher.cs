using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.Configuration;

public static class MqttPublisher
{
    public static async Task<IMqttClient> ConnectAsync(IConfiguration root, CancellationToken ct)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();
        MqttClientOptions Build()
        {
            var mqtt = root.GetSection("mqtt");
            var host = mqtt["host"] ?? "localhost";
            var port = int.TryParse(mqtt["port"], out var p) ? p : 1883;
            var user = mqtt["username"];
            var pass = mqtt["password"];
            var lwt = mqtt["lastWillTopic"] ?? "solar/status";
            var b = new MqttClientOptionsBuilder().WithTcpServer(host, port);
            if (!string.IsNullOrWhiteSpace(user)) b = b.WithCredentials(user, pass);
            b = b.WithWillTopic(lwt)
                .WithWillPayload("offline")
                .WithWillRetain(true)
                .WithWillQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
            return b.Build();
        }

        // Sanitized config log
        var hostForLog = root["mqtt:host"] ?? "localhost";
        var portForLog = root["mqtt:port"] ?? "1883";
        var userForLog = root["mqtt:username"];
        LogHelper.Log(LogLevelSimple.Info, $"[MQTT] Attempting connection host={hostForLog} port={portForLog} user={(string.IsNullOrWhiteSpace(userForLog) ? "<none>" : userForLog)}");
        try
        {
            await client.ConnectAsync(Build(), ct);
            LogHelper.Log(LogLevelSimple.Info, "[MQTT] Connected on primary host.");
            return client;
        }
        catch (MQTTnet.Adapter.MqttConnectingFailedException ex) when (!string.Equals(hostForLog, "core-mosquitto", StringComparison.OrdinalIgnoreCase))
        {
            LogHelper.Log(LogLevelSimple.Warn, $"[MQTT] Primary connection failed ({ex.ResultCode}) - trying fallback host core-mosquitto...");
            // fallback attempt (temporary override)
            try
            {
                // create a transient configuration clone with overridden host
                var mem = new Dictionary<string,string?>
                {
                    ["mqtt:host"] = "core-mosquitto",
                    ["mqtt:port"] = portForLog,
                    ["mqtt:username"] = userForLog,
                    ["mqtt:password"] = root["mqtt:password"],
                    ["mqtt:lastWillTopic"] = root["mqtt:lastWillTopic"] ?? "solar/status"
                };
                var fallbackRoot = new ConfigurationBuilder().AddInMemoryCollection(mem!).Build();
                root = fallbackRoot; // reassign for Build()
                await client.ConnectAsync(Build(), ct);
                LogHelper.Log(LogLevelSimple.Info, "[MQTT] Connected using fallback host core-mosquitto.");
                return client;
            }
            catch (Exception ex2)
            {
                LogHelper.Log(LogLevelSimple.Error, $"[MQTT] Fallback connection failed: {ex2.Message}");
                throw; // propagate last exception
            }
        }
    }
    public static async Task PublishDiscoveryAsync(IMqttClient client, IConfiguration root, CancellationToken ct)
    {
        var baseTopic = (root["mqtt:baseTopic"] ?? "solar").TrimEnd('/');
        var pref = root["device:uniquePrefix"] ?? "pv1_";
        var device = new
        {
            identifiers = new[] { root["device:identifiers"] ?? "pv-system-1" },
            name = root["device:name"] ?? "Rooftop PV",
            manufacturer = root["device:manufacturer"] ?? string.Empty,
            model = root["device:model"] ?? string.Empty
        };
        var sensors = new (string Key, string Name, string Slug, string Icon)[]
        {
            ("solar_total_kwh","Solar Energy Total", $"{pref}solar_total","mdi:solar-power"),
            ("grid_import_kwh","Grid Import Total", $"{pref}grid_import","mdi:transmission-tower-import"),
            ("grid_export_kwh","Grid Export Total", $"{pref}grid_export","mdi:transmission-tower-export"),
        };

        // Pre-build device JSON once to avoid reflection-based serialization (AOT friendly)
        static string J(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    var deviceJson = $"{{\"identifiers\":[\"{J(root["device:identifiers"] ?? "pv-system-1")}\"],\"name\":\"{J(root["device:name"] ?? "Rooftop PV")}\",\"manufacturer\":\"{J(root["device:manufacturer"] ?? string.Empty)}\",\"model\":\"{J(root["device:model"] ?? string.Empty)}\"}}";
        foreach (var s in sensors)
        {
            var stateTopic = $"{baseTopic}/state/{s.Slug}";
            var cfgTopic = $"homeassistant/sensor/{s.Slug}/config";
            var cfgJson = $"{{\"name\":\"{J(s.Name)}\",\"unique_id\":\"{J(s.Slug)}\",\"state_topic\":\"{J(stateTopic)}\",\"unit_of_measurement\":\"kWh\",\"device_class\":\"energy\",\"state_class\":\"total_increasing\",\"icon\":\"{J(s.Icon)}\",\"device\":{deviceJson}}}";
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(cfgTopic)
                .WithPayload(cfgJson)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();
            await client.PublishAsync(msg, ct);
        }

        await client.PublishStringAsync($"{baseTopic}/status", "online", retain: true, cancellationToken: ct);
    }
    public static async Task PublishStringAsync(IMqttClient client, IConfiguration root, string payload, bool retain, CancellationToken ct)
    {
        var baseTopic = (root["mqtt:baseTopic"] ?? "solar").TrimEnd('/');
        await client.PublishStringAsync($"{baseTopic}/status", payload, retain: true, cancellationToken: ct);
    }
    public static Task PublishOfflineAsync(IMqttClient client, IConfiguration root, CancellationToken ct)
        => PublishStringAsync(client, root, "offline", true, ct);
}
