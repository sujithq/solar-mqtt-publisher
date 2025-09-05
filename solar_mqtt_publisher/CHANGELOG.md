# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project adheres to Semantic Versioning once a stable 1.x is reached.

## [Unreleased]

### Added (roadmap)

- Planned: TLS / client cert support for MQTT
- Planned: Additional optional sensors & metrics endpoint
- Planned: Ingress panel / simple UI

## [0.1.0] - 2025-09-05

### Added (initial release)

- Initial Home Assistant add-on structure (`config.yaml`, `repository.json`)
- MQTT publishing with retained energy totals and LWT status topic
- Home Assistant MQTT Discovery messages for 3 energy sensors
- REST API polling with fallback dotted path field mapping
- Auto-detection & aggregation of custom annual/day JSON schema (P/U/I totals)
- Change detection with epsilon (`value_eps`) to suppress redundant publishes
- Layered configuration loader (defaults -> /data/options.json -> user secrets -> SOLAR_ env -> legacy flat env)
- Support for both legacy `api.fields` and new `api_fields` / `api_headers` arrays
- Environment variable overrides (hierarchical `SOLAR_MQTT__HOST`, etc.)
- Sample options file and security guidance
- Basic structured logging with configurable log level

### Changed

- Internal option property names normalized to PascalCase (backward compatible with snake_case JSON & env names)

### Security

- Added `.gitignore` entries to prevent committing secrets
- Added `.gitattributes` to enforce LF normalization

### Documentation

- Comprehensive README with configuration precedence and override examples

[Unreleased]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.0...HEAD
[0.1.0]: https://github.com/sujithq/solar-mqtt-publisher/releases/tag/0.1.0
