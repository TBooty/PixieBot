using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixieBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;

namespace PixieBot.Modules
{
    public class AudioModule : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode _lavaNode;
        private readonly AudioService _audioService;
        private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);
        private readonly ILogger _log;

        public AudioModule(LavaNode lavaNode, AudioService audioService, IServiceProvider services)
        {
            _lavaNode = lavaNode;
            _audioService = audioService;
            _log = services.GetRequiredService<ILogger<AutomationModule>>();
        }

        [Command("Join")]
        public async Task JoinAsync()
        {
            if (_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to join voice channel");
                await ReplyAsync("Failed to join voice channel");
            }
        }

        [Command("Leave")]
        public async Task LeaveAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to any voice channels!");
                return;
            }

            var voiceChannel = (Context.User as IVoiceState)?.VoiceChannel ?? player.VoiceChannel;
            if (voiceChannel == null)
            {
                await ReplyAsync("Not sure which voice channel to disconnect from.");
                return;
            }

            try
            {
                await _lavaNode.LeaveAsync(voiceChannel);
                await ReplyAsync($"I've left {voiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to leave voice channel");
                await ReplyAsync("Failed to leave voice channel");
            }
        }

        [Command("Play")]
        public async Task PlayAsync([Remainder] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ReplyAsync("Please provide search terms.");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            var searchResponse = await _lavaNode.SearchAsync(SearchType.Direct, searchQuery);
            if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
            {
                await ReplyAsync($"I wasn't able to find anything for `{searchQuery}`.");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
            {
                player.Queue.Enqueue(searchResponse.Tracks);
                await ReplyAsync($"Enqueued {searchResponse.Tracks.Count} songs.");
            }
            else
            {
                var track = searchResponse.Tracks.FirstOrDefault();
                player.Queue.Enqueue(track);

                await ReplyAsync($"Enqueued {track?.Title}");
            }

            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                return;
            }

            player.Queue.TryDequeue(out var lavaTrack);
            try
            {
                await player.PlayAsync(x => {
                    x.Track = lavaTrack;
                    x.ShouldPause = false;
                });
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to play track");
                await ReplyAsync("Failed to play song");
            }
            
        }

        [Command("Pause")]
        public async Task PauseAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("I cannot pause when I'm not playing anything!");
                return;
            }

            try
            {
                await player.PauseAsync();
                await ReplyAsync($"Paused: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to pause track");
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Resume")]
        public async Task ResumeAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Paused)
            {
                await ReplyAsync("No current track to resume");
                return;
            }

            try
            {
                await player.ResumeAsync();
                await ReplyAsync($"Resumed: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to resume track");
                await ReplyAsync("Failed to resume track");
            }
        }

        [Command("Stop")]
        public async Task StopAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAsync("No current track playing to stop");
                return;
            }

            try
            {
                await player.StopAsync();
                await ReplyAsync("No longer playing anything.");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to stop track");
                await ReplyAsync("Failed to stop track");
            }
        }

        [Command("Skip")]
        public async Task SkipAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("No current track playing");
                return;
            }

            try
            {
                var (oldTrack, currenTrack) = await player.SkipAsync();
                await ReplyAsync($"Skipped: {oldTrack.Title}\nNow Playing: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to skip track");
                await ReplyAsync("Failed to skip track");
            }
        }


        [Command("NowPlaying"), Alias("Np")]
        public async Task NowPlayingAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("No current track playing");
                return;
            }

            var track = player.Track;
            var artwork = await track.FetchArtworkAsync();

            var embed = new EmbedBuilder()
                .WithAuthor(track.Author, Context.Client.CurrentUser.GetAvatarUrl(), track.Url)
                .WithTitle($"Now Playing: {track.Title}")
                .WithImageUrl(artwork)
                .WithFooter($"{track.Position}/{track.Duration}");

            await ReplyAsync(embed: embed.Build());
        }

        [Command("Genius", RunMode = RunMode.Async)]
        public async Task ShowGeniusLyrics()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("No current track playing");
                return;
            }

            var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await ReplyAsync($"No lyrics found for {player.Track.Title}");
                return;
            }
            await SendLyricsAsync(lyrics);
        }


        [Command("Queue")]
        public Task QueueAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return ReplyAsync("I'm not connected to a voice channel.");
            }

            return ReplyAsync(player.PlayerState != PlayerState.Playing
                ? "There's nothing in queue to display"
                : string.Join(Environment.NewLine, player.Queue.Select(x => x.Title)));
        }

        [Command("ClearQueue")]
        public Task ClearQueueAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return ReplyAsync("I'm not connected to a voice channel.");
            }
            player.Queue.Clear();
            return ReplyAsync(player.PlayerState != PlayerState.Playing
                ? "There's nothing in queue to display"
                : string.Join(Environment.NewLine, player.Queue.Select(x => x.Title)));
        }

        private async Task SendLyricsAsync(string lyrics)
        {
            var splitLyrics = lyrics.Split(Environment.NewLine);
            var stringBuilder = new StringBuilder();
            foreach (var line in splitLyrics)
            {
                if (line.Contains('['))
                {
                    stringBuilder.Append(Environment.NewLine);
                }

                if (Range.Contains(stringBuilder.Length))
                {
                    await ReplyAsync($"```{stringBuilder}```");
                    stringBuilder.Clear();
                }
                else
                {
                    stringBuilder.AppendLine(line);
                }
            }

            await ReplyAsync($"```{stringBuilder}```");
        }
    }
}
