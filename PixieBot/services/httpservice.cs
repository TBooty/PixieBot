using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PixieBot.Services
{
    public class HttpService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly HttpClient _client = new HttpClient();


        public HttpService(DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;

            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = true
            };

            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.95 Safari/537.36");
        }


        public async Task<dynamic> GetRawJSONDataFromUrlAsync(string url, Dictionary<string, string> headers)
        {
            Uri uri = new Uri(url);
            _client.DefaultRequestHeaders.Authorization = null;
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            foreach(var header in headers)
            {
                _client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            HttpResponseMessage response = await _client.GetAsync(uri);
            var data = response.Content.ReadAsStringAsync();

            dynamic jsonData = JsonConvert.DeserializeObject(data.Result);

            return jsonData;
        }

        public async Task<dynamic> PostToUrlAsync(string url, Dictionary<string, string> headers, object body)
        {
            Uri uri = new Uri(url);
            _client.DefaultRequestHeaders.Authorization = null;
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            foreach (var header in headers)
            {
                _client.DefaultRequestHeaders.Add(header.Key, header.Value);
                
            }
            var json = JsonConvert.SerializeObject(body);
            var stuff = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PutAsync(uri, stuff);
            var data = response.Content.ReadAsStringAsync();

            dynamic jsonData = JsonConvert.DeserializeObject(data.Result);

            return jsonData;
        }
    }
}
