using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MQTTnet.Client;
using System.Globalization;
using System.Collections.Generic;

public static class Program
{
    public static async Task Main(string[] args)
    {
    var opts = Options.Load();
    LogHelper.Configure(opts.Log_Level);
    var baseTopic = opts.Mqtt.Base_Topic.TrimEnd('/');
        LogHelper.Log(LogLevelSimple.Info, $"Startup - MQTT broker={opts.Mqtt.Host}:{opts.Mqtt.Port}, base_topic={baseTopic}, poll_interval={opts.Api.Poll_Interval_Sec}s");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; LogHelper.Log(LogLevelSimple.Info, "Cancellation requested (Ctrl+C)"); cts.Cancel(); };

        LogHelper.Log(LogLevelSimple.Info, "Connecting to MQTT broker...");
        var client = await MqttPublisher.ConnectAsync(opts.Mqtt, cts.Token);
        LogHelper.Log(LogLevelSimple.Info, "Connected to MQTT broker.");
        await MqttPublisher.PublishDiscoveryAsync(client, opts, cts.Token);
        LogHelper.Log(LogLevelSimple.Info, "Home Assistant discovery messages published.");

        async Task Pub(string slug, double val)
        {
            ValueChangeTracker.EnsureInitialized(opts);
            if (ValueChangeTracker.TrySkip(slug, val)) return;
            var msg = new MQTTnet.MqttApplicationMessageBuilder()
                .WithTopic($"{baseTopic}/state/{opts.Device.Unique_Prefix}{slug}")
                .WithPayload(val.ToString(CultureInfo.InvariantCulture))
                .WithRetainFlag(true)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await client.PublishAsync(msg, cts.Token);
            ValueChangeTracker.Record(slug, val);
        }

        var iteration = 0;
        while (!cts.IsCancellationRequested)
        {
            try
            {
                iteration++;
                var sw = Stopwatch.StartNew();
                LogHelper.Log(LogLevelSimple.Debug, $"Iteration {iteration} - fetching API data...");
                var json = await ApiClient.FetchAsync(opts.Api, cts.Token);
                sw.Stop();
                LogHelper.Log(LogLevelSimple.Debug, $"Iteration {iteration} - fetch completed in {sw.ElapsedMilliseconds} ms");

                if (ApiClient.TryComputeMyEnergyTotals(json, out var sKwh, out var giKwh, out var geKwh))
                {
                    LogHelper.Log(LogLevelSimple.Info, $"Iteration {iteration} - auto-detect totals solar={sKwh:F3} grid_import={giKwh:F3} grid_export={geKwh:F3}");
                    await Pub("solar_total", sKwh);
                    await Pub("grid_import", giKwh);
                    await Pub("grid_export", geKwh);
                }
                else
                {
                    var totals = new
                    {
                        solar = ApiClient.GetDouble(json, opts.Api.Fields.Solar_Total_Kwh),
                        import_ = ApiClient.GetDouble(json, opts.Api.Fields.Grid_Import_Kwh),
                        export_ = ApiClient.GetDouble(json, opts.Api.Fields.Grid_Export_Kwh)
                    };
                    LogHelper.Log(LogLevelSimple.Info, $"Iteration {iteration} - fallback totals solar={totals.solar:F3} grid_import={totals.import_:F3} grid_export={totals.export_:F3}");
                    await Pub("solar_total", totals.solar);
                    await Pub("grid_import", totals.import_);
                    await Pub("grid_export", totals.export_);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(LogLevelSimple.Error, $"Iteration {iteration} - error during processing", ex);
                await MqttPublisher.PublishStringAsync(client, opts, $"error: {ex.Message}", true, cts.Token);
            }

            LogHelper.Log(LogLevelSimple.Debug, $"Iteration {iteration} - sleeping {opts.Api.Poll_Interval_Sec}s");
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(opts.Api.Poll_Interval_Sec), cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

    LogHelper.Log(LogLevelSimple.Info, "Exited main loop. Shutting down.");
    }
}

// ---- Value change tracking ----
static class ValueChangeTracker
{
    private static readonly Dictionary<string, double> _last = new();
    private static bool _initialized = false;
    private static double _epsilon = 0d; // default exact comparison
    public static void EnsureInitialized(Options root)
    {
        if (_initialized) return;
        _initialized = true;
        if (root.Value_Eps is double eps && eps >= 0) _epsilon = eps;
        LogHelper.Log(LogLevelSimple.Info, $"Value change detection enabled (epsilon={_epsilon})");
    }
    public static bool TrySkip(string slug, double value)
    {
        if (_last.TryGetValue(slug, out var prev))
        {
            if (Math.Abs(prev - value) < _epsilon)
            {
                LogHelper.Log(LogLevelSimple.Debug, $"No change for '{slug}' (prev={prev:F6}, new={value:F6}, eps={_epsilon}) - skipping publish");
                return true;
            }
        }
        return false;
    }
    public static void Record(string slug, double value)
    {
        _last[slug] = value;
        LogHelper.Log(LogLevelSimple.Debug, $"Published '{slug}'={value:F6}");
    }
}

// ---- Minimal logging helper (no external deps) ----
enum LogLevelSimple { Trace = 0, Debug = 1, Info = 2, Warn = 3, Error = 4 }
static class LogHelper
{
    public static LogLevelSimple MinLevel = LogLevelSimple.Info;
    public static void Configure(string? levelRaw)
    {
        MinLevel = ParseLevel(levelRaw);
    }
    private static LogLevelSimple ParseLevel(string? raw) => raw?.ToUpperInvariant() switch
    {
        "TRACE" => LogLevelSimple.Trace,
        "DEBUG" => LogLevelSimple.Debug,
        "WARN" => LogLevelSimple.Warn,
        "WARNING" => LogLevelSimple.Warn,
        "ERROR" => LogLevelSimple.Error,
        _ => LogLevelSimple.Info
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(LogLevelSimple level, string message, Exception? ex = null)
    {
        if (level < MinLevel) return;
        var ts = DateTime.UtcNow.ToString("o");
        var lvl = level.ToString().ToUpperInvariant().PadRight(5);
        Console.WriteLine($"{ts} [{lvl}] {message}{(ex != null ? " :: " + ex.GetType().Name + ": " + ex.Message : string.Empty)}");
        if (ex != null && level >= LogLevelSimple.Debug)
        {
            Console.WriteLine(ex.StackTrace);
        }
    }
}
