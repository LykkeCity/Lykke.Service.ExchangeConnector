using System;
using System.Collections.Generic;
using System.Net.Http;

namespace PusherClient.DotNetCore
{
    public class HttpAuthorizer: IAuthorizer
    {
        private readonly Uri authEndpoint;
        public HttpAuthorizer(string authEndpoint)
        {
            this.authEndpoint = new Uri(authEndpoint);
        }

        public string Authorize(string channelName, string socketId)
        {
            string authToken;


            using (var client = new HttpClient())
            {
                authToken = client.PostAsync(authEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>()
                    {
                        { "channel_name", channelName },
                        { "socket_id", socketId }
                    })).Result.Content.ReadAsStringAsync().Result;
            }
            
//            using (var webClient = new WebClient())
//            {
//                webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
//                authToken = webClient.UploadString(authEndpoint, "POST", $"channel_name={channelName}&socket_id={socketId}");
//            }

            return authToken;
        }
    }
}