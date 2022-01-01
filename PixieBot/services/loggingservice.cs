using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PixieBot.Services
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly ILogger _logger;

        public LoggingService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider services)
        {
            _logger = services.GetRequiredService<ILogger<LoggingService>>();
            _discord = discord;
            _commands = commands;
            _discord.Ready += OnReadyAsync;
            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        // this method executes on the bot being connected/ready
        public Task OnReadyAsync()
        {
            return Task.CompletedTask;

        }
        private async Task<Task> OnLogAsync(LogMessage msg)
        {
            string logText = $": {msg.Exception?.ToString() ?? msg.Message}";
            switch (msg.Severity.ToString())
            {
                case "Critical":
                    {
                        _logger.LogCritical(logText);
                        break;
                    }
                case "Warning":
                    {
                        _logger.LogWarning(logText);
                        break;
                    }
                case "Info":
                    {
                        _logger.LogInformation(logText);
                        break;
                    }
                case "Verbose":
                    {
                        _logger.LogInformation(logText);
                        break;
                    }
                case "Debug":
                    {
                        _logger.LogDebug(logText);
                        break;
                    }
                case "Error":
                    {
                        _logger.LogError(logText);
                        break;
                    }
            }

            return Task.CompletedTask;
        }
    }
}