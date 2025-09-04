using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

public static class MqttPublisher
{
    public static async Task<IMqttClient> ConnectAsync(MqttOptions cfg, CancellationToken ct)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(cfg.Host, cfg.Port);
        if (!string.IsNullOrWhiteSpace(cfg.Username))
            builder = builder.WithCredentials(cfg.Username, cfg.Password);
        // Last Will placeholder (actual topic patched after discovery publish by reconnect not needed; simple static base if env provided)
        // For simplicity we don't know base_topic yet here; will use env override or fallback generic.
        var willTopic = Environment.GetEnvironmentVariable("LWT_TOPIC") ?? "solar/status";
        builder = builder
            .WithWillTopic(willTopic)
            .WithWillPayload("offline")
            .WithWillRetain(true)
            .WithWillQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);

        await client.ConnectAsync(builder.Build(), ct);
        return client;
    }

    public static async Task PublishDiscoveryAsync(IMqttClient client, Options opts, CancellationToken ct)
    {
        var baseTopic = opts.Mqtt.Base_Topic.TrimEnd('/');
        var pref = opts.Device.Unique_Prefix;
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
        foreach (var s in sensors)
        {
            var stateTopic = $"{baseTopic}/state/{s.Slug}";
            var cfgTopic = $"homeassistant/sensor/{s.Slug}/config";
            var cfg = new
            {
                name = s.Name,
                unique_id = s.Slug,
                state_topic = stateTopic,
                unit_of_measurement = "kWh",
                device_class = "energy",
                state_class = "total_increasing",
                icon = s.Icon,
                device
            };
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(cfgTopic)
                .WithPayload(JsonSerializer.Serialize(cfg))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();
            await client.PublishAsync(msg, ct);
        }

        await client.PublishStringAsync($"{baseTopic}/status", "online", retain: true, cancellationToken: ct);
    }

    public static async Task PublishStringAsync(IMqttClient client, Options opts, string payload, bool retain, CancellationToken ct)
    {
        var baseTopic = opts.Mqtt.Base_Topic.TrimEnd('/');
        await client.PublishStringAsync($"{baseTopic}/status", payload, retain: true, cancellationToken: ct);
    }

    public static Task PublishOfflineAsync(IMqttClient client, Options opts, CancellationToken ct)
        => PublishStringAsync(client, opts, "offline", true, ct);
}
