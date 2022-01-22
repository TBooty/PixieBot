using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace PixieBot.Services
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly InteractionService _interactionService;
        private readonly string _commandPrefix;
        public CommandHandler(
            IServiceProvider provider,
            DiscordSocketClient discord,
            CommandService commands,
            InteractionService interactionServivce)
        {
            _provider = provider;
            _discord = discord;
            _commands = commands;
            _interactionService = interactionServivce; 
            _commandPrefix = Environment.GetEnvironmentVariable("bot_prefix");
            _discord.MessageReceived += OnMessageReceivedAsync;


        }

        public async Task InitializeAsync()
        {
            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            // Process the InteractionCreated payloads to execute Interactions commands
            _discord.InteractionCreated += HandleInteraction;

            // Process the command execution results 
            _interactionService.SlashCommandExecuted += SlashCommandExecuted;
            _interactionService.ContextCommandExecuted += ContextCommandExecuted;
            _interactionService.ComponentCommandExecuted += ComponentCommandExecuted;
            _discord.ButtonExecuted += async (interaction) =>
            {
                var ctx = new SocketInteractionContext<SocketMessageComponent>(_discord, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, _provider);
            };
            _discord.SelectMenuExecuted += async (interaction) =>
            {
                var ctx = new SocketInteractionContext<SocketMessageComponent>(_discord, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, _provider);
            };
        }


        private Task ComponentCommandExecuted(ComponentCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    case InteractionCommandError.UnknownCommand:
                        // implement
                        break;
                    case InteractionCommandError.BadArgs:
                        // implement
                        break;
                    case InteractionCommandError.Exception:
                        // implement
                        break;
                    case InteractionCommandError.Unsuccessful:
                        // implement
                        break;
                    default:
                        break;
                }
            }

            return Task.CompletedTask;
        }

        private Task ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    case InteractionCommandError.UnknownCommand:
                        // implement
                        break;
                    case InteractionCommandError.BadArgs:
                        // implement
                        break;
                    case InteractionCommandError.Exception:
                        // implement
                        break;
                    case InteractionCommandError.Unsuccessful:
                        // implement
                        break;
                    default:
                        break;
                }
            }

            return Task.CompletedTask;
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    case InteractionCommandError.UnknownCommand:
                        // implement
                        break;
                    case InteractionCommandError.BadArgs:
                        // implement
                        break;
                    case InteractionCommandError.Exception:
                        // implement
                        break;
                    case InteractionCommandError.Unsuccessful:
                        // implement
                        break;
                    default:
                        break;
                }
            }

            return Task.CompletedTask;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_discord, arg);
                await _interactionService.ExecuteCommandAsync(ctx, _provider);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == Discord.InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
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
