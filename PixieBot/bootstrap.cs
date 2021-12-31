using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            await provider.GetRequiredService<Services.BootstrapService>().StartAsync();
            await Task.Delay(-1);
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
            .AddSingleton<Services.BootstrapService>()
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
