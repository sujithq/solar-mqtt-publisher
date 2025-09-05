# Solar MQTT Publisher Add-on Documentation

## 1. Purpose

Publishes solar energy totals (solar generation, grid import, grid export) to Home Assistant via MQTT Discovery so they appear automatically in the Energy Dashboard.

## 2. High-level Flow

1. Start add-on → load layered configuration.
2. Connect to MQTT (fallback to `core-mosquitto` if primary fails).
3. Publish Home Assistant Discovery configs + retained `status` (online / offline) topic.
4. Poll external HTTP API at `pollIntervalSec`.
5. Auto-detect totals (P/U/I schema) or fall back to explicit mapping.
6. Publish only when delta ≥ `valueEps`.

## 3. Configuration Layers (lowest precedence first)

1. Embedded defaults (`default/options.json`).
2. Supervisor `/data/options.json` (from add-on UI save).
3. User Secrets (development only, if present).
4. Prefixed hierarchical env vars: `SOLAR_MQTT__HOST`, `SOLAR_API__URL`, `SOLAR_LOG__LEVEL`, `SOLAR_VALUE_EPS`.
5. `LWT_TOPIC` (special override for status topic name only).

The add-on UI exposes a minimal subset; advanced fields require editing `/data/options.json` or using env overrides.

## 4. options.json Fields (sample summary)

```text
logLevel: INFO|DEBUG|TRACE|WARN|ERROR
valueEps: double (minimum change required to republish)
mqtt.*: host / port / username / password / baseTopic
device.*: name / manufacturer / model / identifiers / uniquePrefix
api.*: url / method / headers / verifySsl / timeoutSec / pollIntervalSec
api.fields.* OR apiFields[] (manual mapping)
```

## 5. Array Form Configuration

Use PascalCase arrays for field mapping and headers:

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

These arrays provide manual mapping when auto-detection fails.

## 6. Environment Override Examples

```bash
SOLAR_MQTT__HOST=broker.internal
SOLAR_MQTT__USERNAME=solar
SOLAR_MQTT__PASSWORD=secret
SOLAR_API__URL=https://example.com/data.json
SOLAR_LOG__LEVEL=DEBUG
SOLAR_VALUE_EPS=0.01
```

Legacy equivalents still accepted: `MQTT_HOST`, `API_URL`, etc.

## 7. MQTT Topics

Assuming `baseTopic=solar` and `uniquePrefix=pv1_`:

```text
homeassistant/sensor/pv1_solar_total/config
homeassistant/sensor/pv1_grid_import/config
homeassistant/sensor/pv1_grid_export/config
solar/state/pv1_solar_total
solar/state/pv1_grid_import
solar/state/pv1_grid_export
solar/status (online|offline)
```

## 8. Change Detection

Publishing skipped when `abs(new - previous) < valueEps`. Set `valueEps=0.0` to publish every poll.

## 9. Auto-detection vs Manual Fields

If the API root is a dictionary of years pointing to arrays of objects containing `P` (solar kWh), `U` (import kWh), `I` (export Wh) the totals are auto-summed (export converted to kWh). Otherwise provide manual mapping using `apiFields`.

## 10. Timeouts & SSL

`timeoutSec` controls HTTP timeout. `verifySsl=false` disables certificate validation (dev only).

## 11. Status / Last Will

LWT payload `offline` is set (topic: `solar/status` or custom `LWT_TOPIC`). On start `online` is published retained.

## 12. Security Notes

- Never commit `/data/options.json`.
- Use least-privileged MQTT credentials.
- Rotate credentials if exposed.

## 13. Troubleshooting

| Symptom | Likely Cause | Action |
|---------|--------------|--------|
| Sensors not discovered | MQTT discovery disabled | Enable discovery in HA MQTT integration |
| Values never change | `valueEps` too high | Lower `valueEps` |
| All zeros | Bad field paths | Check logs / adjust mapping |
| Offline status | Retained old LWT | Restart add-on |
| HTTP errors | Bad URL / timeout | Verify URL, raise `timeoutSec` |

## 14. Updating Configuration Safely

1. Edit via UI or modify `/data/options.json`.
2. Restart add-on.
3. Review startup log line for effective settings.

## 15. Release & Versioning

See `CHANGELOG.md`. Pre-1.0.0 may include breaking changes.

## 16. Roadmap (Short List)

- TLS / client certificate support
- Additional sensors (instant power, voltage, etc.)
- Metrics endpoint / Prometheus scrape
- Ingress UI

## 17. Support / Issues

Include when filing issues:

- Add-on version
- Log excerpt (relevant lines)
- Sanitized `/data/options.json`
- Expected vs actual behavior

## 18. Minimal Example `/data/options.json`

```json
{
  "logLevel": "INFO",
  "valueEps": 0.0,
  "mqtt": { "host": "core-mosquitto", "port": 1883, "username": "", "password": "", "baseTopic": "solar" },
  "device": { "name": "Rooftop PV", "identifiers": "pv-system-1", "uniquePrefix": "pv1_" },
  "api": { "url": "http://host.docker.internal:8080/metrics", "pollIntervalSec": 60, "timeoutSec": 10 }
}
```

## 19. Development Notes

Run locally (without Supervisor):

```bash
dotnet build
cp sample-options.json bin/Debug/net9.0/data/options.json
./bin/Debug/net9.0/SolarMqtt
```

Expect MQTT connection failure if no broker is running (normal during local testing).

---
End of documentation.
