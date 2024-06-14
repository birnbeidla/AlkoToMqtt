using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AlkoToMqtt.Client {
    internal class TokenClient {

        private const string URL = "https://idp.al-ko.com/connect/token";
        private const string AUTH_FILE_PATH = "auth.json";

        private class AuthData {

            public string AccessToken { get; set; }
            public string RefrehsToken { get; set; }
            public DateTime ExpireTimeUtc { get; set; }
        }

        public TokenClient(string clientID, string clientSecret, string userName, string userPassword) {
            m_clientID = clientID;
            m_clientSecret = clientSecret;
            m_userName = userName;  
            m_userPassword = userPassword;
        }

        public async Task AuthorizeAsync() {
                      
            if (File.Exists(AUTH_FILE_PATH)) {
                await AuthorizeFromFile();
            } else {
                await AuthorizeFromApi();
            }
        }

        public async Task<string> GetTokenAsync() {

            if (DateTime.UtcNow < m_authData.ExpireTimeUtc) {
                return m_authData.AccessToken;
            }

            Logger.WriteDebug("refreshing token");

            var contentString = string.Format("client_id={0}&client_secret={1}&grant_type=refresh_token&refresh_token={2}&scope=alkoCustomerId%20alkoCulture%20offline_access%20introspection",
                m_clientID, m_clientSecret, m_authData.RefrehsToken);

            var authJson = await MakeRequestAsync(contentString);

            var expiresInSeconds = (int)authJson["expires_in"];

            m_authData = new AuthData() {
                AccessToken = authJson["access_token"].ToString(),
                RefrehsToken = authJson["refresh_token"].ToString(),
                ExpireTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(expiresInSeconds)
            };

            File.WriteAllText("auth.json", JsonConvert.SerializeObject(m_authData));
        
            return m_authData.AccessToken;
        }

        private async Task AuthorizeFromApi() {

            Logger.WriteDebug("auth from api");

            var contentString = string.Format("client_id={0}&client_secret={1}&grant_type=password&username={2}&password={3}&scope=alkoCustomerId%20alkoCulture%20offline_access%20introspection",
                   m_clientID, m_clientSecret, m_userName, m_userPassword);

            var authJson = await MakeRequestAsync(contentString);

            var expiresInSeconds = int.Parse(authJson["expires_in"].ToString());

            m_authData = new AuthData() {
                AccessToken = authJson["access_token"].ToString(),
                RefrehsToken = authJson["refresh_token"].ToString(),
                ExpireTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(expiresInSeconds)
            };

            File.WriteAllText("auth.json", JsonConvert.SerializeObject(m_authData));
        }

        private async Task AuthorizeFromFile() {

            Logger.WriteDebug("auth from file");

            var authJsonStr = File.ReadAllText(AUTH_FILE_PATH);
            m_authData = JsonConvert.DeserializeObject<AuthData>(authJsonStr);

            if (m_authData.ExpireTimeUtc < DateTime.UtcNow) {
                Logger.WriteDebug("auth from file -> token expired. falling back.");
                await AuthorizeFromApi();
            }
        }

        private async Task<JObject> MakeRequestAsync(string contentString) {

            var content = new StringContent(contentString, null, "application/x-www-form-urlencoded");

            var client = new HttpClient();

            var response = await client.PostAsync(URL, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                var msg = string.Format("invalid status code {0}. content={1}", response.StatusCode, responseContent);
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<JObject>(responseContent);
        }

        string m_clientID;
        string m_clientSecret;
        string m_userName;
        string m_userPassword;

        AuthData m_authData;
    }
}
