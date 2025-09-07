# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project adheres to Semantic Versioning once a stable 1.x is reached.

## [Unreleased]

## [0.1.20] - 2025-09-07

### Removed

- Environment variable configuration layer (`SOLAR_*` and `LWT_TOPIC`). All configuration now sourced exclusively from JSON files (`default/options.json` layered with `/data/options.json` and optional user secrets during development).

### Added

- `mqtt.lastWillTopic` JSON property replacing prior `LWT_TOPIC` environment variable.

### Changed

- Documentation (`README.md`, `DOCS.md`) updated to reflect JSON-only configuration model.


### Added (roadmap)

- Planned: TLS / client cert support for MQTT
- Planned: Additional optional sensors & metrics endpoint
- Planned: Ingress panel / simple UI

## [0.1.18] - 2025-09-05

### Changed (docs & assets)

- Documentation refinements (`DOCS.md` formatting, added environment override clarity)
- Synchronized internal documentation with add-on schema

### Added (assets)

- Project icons (`icon.png`, `logo.png`) placeholder assets

## [0.1.17] - 2025-09-05

### Changed (readme wording)

- Minor wording improvements in README

## [0.1.16] - 2025-09-05

### Added (docs)

- Initial `DOCS.md` comprehensive add-on documentation

## [0.1.15] - 2025-09-05

### Changed (options refactor)

- Normalized option property names internally to PascalCase (backward compatible mapping)

## [0.1.14] - 2025-09-05

### Added (compat layer)

- Backward compatibility layer for legacy snake_case env / JSON keys

## [0.1.13] - 2025-09-05

### Removed (backwards compatibility)

- Removed legacy snake_case JSON mappings
- Removed legacy flat environment variables (MQTT_HOST, API_URL, etc.)
- Removed legacy array forms (api_fields, api_headers)
- Enforced PascalCase schema across all configuration

## [0.1.12] - 2025-09-05

### Changed (epsilon logging)

- Improved change detection epsilon handling and logging precision

## [0.1.11] - 2025-09-05

### Added (layered config)

- Layered configuration loader (defaults -> options.json -> user secrets -> SOLAR_ env vars)

## [0.1.10] - 2025-09-05

### Added (auto-detect schema)

- Auto-detection & aggregation of P/U/I JSON schema

## [0.1.9] - 2025-09-05

### Added (fallback mapping)

- Fallback dotted path field mapping for REST payload

## [0.1.8] - 2025-09-05

### Added (discovery messages)

- Home Assistant MQTT Discovery messages for 3 energy sensors

## [0.1.7] - 2025-09-05

### Added (MQTT publishing)

- MQTT publishing with retained energy totals and LWT status topic

## [0.1.6] - 2025-09-05

### Added (API polling loop)

- Initial REST API polling loop

## [0.1.5] - 2025-09-05

### Added (logging)

- Basic structured logging with configurable log level

## [0.1.4] - 2025-09-05

### Security (attributes)

- Added `.gitattributes` to enforce LF normalization

## [0.1.3] - 2025-09-05

### Security (gitignore)

- Added `.gitignore` entries to prevent committing secrets

## [0.1.2] - 2025-09-05

### Added (sample options)

- Sample options file and security guidance

## [0.1.1] - 2025-09-05

### Added (addon scaffold)

- Initial Home Assistant add-on structure (`config.yaml`, `repository.json`)

## [0.1.0] - 2025-09-05

### Added (initial implementation summary)

- Core scaffold & minimal functionality bootstrap

### Changed (naming normalization)

- Internal option property names normalized to PascalCase (backward compatible with snake_case JSON & env names)

### Security (secrets & normalization)

- Added `.gitignore` entries to prevent committing secrets
- Added `.gitattributes` to enforce LF normalization

### Documentation (readme)

- Comprehensive README with configuration precedence and override examples

[Unreleased]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.20...HEAD
[0.1.20]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.18...0.1.20
[0.1.18]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.17...0.1.18
[0.1.17]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.16...0.1.17
[0.1.16]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.15...0.1.16
[0.1.15]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.14...0.1.15
[0.1.14]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.13...0.1.14
[0.1.13]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.12...0.1.13
[0.1.12]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.11...0.1.12
[0.1.11]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.10...0.1.11
[0.1.10]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.9...0.1.10
[0.1.9]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.8...0.1.9
[0.1.8]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.7...0.1.8
[0.1.7]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.6...0.1.7
[0.1.6]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.5...0.1.6
[0.1.5]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.4...0.1.5
[0.1.4]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.3...0.1.4
[0.1.3]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.2...0.1.3
[0.1.2]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.1...0.1.2
[0.1.1]: https://github.com/sujithq/solar-mqtt-publisher/compare/0.1.0...0.1.1
[0.1.0]: https://github.com/sujithq/solar-mqtt-publisher/releases/tag/0.1.0
