using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PixieBot.modules;
using PixieBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixieBot.Modules
{
    [Name("Home-Auto")]
    public class AutomationModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfigurationRoot _config;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly HttpService _httpService;
        private readonly string _goveeApiKey;

        public AutomationModule(IConfigurationRoot config, DiscordSocketClient discord, CommandService commands, HttpService httpService)
        {
            _config = config;
            _discord = discord;
            _commands = commands;
            _httpService = httpService;
            _discord.ButtonExecuted += ButtonExecuted;
            _goveeApiKey = Environment.GetEnvironmentVariable("govee_api_key");

        }

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
            try
            {
                var blah = JsonConvert.DeserializeObject(jsonData.ToString());
                foreach (var item in blah.data.devices)
                {
                    button_builder.WithButton(item.deviceName.ToString(), item.deviceName.ToString(), ButtonStyle.Primary);
                }
            }
            catch (System.Exception)
            {

                throw;
            }
            var yay = button_builder.Build();
            

        }


        private async Task UpdateDevice()
        {
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
                switch (component.Data.CustomId)
                {
                    case "Tv lights":
                        {
                            await UpdateDevice();
                        }
                        break;
                    case "WhiteNoiseMachine":
                        {
                            await ReplyAsync("Device List");
                        }
                        break;
                    case "Bedroom lights":
                        {
                            await ReplyAsync("Device List");
                        }
                        break;
                    case "Paint light":
                        {
                            await ReplyAsync("Device List");
                        }
                        break;
                    case "Paint light 2":
                        {
                            await ReplyAsync("Device List");
                        }
                        break;

                }

            }
        }
    }
}
