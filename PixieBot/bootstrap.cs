using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Victoria;

namespace PixieBot
{
    public class Bootstrap
    {
        private string _configFile = "config//pixie.json";
        public IConfigurationRoot Configuration { get; }
        public Bootstrap(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(_configFile);
            Configuration = builder.Build();

            Console.Out.WriteLineAsync("Bot Name:          " + "Pixie");
            Console.Out.WriteLineAsync("Bot Version:       " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
            Console.Out.WriteLineAsync("Bot Prefix:        " + Environment.GetEnvironmentVariable("bot_prefix"));
            Console.Out.WriteLineAsync($"Meow");
        }

        public static async Task RunAsync(string[] args)
        {
            var start = new Bootstrap(args);
            await start.RunAsync();
        }

        public async Task RunAsync()
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/pixie.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<Services.LoggingService>();
            provider.GetRequiredService<Services.CommandHandler>();

            await ConnectBotToDiscord(provider);
            await Task.Delay(-1);
        }

        private async Task ConnectBotToDiscord(IServiceProvider services)
        {
            var _log = services.GetRequiredService<ILogger<Bootstrap>>();
            var discord = services.GetRequiredService<DiscordSocketClient>();
            var command_service = services.GetRequiredService<CommandService>();

            // Get the discord token from environment variables
            string discordToken = Environment.GetEnvironmentVariable("discord_token");
            if (string.IsNullOrWhiteSpace(discordToken))
            {
                throw new Exception("No discord tokens found in environment variables");
            }
            _log.LogInformation("Logging into Discord...");

            // Login to discord
            await discord.LoginAsync(Discord.TokenType.Bot, discordToken);

            // Connect to the websocket
            await discord.StartAsync();

            // Load commands and modules into the command service
            await command_service.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            _log.LogInformation($"Pixie bot started!");
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
            }))
            .AddSingleton<Services.CommandHandler>()
            .AddLogging(configure => configure.AddSerilog())
            .AddSingleton<Services.LoggingService>()
            .AddSingleton<Services.HttpService>()
            .AddSingleton<Services.AudioService>()
            .AddSingleton<LavaNode>()
            .AddSingleton<LavaConfig>()
            .AddSingleton(Configuration);
        }
    }
}
