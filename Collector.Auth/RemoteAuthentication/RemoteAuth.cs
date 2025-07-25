using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System.Xml.Linq;

namespace Collector.Auth.RemoteAuthentication
{

    public interface IRemoteAuth
    {
        Task PingAuthentication();
        Task<RemoteAuthenicationResponse> AuthenticateAsync(string username, string password, string secret, string ip = null);
        RemoteAuthenicationResponse Authenticate(string username, string password, string secret, string ip);
    }

    public sealed class RemoteAuth : IRemoteAuth
    {
        private IHttpClientFactory _factory;

        public RemoteAuth(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        public async Task PingAuthentication()
        {
            var client = _factory.CreateClient();
            using (var response = await client.GetAsync("[your-remove-server-here]"))
            {
                var content = await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<RemoteAuthenicationResponse> AuthenticateAsync(string username, string password, string secret, string ip = null)
        {
            //ToDo verify athentication string

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            var postData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("token", secret),
                new KeyValuePair<string,string>("username", username),
                new KeyValuePair<string,string>("password", password),
                new KeyValuePair<string,string>("userip", ip),
            });

            using (var response = await client.PostAsync("[your-remote-api-here]", postData))
            {
                var content = await response.Content.ReadAsStringAsync();
                XDocument xml = XDocument.Parse(content);
                XElement xe = xml.Element("auth");

                // if the error element even exists, fail as invalid
                if (xe.Element("error") != null)
                {
                    // checks error code to see if the code returned is an IP block
                    if (xe.Element("code") != null)
                    {
                        if (xe.Element("code").Value == "6")
                            return RemoteAuthenicationResponse.IPLocked;
                    }
                    return RemoteAuthenicationResponse.Invalid;
                }

                return bool.Parse(xe.Element("authenticated").Value) ? RemoteAuthenicationResponse.Valid : RemoteAuthenicationResponse.Invalid;

            }
        }

        public RemoteAuthenicationResponse Authenticate(string username, string password, string secret, string ip)
        {
            Task<RemoteAuthenicationResponse> process = Task.Run(async () =>
            {
                return await AuthenticateAsync(username, password, secret, ip);
            });
            return process.Result;
        }
    }
}
