using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace PixieBot.Services
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly string _commandPrefix;
        public CommandHandler(
            IServiceProvider provider,
            DiscordSocketClient discord,
            CommandService commands)
        {
            _provider = provider;
            _discord = discord;
            _commands = commands;
            _commandPrefix = Environment.GetEnvironmentVariable("bot_prefix");

            _discord.MessageReceived += OnMessageReceivedAsync;
        }


        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            // Ensure the message is from a user/bot
            var msg = s as SocketUserMessage;
            if (msg == null) return;
            if (msg.Author.IsBot || msg.Author.Id == _discord.CurrentUser.Id) return;


            // Create the command context
            var context = new SocketCommandContext(_discord, msg);

            // Check if the message has a valid command prefix
            int argPos = 0;

            // Otherwise, check if there's accidentally an extra space after the prefix
            if (msg.HasStringPrefix(_commandPrefix, ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                // Execute the command
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                // If not successful, reply with the error.
                if (!result.IsSuccess)
                {
                    await context.Channel.SendMessageAsync(result.ToString());
                }
            }
            else
            {
                // Then check for just the prefix without the space
                if (msg.HasStringPrefix(_commandPrefix, ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
                {
                    // Execute the command
                    var result = await _commands.ExecuteAsync(context, argPos, _provider);

                    // If not successful, reply with the error.
                    if (!result.IsSuccess)
                    {
                        await context.Channel.SendMessageAsync(result.ToString());
                    }
                }
            }
        }
    }
}
