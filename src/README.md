# Solar MQTT Publisher Home Assistant Add-on

Publishes solar / grid import / grid export energy totals via MQTT Discovery so they appear in Home Assistant and can be added to the Energy Dashboard.

## Features

- Auto–discovery of 3 energy sensors (solar total, grid import, grid export)
- Optional auto-detection logic for specific JSON schema (aggregates per-day arrays)
- Fallback dotted JSON path extraction (configurable)
- Change detection (only publish when value changes) with optional epsilon threshold
- Retained state topics, retained discovery configs
- Status topic with MQTT Last Will (offline) + online heartbeat
- Multi-arch build (amd64, aarch64, armv7) using .NET 9 single-file trimmed binary

## Home Assistant Add-on Installation

1. Add this repository URL to your Home Assistant Add-on Store (Repositories).
2. Install the "Solar MQTT Publisher" add-on.
3. Configure options (see below) and start the add-on.
4. Ensure you have the MQTT integration configured in Home Assistant.
5. After startup, three sensors should appear automatically (may take up to 30 seconds) and be selectable in the Energy Dashboard.

## Configuration Options (`config.yaml`)

```yaml
mqtt:
  host: 127.0.0.1
  port: 1883
  username: ""
  password: ""
  base_topic: "solar"          # Base under which state & status are published
device:
  name: "Rooftop PV"
  manufacturer: "Custom"
  model: "API"
  identifiers: "pv-system-1"    # Stable unique ID for the physical system
  unique_prefix: "pv1_"         # Prefix used in entity unique_id's
api:
  url: "http://host.docker.internal:8080/metrics"
  method: GET
  headers: {}
  key: ""                       # Optional Bearer token
  verify_ssl: true
  timeout_sec: 10
  poll_interval_sec: 60          # Seconds between polls
  fields:                        # Used if auto-detect fails
    solar_total_kwh: "solar_energy_total_kwh"
    grid_import_kwh: "grid_import_total_kwh"
    grid_export_kwh: "grid_export_total_kwh"
```

### Auto-detect vs Fallback

The app first tries to parse the response as a `Dictionary<string, List<BarChartData>>` (year keyed). If successful it computes:

- Solar total from field `P` (kWh)
- Grid import from `U` (kWh)
- Grid export from `I` (Wh, converted to kWh)

If that fails it resolves dotted paths defined under `api.fields`.

## MQTT Topics

Assuming `base_topic=solar` and `unique_prefix=pv1_`:

- Discovery configs: `homeassistant/sensor/pv1_solar_total/config` (and equivalents)
- States: `solar/state/pv1_solar_total`, `solar/state/pv1_grid_import`, `solar/state/pv1_grid_export`
- Status / LWT: `solar/status` (payload: `online` / `offline`)

## Environment Variable Overrides (optional)

Only an override for the Last Will topic is currently read from the environment. All other tuning (log level, epsilon) now lives in `options.json`.

| Variable | Purpose | Default |
|----------|---------|---------|
| `LWT_TOPIC` | Override Last Will topic (otherwise `<base_topic>/status`) | `<base_topic>/status` |

## Change Detection

Values are only published when: `abs(new - prev) >= value_eps` where `value_eps` is the floating value in `options.json` (default 0). This reduces retained message churn.

## Build Locally

Prereqs: Docker Buildx.

```bash
docker buildx build --platform linux/amd64,linux/arm64,linux/arm/v7 -t yourrepo/solar-mqtt-publisher:0.1.0 .
```

For single arch test:

```bash
docker build -t solar-mqtt-publisher:dev .
```

Create & bind an options file (edit for your broker & API):

```bash
mkdir -p data
cp sample-options.json data/options.json
sed -i 's/"host": "127.0.0.1"/"host": "mqtt.local"/' data/options.json
sed -i 's/"log_level": "INFO"/"log_level": "DEBUG"/' data/options.json # enable debug
docker run --rm -v $PWD/data:/data solar-mqtt-publisher:dev
```

## Development (without Home Assistant Supervisor)

You can run the compiled binary directly:

```bash
dotnet build
cp sample-options.json bin/Debug/net9.0/data/options.json
./bin/Debug/net9.0/SolarMqtt
```

## Graceful Shutdown

Ctrl+C triggers cancellation; the MQTT Last Will ensures `offline` if the container crashes. To force an immediate offline publish on a clean stop you can extend the shutdown logic (currently LWT is relied upon for unexpected exit).

## Roadmap / Ideas

- Optional TLS & client cert support
- Ingress panel (simple recent values UI)
- Metrics endpoint for add-on watchdog / Prometheus
- Configurable additional sensors

## Troubleshooting

| Symptom | Possible Cause | Action |
|---------|----------------|--------|
| Sensors not appearing | MQTT Discovery disabled | Ensure HA MQTT integration "Enable discovery" is on |
| All zeros | Wrong JSON schema | Check logged auto-detect fallbacks and confirm field paths |
| No updates | `value_eps` too large | Lower or unset `value_eps` |
| Offline status stuck | Broker retained old LWT | Restart add-on to republish discovery & status |

Enable debug logs: set `log_level: DEBUG` in the add-on options (UI or edit `options.json`).

## Security

- Never commit a real `data/options.json`; it may contain MQTT passwords or API keys.
- `sample-options.json` is the public template—edit a copy under `data/` at runtime.
- If you add an API `key` or MQTT credentials, they are only read from `/data/options.json` and not logged.
- Rotate broker credentials if accidentally exposed and purge the commit from history.

## License

See [LICENSE](./LICENSE).

## Disclaimer

Use at your own risk. Validate energy totals against your inverter / utility portal before relying on them for billing accuracy.

---

Contributions welcome: open issues or PRs at the repository URL.
