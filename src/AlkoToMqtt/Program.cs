using AlkoToMqtt.Client;
using MQTTnet.Client;
using MQTTnet;
using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Server;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using MQTTnet.Exceptions;

namespace AlkoToMqtt {

    internal class Program {

        #region things cache

        private class ThingInfo {
            public string Name { get; init; }

            public string DesiredStateTopic => string.Format("{0}/things/{1}/state/desired", Config.MqttTopic, Name);
        }

        private class ThingContainer {

            public bool GetOrCreate(string thingName, out ThingInfo thing) {
                lock (m_lock) {
                    if (!m_things.TryGetValue(thingName, out thing)) {
                        thing = new ThingInfo() {
                            Name = thingName,
                        };

                        m_things.Add(thingName, thing);

                        return true;
                    }

                    return false;
                }
            }

            public ThingInfo FindByDesiredStateTopic(string topic) {
                lock (m_lock) {
                    return m_things.Values.Where(t => t.DesiredStateTopic == topic).SingleOrDefault();
                }
            }

            private IDictionary<string, ThingInfo> m_things = new Dictionary<string, ThingInfo>();
            private object m_lock = new object();
        }

        #endregion

        private class MqttHelper {

            public MqttHelper() {
                m_client = new MqttFactory().CreateMqttClient();
            }

            public IMqttClient Client => m_client;

            public async Task ConnectAsync() {

                var options = new MqttClientOptionsBuilder()
                  .WithTcpServer(Config.MqttHost, Config.MqttPort)
                  .Build();

                await m_client.ConnectAsync(options);
            }

            public async Task PublishAsync(string topic, string payload) {

                for (int retries = 0; ; retries++) {

                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(payload)
                        .WithRetainFlag(true)
                        .Build();

                    try {

                        await m_client.PublishAsync(applicationMessage, CancellationToken.None);
                        Logger.WriteFine("publish {0} -> {1}", topic, payload);

                        return;

                    } catch (MqttClientNotConnectedException) {
                        if (retries >= 2) {
                            throw;
                        }

                        Logger.WriteInfo("mqtt client disconnected. retry {0}.", retries + 1);
                    }

                    Thread.Sleep(500);

                    await ConnectAsync();
                }
            }

            IMqttClient m_client;
        }

        static async Task Main(string[] args) {

            Logger.Level = Config.ServiceLogLevel;

            try {

                m_alkoTokenClient = new Client.TokenClient(Config.AlkoClientID, Config.AlkoClientSecret, Config.AlkoLoginUser, Config.AlkoLoginPassword);
                await m_alkoTokenClient.AuthorizeAsync();

                Logger.WriteInfo("conencted to alko token service.");

            } catch (Exception ex) {
                Logger.WriteError("failed to authorize with token service: {0}", ex.Message);
                return;
            }

            try {

                m_mqttHelper = new MqttHelper();
                m_mqttHelper.Client.ApplicationMessageReceivedAsync += HandleMessage;

                await m_mqttHelper.ConnectAsync();

                Logger.WriteInfo("conencted to mqtt.");

            } catch (Exception ex) {
                Logger.WriteError("failed to connect to mqtt: {0}", ex.Message);
                return;
            }

            while (true) {

                try {

                    await UpdateAsync();

                } catch (Exception ex) {

                    Logger.WriteError("error in update: {0}", ex.Message);
                }

                Thread.Sleep(Config.ServiceUpdateInterval);
            }
        }

        static async Task UpdateAsync() {

            var token = await m_alkoTokenClient.GetTokenAsync();
            var alkoApiClient = new Client.ApiClient(token);

            var things = await alkoApiClient.GetThingsAsync();

            foreach (var thing in things) {

                var thingName = thing["thingName"].ToString();
                var thingTopic = Config.MqttTopic + "/things/" + thingName;

                if (m_things.GetOrCreate(thingName, out ThingInfo thingInfo)) {

                    var alias = thing["accessInformation"]["accessAlias"].ToString();

                    Logger.WriteInfo("discovered {0} '{1}'", thingName, alias);

                    var mqttSubscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                       .WithTopicFilter(thingInfo.DesiredStateTopic)
                       .Build();

                    var sr = await m_mqttHelper.Client.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

                    Logger.WriteInfo("subscribed to {0}", thingInfo.DesiredStateTopic);
                }

                await PublishRecursiveAsync(thingTopic + "/info", thing);

                var reportedState = await alkoApiClient.GetReportedState(thingName);
                await PublishRecursiveAsync(thingTopic + "/state/reported", reportedState);

                var desiredState = await alkoApiClient.GetDesiredState(thingName);                
                if (desiredState.HasValues) {
                    await PublishRecursiveAsync(thingTopic + "/state/desired", desiredState);
                } else {
                    // manuall reset the desired states
                    await m_mqttHelper.PublishAsync(thingTopic + "/state/desired/operationState", "");
                    await m_mqttHelper.PublishAsync(thingTopic + "/state/desired/rtc", "");
                }
            }

            Logger.WriteDebug("update complete");
        }

        static async Task HandleMessage(MqttApplicationMessageReceivedEventArgs arg) {

            var thing = m_things.FindByDesiredStateTopic(arg.ApplicationMessage.Topic);
            if (thing == null) {
                return;
            }

            Logger.WriteInfo("COMMAND {0} -> {1}", arg.ApplicationMessage.Topic, arg.ApplicationMessage.ConvertPayloadToString());
        }

        static async Task PublishRecursiveAsync(string topic, JToken tok) {

            if (tok is JObject obj) {
                foreach (var pair in obj) {
                    await PublishRecursiveAsync(topic + "/" + pair.Key, pair.Value);
                }
            } else {
                await m_mqttHelper.PublishAsync(topic, tok.ToString());
            }
        }


        private static Client.TokenClient m_alkoTokenClient;
        private static MqttHelper m_mqttHelper;

        private static ThingContainer m_things = new ThingContainer();
    };
}
