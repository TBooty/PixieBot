using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Victoria;

namespace PixieBot
{
    public class Bootstrap
    {
        private const string _culture = "en-US";
        public Bootstrap(string[] args)
        {
            Console.Out.WriteLineAsync("Bot Name:          " + "Pixie");
            Console.Out.WriteLineAsync("Bot Version:       " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
            Console.Out.WriteLineAsync("Bot Prefix:        " + Environment.GetEnvironmentVariable("bot_prefix"));
            Console.Out.WriteLineAsync($"Meow");
            var cultureInfo = new CultureInfo(_culture);
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            Console.Out.WriteLineAsync("Bot Culture:       " + _culture);
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
            await provider.GetRequiredService<Services.CommandHandler>().InitializeAsync();

            await ConnectBotToDiscord(provider);
            await Task.Delay(-1);
        }

        private async Task ConnectBotToDiscord(IServiceProvider services)
        {
            var _log = services.GetRequiredService<ILogger<Bootstrap>>();
            var discord = services.GetRequiredService<DiscordSocketClient>();
            var command_service = services.GetRequiredService<CommandService>();
            var interactionService = services.GetRequiredService<InteractionService>();
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


            discord.Ready += async () =>
            {
                if (IsDebug())
                {
                    await interactionService.RegisterCommandsToGuildAsync(552867282364268550);
                }
                else
                {
                    await interactionService.RegisterCommandsGloballyAsync(true);
                }
            };
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
                DefaultRunMode = Discord.Commands.RunMode.Async,
            }))
            .AddSingleton<Services.CommandHandler>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddLogging(configure => configure.AddSerilog())
            .AddSingleton<Services.LoggingService>()
            .AddSingleton<Services.HttpService>()
            .AddSingleton<Services.AudioService>()
            .AddSingleton<LavaNode>();

            if(IsDebug())
            {
                services.AddSingleton<LavaConfig>();
            }
            else
            {
                var config = new LavaConfig()
                {
                    Hostname = "lavalink",
                    Port = 2333
                };
                services.AddSingleton<LavaConfig>(config);
            }
        }

        static bool IsDebug()
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}
