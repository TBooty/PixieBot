using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PixieBot.Services
{
    public class BootstrapService
    {
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly Microsoft.Extensions.Logging.ILogger _log;

        public BootstrapService(
           IConfigurationRoot config,
           IServiceProvider provider,
           DiscordSocketClient discord,
           CommandService commands,
           IServiceProvider services)
        {
            _config = config;
            _provider = provider;
            _discord = discord;
            _commands = commands;
            _log = services.GetRequiredService<ILogger<BootstrapService>>();
        }

        public async Task StartAsync()
        {
            // Get the discord token from environment variables
            string discordToken = Environment.GetEnvironmentVariable("discord_token");
            if (string.IsNullOrWhiteSpace(discordToken))
            {
                throw new Exception("No discord tokens found in environment variables");
            }
            _log.LogInformation($"{_config["name"]} starting up...");

            // Login to discord
            await _discord.LoginAsync(Discord.TokenType.Bot, discordToken);

            // Connect to the websocket
            await _discord.StartAsync();

            // Load commands and modules into the command service
            var assemblies = Assembly.GetEntryAssembly();
            foreach (var assembly in assemblies.GetModules())
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.Namespace != null)
                    {
                        if (type.Namespace.Contains("PixieBot.Modules") && type.IsSubclassOf(typeof(ModuleBase<SocketCommandContext>)))
                        {
                            await _commands.AddModuleAsync(type, _provider);
                        }
                    }
                }
            }
            _log.LogInformation($"{_config["name"]} started!");
        }
    }
}
