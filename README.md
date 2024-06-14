# AlkoToMqtt

A service to bridge Al-Ko API to MQTT.

* Go to https://alko-garden.at/iot-api-zugang-anfordern/ and request your ClientID and ClientSecret.
* Set environment variables and start the service

```
ALKO_CLIENT_ID = [client id as provided]
ALKO_CLIENT_SECRET = [client secret as provided]
ALKO_LOGIN_USER = [user name as used in InTouch app]
ALKO_LOGIN_PASSWORD = [password as used in InTouch app]
MQTT_HOST = [mqtt hostname or IP]
MQTT_PORT = [mqtt port (optional, default=1883)]
MQTT_TOPIC = [root topic (optional, default=alko)]
SERVICE_LOG_LEVEL = [one of Error, Info, Debug, Fine (optional, default=Info)]
SERVICE_UPDATE_INTERVAL = [polling interval in minutes (optional, default=5)]
```
