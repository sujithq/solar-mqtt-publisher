using MQTTnet;
using MQTTnet.Client;

public static class MqttPublisher
{
    public static async Task<IMqttClient> ConnectAsync(MqttOptions cfg, CancellationToken ct)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();

        static MqttClientOptions Build(MqttOptions cfg)
        {
            var b = new MqttClientOptionsBuilder().WithTcpServer(cfg.Host, cfg.Port);
            if (!string.IsNullOrWhiteSpace(cfg.Username))
                b = b.WithCredentials(cfg.Username, cfg.Password);
            var willTopic = Environment.GetEnvironmentVariable("LWT_TOPIC") ?? "solar/status";
            b = b.WithWillTopic(willTopic)
                .WithWillPayload("offline")
                .WithWillRetain(true)
                .WithWillQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
            return b.Build();
        }

        // Sanitized config log
        LogHelper.Log(LogLevelSimple.Info, $"[MQTT] Attempting connection host={cfg.Host} port={cfg.Port} user={(string.IsNullOrWhiteSpace(cfg.Username) ? "<none>" : cfg.Username)}");
        try
        {
            await client.ConnectAsync(Build(cfg), ct);
            LogHelper.Log(LogLevelSimple.Info, "[MQTT] Connected on primary host.");
            return client;
        }
        catch (MQTTnet.Adapter.MqttConnectingFailedException ex) when (!string.Equals(cfg.Host, "core-mosquitto", StringComparison.OrdinalIgnoreCase))
        {
            LogHelper.Log(LogLevelSimple.Warn, $"[MQTT] Primary connection failed ({ex.ResultCode}) - trying fallback host core-mosquitto...");
            // fallback attempt
            var originalHost = cfg.Host;
            cfg.Host = "core-mosquitto"; // mutate for fallback; acceptable since options instance not reused elsewhere for host-specific logic
            try
            {
                await client.ConnectAsync(Build(cfg), ct);
                LogHelper.Log(LogLevelSimple.Info, "[MQTT] Connected using fallback host core-mosquitto.");
                return client;
            }
            catch (Exception ex2)
            {
                // restore host before throwing
                cfg.Host = originalHost;
                LogHelper.Log(LogLevelSimple.Error, $"[MQTT] Fallback connection failed: {ex2.Message}");
                throw; // propagate last exception
            }
        }
    }

    public static async Task PublishDiscoveryAsync(IMqttClient client, Options opts, CancellationToken ct)
    {
        var baseTopic = opts.Mqtt.BaseTopic.TrimEnd('/');
        var pref = opts.Device.UniquePrefix;
        var device = new
        {
            identifiers = new[] { opts.Device.Identifiers },
            name = opts.Device.Name,
            manufacturer = opts.Device.Manufacturer ?? string.Empty,
            model = opts.Device.Model ?? string.Empty
        };
        var sensors = new (string Key, string Name, string Slug, string Icon)[]
        {
            ("solar_total_kwh","Solar Energy Total", $"{pref}solar_total","mdi:solar-power"),
            ("grid_import_kwh","Grid Import Total", $"{pref}grid_import","mdi:transmission-tower-import"),
            ("grid_export_kwh","Grid Export Total", $"{pref}grid_export","mdi:transmission-tower-export"),
        };

        // Pre-build device JSON once to avoid reflection-based serialization (AOT friendly)
        static string J(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var deviceJson = $"{{\"identifiers\":[\"{J(opts.Device.Identifiers)}\"],\"name\":\"{J(opts.Device.Name)}\",\"manufacturer\":\"{J(opts.Device.Manufacturer ?? string.Empty)}\",\"model\":\"{J(opts.Device.Model ?? string.Empty)}\"}}";
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

    public static async Task PublishStringAsync(IMqttClient client, Options opts, string payload, bool retain, CancellationToken ct)
    {
        var baseTopic = opts.Mqtt.BaseTopic.TrimEnd('/');
        await client.PublishStringAsync($"{baseTopic}/status", payload, retain: true, cancellationToken: ct);
    }

    public static Task PublishOfflineAsync(IMqttClient client, Options opts, CancellationToken ct)
        => PublishStringAsync(client, opts, "offline", true, ct);
}
