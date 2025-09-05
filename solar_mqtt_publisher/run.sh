#!/usr/bin/with-contenv bashio

# echo "Hello world2!"

set -euo pipefail


# MQTT_HOST=$(bashio::config 'mqtt.host')
# MQTT_PASSWORD=$(bashio::config 'mqtt.password')
# API_URL=$(bashio::config 'api.url')
# LOG_LEVEL=$(bashio::config 'log.level')

# bashio::log.info "Starting Solar MQTT Publisher"
# bashio::log.info "MQTT ${MQTT_HOST}"
# bashio::log.info "API ${API_URL}"
# bashio::log.info "Log Level ${LOG_LEVEL}"

export MQTT_HOST API_URL LOG_LEVEL MQTT_PASSWORD

exec /app/SolarMqtt
