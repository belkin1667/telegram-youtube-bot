using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TelegramBot
{
    public static class Http
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<T> PostAsync<T>(string url, Dictionary<string, string> values) 
        {
            var content = new FormUrlEncodedContent(values);
            //client.DefaultRequestHeaders.Add("", "");
            var response = await client.PostAsync(url, content);
            

            var str = await response.Content.ReadAsStringAsync();
            var code = response.StatusCode;
            
            if (typeof(T) == typeof(String))
                return (T)Convert.ChangeType(str, typeof(T));

            if (typeof(T) == typeof(HttpStatusCode))
                return (T)Convert.ChangeType(code, typeof(T));

            return default(T);
        }

        public static async Task<String> PostAsync(string url, Dictionary<string, string> values)
        {
            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(url, content);

            var str = await response.Content.ReadAsStringAsync();

            return str;
        }

        public static async void PutAsync()
        {
            throw new NotImplementedException();
        }

        public static async void SendAsync()
        {
            throw new NotImplementedException();
        }

    }

}
