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

## Configuration Options (`options.json`)

```json
{
  "mqtt": {
    "host": "127.0.0.1",
    "port": 1883,
    "username": "",
    "password": "",
    "baseTopic": "solar"
  },
  "device": {
    "name": "Rooftop PV",
    "manufacturer": "Custom",
    "model": "API",
    "identifiers": "pv-system-1",
    "uniquePrefix": "pv1_"
  },
  "api": {
    "url": "http://host.docker.internal:8080/metrics",
    "method": "GET",
    "headers": {},
    "key": "",
    "verifySsl": true,
    "timeoutSec": 10,
    "pollIntervalSec": 60,
    "apiFields": {
      "solarTotalKwh": "solar_energy_total_kwh",
      "gridImportKwh": "grid_import_total_kwh",
      "gridExportKwh": "grid_export_total_kwh"
    }
  },
  "log": {
    "level": "INFO"
  }
}
```

### Auto-detect vs Fallback

The app first tries to parse the response as a `Dictionary<string, List<BarChartData>>` (year keyed). If successful it computes:

- Solar total from field `P` (kWh)
- Grid import from `U` (kWh)
- Grid export from `I` (Wh, converted to kWh)

If that fails it resolves dotted paths defined under `api.fields`.

## MQTT Topics

Assuming `baseTopic=solar` and `uniquePrefix=pv1_`:

- Discovery configs: `homeassistant/sensor/pv1_solar_total/config` (and equivalents)
- States: `solar/state/pv1_solar_total`, `solar/state/pv1_grid_import`, `solar/state/pv1_grid_export`
- Status / LWT: `solar/status` (payload: `online` / `offline`)

## Configuration Sources & Overrides

Layered precedence (later overrides earlier):

1. `data/options.json` (required base file)
2. User Secrets (development only, if defined) – keys like `MQTT_HOST`, `API_URL` etc.
3. Environment variables with prefix & hierarchy: `SOLAR_MQTT__HOST`, `SOLAR_API__URL`, `SOLAR_LOG__LEVEL`, `SOLAR_VALUE_EPS`
4. (Special) `LWT_TOPIC` for Last Will override.

Recommended form: use the hierarchical `SOLAR_` prefixed vars in production.

Example overrides:

```bash
SOLAR_MQTT__HOST=broker.internal \
SOLAR_MQTT__USERNAME=solar \
SOLAR_MQTT__PASSWORD=secret \
SOLAR_API__URL=https://example.com/data.json \
SOLAR_LOG__LEVEL=DEBUG \
SOLAR_VALUE_EPS=0.01
```

Last Will override (optional):

```bash
LWT_TOPIC=custom/solar/status
```

### `apiFields` & `apiHeaders`

The configuration uses PascalCase arrays for field mapping and headers:

```jsonc
"apiFields": [
  { "name": "solarTotalKwh", "metric": "solar_energy_total_kwh" },
  { "name": "gridImportKwh", "metric": "grid_import_total_kwh" },
  { "name": "gridExportKwh", "metric": "grid_export_total_kwh" }
],
"apiHeaders": [
  { "key": "Accept", "value": "application/json" }
]
```

These arrays provide manual mapping when auto-detection fails and allow custom HTTP headers for API requests.

## Change Detection

Values are only published when: `abs(new - prev) >= valueEps` where `valueEps` is the floating value in `options.json` (default 0). This reduces retained message churn.

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
sed -i 's/"level": "INFO"/"level": "DEBUG"/' data/options.json # enable debug
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
| No updates | `valueEps` too large | Lower or unset `valueEps` |
| Offline status stuck | Broker retained old LWT | Restart add-on to republish discovery & status |

Enable debug logs: set `level: DEBUG` in options or export `SOLAR_LOG__LEVEL=DEBUG`.

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
