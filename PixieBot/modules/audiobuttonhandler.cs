using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixieBot.Modules;
using PixieBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;

namespace PixieBot.modules
{
    public class audiobuttonhandler : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
    {
        private readonly LavaNode _lavaNode;
        public audiobuttonhandler(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
        }
        [ComponentInteraction("*youtube*")]
        public async Task Play()
        {
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }
            var response = await _lavaNode.SearchAsync(SearchType.Direct, Context.Interaction.Data.CustomId);
            var player = _lavaNode.GetPlayer(Context.Guild);
            var track = response.Tracks.FirstOrDefault();
            player.Queue.Enqueue(track);
            if(player.PlayerState == PlayerState.None || player.PlayerState == PlayerState.Stopped)
            {
                player.Queue.TryDequeue(out var lavaTrack);
                await player.PlayAsync(x =>
                {
                    x.Track = lavaTrack;
                    x.ShouldPause = false;
                });
            }

            await Context.Interaction.UpdateAsync(x =>
            {
                x.Content = $"{response.Tracks.FirstOrDefault().Title} has been added to the queue";
                x.Components = null;
            });
        }
    }
}
