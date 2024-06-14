using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlkoToMqtt {
    internal static class Config {

        public static string AlkoClientID => GetConfigFromEnvironment("ALKO_CLIENT_ID");
        public static string AlkoClientSecret => GetConfigFromEnvironment("ALKO_CLIENT_SECRET");
        public static string AlkoLoginUser => GetConfigFromEnvironment("ALKO_LOGIN_USER");
        public static string AlkoLoginPassword => GetConfigFromEnvironment("ALKO_LOGIN_PASSWORD");


        public static string MqttHost => GetConfigFromEnvironment("MQTT_HOST");
        public static int MqttPort => int.Parse(GetConfigFromEnvironment("MQTT_PORT", "1883"));
        public static string MqttTopic => GetConfigFromEnvironment("MQTT_TOPIC", "alko");


        public static LogLevel ServiceLogLevel => Enum.Parse<LogLevel>(GetConfigFromEnvironment("SERVICE_LOG_LEVEL", "Info"));
        public static TimeSpan ServiceUpdateInterval => TimeSpan.FromSeconds(int.Parse(GetConfigFromEnvironment("SERVICE_UPDATE_INTERVAL", "5")));


        private static string GetConfigFromEnvironment(string envVarName) {
            var value = Environment.GetEnvironmentVariable(envVarName);
            if (value == null) {
                throw new Exception(string.Format("config error. missing madatory environment variable: " + envVarName));
            }

            return value;
        }

        private static string GetConfigFromEnvironment(string envVarName, string fallback) {
            var value = Environment.GetEnvironmentVariable(envVarName);
            if (value == null) {
                return fallback;
            }

            return value;
        }
    }
}
