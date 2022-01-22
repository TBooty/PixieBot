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
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
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
        private AudioService _service;
        private DiscordSocketClient _discord;
        public static string _pixieGreetingLink = "https://www.youtube.com/watch?v=kpfoFp3CSfU";


        public AudioSlash(LavaNode lavaNode, IServiceProvider services, AudioService service)
        {
            _log = services.GetRequiredService<ILogger<AudioSlash>>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _lavaNode = lavaNode;
            _service = service;
        }

        [SlashCommand("play", "Plays a song via youtube link")]
        public async Task PlaySongAsync([Summary(description: "the youtube link")] string searchString)
        {
            await PlayAsync(searchString);
        }

        private async Task PlayAsync(string searchString)
        {
            bool joined_voice = await JoinVoiceAsync();
            if (!joined_voice)
            {
                await RespondAsync("Couldn't play song as we're not connected to any voice channel");
                return;
            }


            //Default to searching words from youtube
            SearchType searchType = SearchType.YouTube;

            //if link contains a list parameter, pull the id from the list and search direct
            if (searchString.Contains("youtube") && searchString.Contains("list="))
            {
                var blah = searchString.IndexOf("list=");
                searchString = searchString.Substring(blah, searchString.Length - blah);
                searchString = searchString.Split("=")[1];
                searchType = SearchType.Direct;
            }
            
            //if passed a link, just look it up as direct
            if (Uri.IsWellFormedUriString(searchString, UriKind.Absolute))
            {
                searchType = SearchType.Direct;
            }
            var searchResponse = await _lavaNode.SearchAsync(searchType, searchString);
            switch (searchResponse.Status)
            {
                case SearchStatus.LoadFailed:
                case SearchStatus.NoMatches:
                    await RespondAsync($"I wasn't able to find anything for `{searchString}`.");
                    break;
                case SearchStatus.SearchResult:
                    var button_builder = new ComponentBuilder();
                    foreach (var track in searchResponse.Tracks.Take(5))
                    {
                        button_builder.WithButton(track.Title, track.Url, ButtonStyle.Primary);
                    }
                    var buttons = button_builder.Build();
                    await RespondAsync($"Found the following 5 results for {searchString} Click the button to add it to the queue", components: buttons,
                        ephemeral: true);
                    break;
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

        private async Task<bool> JoinVoiceAsync()
        {
            if (_lavaNode.HasPlayer(Context.Guild))
            {
                _log.LogInformation("I'm already connected to a voice channel!");
                return true;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                _log.LogWarning("You must be connected to a voice channel!");
                return false;
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                return true;
            }
            catch (Exception exception)
            {
                _log.LogError(exception: exception, message: "Failed to join voice channel");
            }
            return false;
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
    }
}
