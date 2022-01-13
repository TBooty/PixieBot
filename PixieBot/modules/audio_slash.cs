using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixieBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace PixieBot.Modules
{
    [Group("audio", "play audio from various links")]
    public class AudioSlash : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private readonly LavaNode _lavaNode;
        private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);
        private readonly ILogger _log;
        public readonly HashSet<ulong> VoteQueue;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;
        private readonly DiscordSocketClient _client;

        public AudioSlash(LavaNode lavaNode, DiscordSocketClient client, IServiceProvider services)
        {
            _client = client;
            _log = services.GetRequiredService<ILogger<AudioSlash>>();
            _lavaNode = lavaNode;
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            _lavaNode.OnPlayerUpdated += OnPlayerUpdated;
            _lavaNode.OnStatsReceived += OnStatsReceived;
            _lavaNode.OnTrackEnded += OnTrackEnded;
            _lavaNode.OnTrackStarted += OnTrackStarted;
            _lavaNode.OnTrackException += OnTrackException;
            _lavaNode.OnTrackStuck += OnTrackStuck;
            _lavaNode.OnWebSocketClosed += OnWebSocketClosed;
            _client.Ready += ClientReadyAsync;
        }


        [SlashCommand("ping", "Recieve a pong")]
        public async Task Ping()
        {
            await RespondAsync("pong");
        }

        [SlashCommand("play", "Plays a song via youtube link")]
        public async Task PlayAsync([Summary(description: "mention the user")] string seacrchString)
        {
            if (string.IsNullOrWhiteSpace(seacrchString))
            {
                await RespondAsync("Please provide search terms.");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }

            var searchResponse = await _lavaNode.SearchAsync(SearchType.Direct, seacrchString);
            if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
            {
                await RespondAsync($"I wasn't able to find anything for `{seacrchString}`.");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
            {
                player.Queue.Enqueue(searchResponse.Tracks);
                await RespondAsync($"Enqueued {searchResponse.Tracks.Count} songs.");
            }
            else
            {
                var track = searchResponse.Tracks.FirstOrDefault();
                player.Queue.Enqueue(track);

                await RespondAsync($"Added {track?.Title} to the queue.... I guess i'll play this song... you could pick better next time.");
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
                await RespondAsync("Failed to play song");
            }

        }

        [SlashCommand("pause", "Pauses the currently playing track")]
        public async Task PauseAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await RespondAsync("I cannot pause when I'm not playing anything!");
                return;
            }

            try
            {
                await player.PauseAsync();
                await RespondAsync($"Paused: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to pause track");
                await RespondAsync(exception.Message);
            }
        }

        [SlashCommand("resume", "Resumes the currently playing song")]
        public async Task ResumeAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Paused)
            {
                await RespondAsync("No current track to resume");
                return;
            }

            try
            {
                await player.ResumeAsync();
                await RespondAsync($"Resumed: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to resume track");
                await RespondAsync("Failed to resume track");
            }
        }

        [SlashCommand("stop", "Stops the currently playing song")]
        public async Task StopAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                await RespondAsync("No current track playing to stop");
                return;
            }

            try
            {
                await player.StopAsync();
                await RespondAsync("No longer playing anything.");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to stop track");
                await RespondAsync("Failed to stop track");
            }
        }


        [SlashCommand("skip", "Skips the currently playing song")]
        public async Task SkipAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await RespondAsync("No current track playing");
                return;
            }

            try
            {
                var (oldTrack, currenTrack) = await player.SkipAsync();
                await RespondAsync($"Skipped: {oldTrack.Title}\nNow Playing: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to skip track");
                await RespondAsync("Failed to skip track");
            }
        }

        [SlashCommand("nowplaying", "Displaying the currently playing song")]
        public async Task NowPlayingAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await RespondAsync("No current track playing");
                return;
            }

            var track = player.Track;
            var artwork = await track.FetchArtworkAsync();

            var embed = new EmbedBuilder()
                .WithAuthor(track.Author, Context.Client.CurrentUser.GetAvatarUrl(), track.Url)
                .WithTitle($"Now Playing: {track.Title}")
                .WithImageUrl(artwork)
                .WithFooter($"{track.Position}/{track.Duration}");

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("queue", "Displays the queue of pixie")]
        public async Task QueueAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }

            await RespondAsync(player.PlayerState != PlayerState.Playing
                ? "There's nothing in queue to display"
                : string.Join(Environment.NewLine, player.Queue.Select(x => x.Title)));
            return;
        }

        [SlashCommand("leave", "Leaves the current voice channel")]
        public async Task LeaveAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to any voice channels!");
                return;
            }

            var voiceChannel = (Context.User as IVoiceState)?.VoiceChannel ?? player.VoiceChannel;
            if (voiceChannel == null)
            {
                await RespondAsync("Not sure which voice channel to disconnect from.");
                return;
            }

            try
            {
                await _lavaNode.LeaveAsync(voiceChannel);
                await RespondAsync($"I've left {voiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to leave voice channel");
                await RespondAsync("Failed to leave voice channel");
            }
        }

        [SlashCommand("join", "Calls Pixie to come to the current voice room with some treats")]
        public async Task JoinAsync()
        {
            if (_lavaNode.HasPlayer(Context.Guild))
            {
                await RespondAsync("I'm already connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await RespondAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                await RespondAsync($"Joined {voiceState.VoiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to join voice channel");
                await RespondAsync("Failed to join voice channel");
            }
        }



        [SlashCommand("genius", "Displays the lyrics for the current song", runMode: RunMode.Async)]
        public async Task ShowGeniusLyrics()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await RespondAsync("No current track playing");
                return;
            }

            var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await RespondAsync($"No lyrics found for {player.Track.Title}");
                return;
            }
            await SendLyricsAsync(lyrics);
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
                    await RespondAsync($"```{stringBuilder}```");
                    stringBuilder.Clear();
                }
                else
                {
                    stringBuilder.AppendLine(line);
                }
            }

            await RespondAsync($"```{stringBuilder}```");
        }


        private async Task ClientReadyAsync()
        {
            await _lavaNode.ConnectAsync();
            _log.LogInformation($"Connected to Audio Service");
        }

        private Task OnPlayerUpdated(PlayerUpdateEventArgs arg)
        {
            _log.LogInformation($"Track update received for @arg", arg);
            return Task.CompletedTask;
        }

        private Task OnStatsReceived(StatsEventArgs arg)
        {
            _log.LogInformation($"Lavalink has been up for {arg?.Uptime}. CPU Usage: {arg?.Cpu.LavalinkLoad}%. Memory Usage:{arg?.Memory.Used}");
            return Task.CompletedTask;
        }

        private async Task OnTrackStarted(TrackStartEventArgs arg)
        {
            if (!_disconnectTokens.TryGetValue(arg.Player.VoiceChannel.Id, out var value))
            {
                return;
            }

            if (value.IsCancellationRequested)
            {
                return;
            }

            value.Cancel(true);
            await arg.Player.TextChannel.SendMessageAsync("Auto disconnect has been cancelled!");
        }

        private async Task OnTrackEnded(TrackEndedEventArgs args)
        {
            if (args.Reason != TrackEndReason.Finished)
            {
                return;
            }

            var player = args.Player;
            if (!player.Queue.TryDequeue(out var lavaTrack))
            {
                //set time out higher here
                await player.TextChannel.SendMessageAsync("Queue completed! Add some more tracks before I find something better to do.");
                _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(120));
                return;
            }

            if (lavaTrack is null)
            {
                await player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                return;
            }

            await args.Player.PlayAsync(lavaTrack);
        }

        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
        {
            if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
            {
                value = new CancellationTokenSource();
                _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
            }
            else if (value.IsCancellationRequested)
            {
                _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = _disconnectTokens[player.VoiceChannel.Id];
            }

            await player.TextChannel.SendMessageAsync($"Auto disconnect initiated! Disconnecting in {timeSpan}...");
            var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled)
            {
                return;
            }

            await _lavaNode.LeaveAsync(player.VoiceChannel);
            await player.TextChannel.SendMessageAsync("Leaving this voice channel.. Don't bother me again!");
        }

        private async Task OnTrackException(TrackExceptionEventArgs arg)
        {
            _log.LogError($"Track {arg.Track.Title} threw an exception. Please check Lavalink console/logs.");
            arg.Player.Queue.Enqueue(arg.Track);
            await arg.Player.TextChannel.SendMessageAsync(
                $"{arg.Track.Title} has been re-added to queue after throwing an exception.");
        }

        private async Task OnTrackStuck(TrackStuckEventArgs arg)
        {
            _log.LogError(
                $"Track {arg.Track.Title} got stuck for {arg.Threshold}ms. Please check Lavalink console/logs.");
            arg.Player.Queue.Enqueue(arg.Track);
            await arg.Player.TextChannel.SendMessageAsync(
                $"{arg.Track.Title} has been re-added to queue after getting stuck.");
        }

        private Task OnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            _log.LogError($"Discord WebSocket connection closed with following reason: {arg.Reason}");
            return Task.CompletedTask;
        }
    }
}
