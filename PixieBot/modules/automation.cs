using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PixieBot.modules;
using PixieBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PixieBot.Modules
{
    public class AutomationModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly HttpService _httpService;
        private readonly string _goveeApiKey;
        private readonly ILogger _log;

        public AutomationModule(DiscordSocketClient discord, CommandService commands, HttpService httpService, IServiceProvider services)
        {
            _discord = discord;
            _commands = commands;
            _httpService = httpService;
            _discord.ButtonExecuted += ButtonExecuted;
            _goveeApiKey = Environment.GetEnvironmentVariable("govee_api_key");
            _log = services.GetRequiredService<ILogger<AutomationModule>>();
        }

        //todo cache results of this outside the module since it's disposed after calling
        [Command("DeviceList")]
        [Summary("List devices on Govee")]
        public async Task DeviceList()
        {
            string url = $"https://developer-api.govee.com/v1/devices";
            var headers = new Dictionary<string, string>()
            {
                {
                    "Govee-API-Key",
                    $"{_goveeApiKey}"
                }
            };
            var jsonData = _httpService.GetRawJSONDataFromUrlAsync(url, headers).Result;

            var button_builder = new ComponentBuilder();
            var blah = JsonConvert.DeserializeObject(jsonData.ToString());
            foreach (var item in blah.data.devices)
            {
                string model_mac_device_string = item.device.ToString() + " " + item.model.ToString();
                button_builder.WithButton(item.deviceName.ToString(), model_mac_device_string, ButtonStyle.Primary);
            }
            var buttons = button_builder.Build();
            await ReplyAsync(message: "Current Device list below", components: buttons);


        }

        //todo pull from cache and then negate current state
        private async Task UpdateDevice(string device)
        {
            var device_mac = device.Split(' ')[0];
            var model = device.Split(' ')[1];
            string url = $"https://developer-api.govee.com/v1/devices/control";
            var headers = new Dictionary<string, string>()
            {
                {
                    "Govee-API-Key",
                    $"{_goveeApiKey}"
                }
            };
            var stuff = new GoveeRequest()
            {
                device = "",
                model = "",
                cmd = new Cmd()
                {
                    name = "turn",
                    value = "off"
                }

            };
            var jsonData = _httpService.PostToUrlAsync(url, headers, stuff).Result;
        }
        private async Task ButtonExecuted(SocketMessageComponent component)
        {
            if (component.HasResponded == false)
            {
                await UpdateDevice(component.Data.CustomId);
            }
        }
    }
}
