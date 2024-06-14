using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace AlkoToMqtt.Client {
    internal class ApiClient {

        public ApiClient(string accessToken) {
            m_accessToken = accessToken;
        }

        public async Task<JArray> GetThingsAsync() {

            var responseStr = await CallApiAsync("things?pimInfo=1&thingState=1&accesses=1");
            var responseJson = JsonConvert.DeserializeObject<JArray>(responseStr);

            return responseJson;
        }

        public async Task<JObject> GetStateAsync(string thingName) {

            var responseStr = await CallApiAsync("things/" + thingName + "/state");
            var responseJson = JsonConvert.DeserializeObject<JObject>(responseStr);

            return responseJson;
        }

        public async Task<JObject> GetDesiredState(string thingName) {

            var responseStr = await CallApiAsync("things/" + thingName + "/state/desired");
            var responseJson = JsonConvert.DeserializeObject<JObject>(responseStr);

            return responseJson;
        }
        public async Task<JObject> GetReportedState(string thingName) {

            var responseStr = await CallApiAsync("things/" + thingName + "/state/reported");
            var responseJson = JsonConvert.DeserializeObject<JObject>(responseStr);

            return responseJson;
        }

        private async Task<string> CallApiAsync(string relativeUrl) {

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + m_accessToken); 
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            var response = await client.GetAsync("https://api.al-ko.com/v1/iot/" + relativeUrl);
            var responseStr = await response.Content.ReadAsStringAsync();

            return responseStr;
        }

        string m_accessToken;
    }
}
