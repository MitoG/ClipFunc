using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClipFunc.Configuration;
using ClipFunc.DataContext;
using ClipFunc.DataContext.Models;
using ClipFunc.Exceptions;
using Coravel.Invocable;
using Discord;
using Discord.Webhook;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Helix.Models.Clips.GetClips;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using Game = TwitchLib.Api.Helix.Models.Games.Game;

namespace ClipFunc.Invocables
{
    public class HandleClipsInvocable : IInvocable, ICancellableInvocable
    {
        public CancellationToken CancellationToken { get; set; }

        private readonly IDbContextFactory<ClipFuncContext> _contextFactory;
        private readonly ILogger<HandleClipsInvocable> _logger;
        private readonly List<ClipFuncChannel> _watchedChannels;

        private readonly TwitchAPI _api;

        public HandleClipsInvocable(IDbContextFactory<ClipFuncContext> factory,
            IOptions<ClipFuncOptions> clipFuncOptions,
            ILogger<HandleClipsInvocable> logger)
        {
            _contextFactory = factory;
            _logger = logger;

            var options = clipFuncOptions.Value;
            _watchedChannels = options.WatchedChannels;

            var twitchApiSettings = new ApiSettings()
            {
                ClientId = options.TwitchCredentials.ClientId,
                Secret = options.TwitchCredentials.ClientSecret
            };
            _api = new TwitchAPI(settings: twitchApiSettings);
        }

        public async Task Invoke()
        {
            await UpdateAccessToken();

            foreach (var channel in _watchedChannels)
            {
                _logger.LogInformation("Starting clip search for channel: {channel_id}", channel.BroadcasterId);
                var latestClip = await GetLatestClipByDate(channel.BroadcasterId);

                // If we didn't find any clips in the database it's likely,
                // that it's the first time we are looking for clips for this broadcaster. 
                var possibleFirstLoad = latestClip is null;

                var clips = await GetClips(channel.BroadcasterId, latestClip?.ClipId, latestClip?.ClipCreationDate);
                if (clips.Count == 0)
                {
                    _logger.LogInformation("No new clips found for channel `{channel_id}`. Continuing to next channel",
                        channel.BroadcasterId);
                    continue;
                }

                var newClips = await AddNewClips(clips);
                if (newClips.Count == 0)
                {
                    _logger.LogInformation("No new clips found for channel `{channel_id}`. Continuing to next channel",
                        channel.BroadcasterId);
                    continue;
                }

                if (possibleFirstLoad && newClips.Count > 2)
                {
                    _logger.LogInformation(
                        "Skipping webhook since it's likely that this is a first load for channel: {channel_id}",
                        channel.BroadcasterId);
                    continue;
                }

                foreach (var clip in newClips)
                {
                    await PostDiscordWebhook(channel, clip.ClipId);
                }
            }
        }

        private async Task UpdateAccessToken()
        {
            await using var context = await _contextFactory.CreateDbContextAsync(CancellationToken);
            var accessTokens = await context.AccessTokens
                .Where(x => DateTime.UtcNow < x.Expires && !x.IsExpired)
                .ToListAsync(CancellationToken);

            var accessToken = accessTokens.MaxBy(x => x.Expires)?.AccessToken;

            accessToken = await _api.Auth.GetAccessTokenAsync(accessToken);
            var validationResponse = await _api.Auth.ValidateAccessTokenAsync(accessToken);
            if (validationResponse is null)
                throw new ValidationException(
                    new ValidationResult("accessToken could not be validated"),
                    new RequiredAttribute(),
                    accessToken);

            var model = await context.AccessTokens
                .AsTracking()
                .FirstOrDefaultAsync(x => x.AccessToken == accessToken, CancellationToken);

            if (model is null)
            {
                var expires = DateTime.UtcNow.AddSeconds(validationResponse.ExpiresIn);
                var newModel = new AccessTokenModel
                {
                    AccessToken = accessToken,
                    Expires = expires,
                    IsExpired = expires <= DateTime.UtcNow,
                };
                await context.AccessTokens.AddAsync(newModel, CancellationToken);
            }
            else if (model.Expires <= DateTime.UtcNow)
            {
                model.IsExpired = true;
            }

            await context.SaveChangesAsync(CancellationToken);
            _api.Settings.AccessToken = accessToken;
        }

        private async Task<ClipModel?> GetLatestClipByDate(int broadcasterId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(CancellationToken);
            return await context.Clips
                .Where(x => x.BroadcasterId == broadcasterId)
                .OrderByDescending(x => x.ClipCreationDate)
                .FirstOrDefaultAsync(CancellationToken);
        }

        private async Task<List<Clip>> GetClips(int broadcasterId,
            string? latestClipId = null,
            DateTime? lastClipDate = null,
            string? cursor = null)
        {
            var clips = new List<Clip>();

            var now = DateTime.UtcNow;

            // we either start at the last clip date or at the start of the year to somewhat limit the request count
            var startedAt = lastClipDate ?? new DateTime(now.Year, 1, 1);

            // we only search for a day into the future to account for wibbly-wobbly-timey-wimey-shenanigans
            var endedAt = new DateTime(now.Year, now.Month, now.Day).AddDays(1);

            var clipsResponse = await _api.Helix.Clips
                .GetClipsAsync(
                    broadcasterId: broadcasterId.ToString(),
                    first: 100,
                    startedAt: startedAt,
                    endedAt: endedAt, // recommendation by twitch to include an end date,
                    // we won't find clips from the future ... I guess
                    after: cursor);

            if (clipsResponse?.Clips is null || // if the response is null
                clipsResponse.Clips.Length == 0 || // or empty
                (clipsResponse.Clips.Length == 1 && // or only has one clip
                 clipsResponse.Clips.First().Id == latestClipId)) // which is the latest,
            {
                return []; // we just return an empty list
            }

            clips.AddRange(clipsResponse.Clips);
            if (string.IsNullOrWhiteSpace(clipsResponse.Pagination.Cursor)) return clips;

            lastClipDate = clips.Select(x =>
                    DateTime.ParseExact(x.CreatedAt, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
                .Max();

            // we'll loop recursively while there is a pagination cursor in the response to get all clips
            var recClips = await GetClips(broadcasterId,
                lastClipDate: lastClipDate,
                cursor: clipsResponse.Pagination.Cursor);

            clips.AddRange(recClips);

            return clips;
        }

        private async Task<List<ClipModel>> AddNewClips(List<Clip> clips)
        {
            var newClips = await BuildClipModels(clips);
            if (newClips.Count == 0)
            {
                _logger.LogInformation("No clips remaining after building models");
                return [];
            }

            await using var context = await _contextFactory.CreateDbContextAsync(CancellationToken);
            await using var trans = await context.Database.BeginTransactionAsync(CancellationToken);

            newClips = await FilterClips(newClips);
            if (newClips.Count == 0)
            {
                _logger.LogInformation("No clips remaining after filtering");
                return [];
            }

            context.Clips.AddRange(newClips);

            var delta = await context.SaveChangesAsync(CancellationToken);
            if (newClips.Count != delta)
            {
                _logger.LogError(
                    "Could not add all new clips. Found {new_clips} but only added {added_clips}. Rolling back",
                    newClips.Count, delta);
                await trans.RollbackAsync(CancellationToken);
                return [];
            }

            _logger.LogInformation("Added {new_clips} clips to the database", delta);
            await trans.CommitAsync(CancellationToken);
            return newClips;
        }

        private async Task<List<ClipModel>> FilterClips(List<ClipModel> models)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(CancellationToken);

            var clipIds = models.Select(x => x.ClipId).Distinct().ToList();
            var existingClips = await context.Clips
                .Where(x => clipIds.Contains(x.ClipId))
                .Select(x => x.ClipId)
                .ToListAsync(CancellationToken);

            return models
                .ExceptBy(existingClips, x => x.ClipId)
                .ToList();
        }

        private async Task<List<ClipModel>> BuildClipModels(List<Clip> clips)
        {
            var gameIds = clips
                .Select(x => int.Parse(x.GameId))
                .Distinct()
                .ToList();
            var games = await GetGames(gameIds);

            var newClips = new List<ClipModel>();
            foreach (var clip in clips)
            {
                if (!int.TryParse(clip.BroadcasterId, out var clipBroadcasterId))
                {
                    _logger.LogError("Unable to parse broadcaster_id: `{broadcaster_id}` to int for clip: `{@clip}`",
                        clip.BroadcasterId, clip);
                    continue;
                }

                if (!int.TryParse(clip.CreatorId, out var clipCreatorId))
                {
                    _logger.LogError("Unable to parse creator_id: `{creator_id}` to int for clip: `{@clip}`",
                        clip.CreatorId, clip);
                    continue;
                }

                if (!int.TryParse(clip.GameId, out var clipGameId))
                {
                    _logger.LogError("Unable to parse game_id: `{game_id}` to int for clip: `{@clip}`",
                        clip.GameId, clip);
                    continue;
                }

                if (!DateTime.TryParseExact(clip.CreatedAt, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var clipCreationDate))
                {
                    _logger.LogError("Unable to parse created_at: `{created_at}` to DateTime for clip: `{@clip}`",
                        clip.CreatedAt, clip);
                    continue;
                }

                var game = games.FirstOrDefault(x => x.GameId == clipGameId);
                if (game is null)
                {
                    _logger.LogError("Unable to find game with id: `{game_id}` for clip: `{@clip}`",
                        clipGameId, clip);
                    continue;
                }

                var broadcaster = await GetUser(clipBroadcasterId);
                var creator = await GetUser(clipCreatorId);

                var newClip = new ClipModel
                {
                    ClipId = clip.Id,
                    Title = clip.Title,
                    BroadcasterId = broadcaster.UserId,
                    CreatorId = creator.UserId,
                    GameId = game.GameId,
                    ViewCount = clip.ViewCount,
                    Url = clip.Url,
                    ThumbnailUrl = clip.ThumbnailUrl,
                    ClipCreationDate = clipCreationDate,
                    Duration = clip.Duration,
                    VodOffset = clip.VodOffset,
                };
                newClips.Add(newClip);
            }

            return newClips;
        }

        private async Task<List<GameModel>> GetGames(List<int> gameIds)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(CancellationToken);
            var knownGames = await context.Games
                .Where(x => gameIds.Contains(x.GameId))
                .ToListAsync(CancellationToken);

            var unknownGames = gameIds.Except(knownGames.Select(x => x.GameId)).ToList();
            if (unknownGames.Count <= 0) return knownGames;

            var newGames = await AddNewGames(unknownGames);
            knownGames.AddRange(newGames);
            return knownGames.DistinctBy(x => x.GameId).ToList();
        }

        private async Task<UserModel> GetUser(int userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(CancellationToken);
            var model = await context.Users.FindAsync([userId], CancellationToken);
            if (model is not null)
                return model;

            var user = await GetTwitchUser(userId);

            if (!int.TryParse(user.Id, out userId))
            {
                _logger.LogError("Unable to parse user_id: `{user_id}` to int for user: `{@user}`", user.Id, user);
                throw new InvalidCastException($"Unable to parse user_id: `{user.Id}`");
            }

            model = new UserModel
            {
                UserId = userId,
                Username = user.DisplayName,
                ProfileImageUrl = user.ProfileImageUrl
            };

            context.Users.Add(model);
            await context.SaveChangesAsync(CancellationToken);
            return model;
        }

        private async Task<List<GameModel>> AddNewGames(List<int> gameIds)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(CancellationToken);
            await using var trans = await context.Database.BeginTransactionAsync(CancellationToken);

            var games = await GetClipGames(gameIds);
            var newGames = new List<GameModel>();
            foreach (var game in games)
            {
                if (!int.TryParse(game.Id, out var gameId))
                {
                    _logger.LogError("Unable to parse game_id: `{game_id}` to int for game: `{@game}`",
                        game.Id, game);
                    continue;
                }

                int? igdbId = null;
                if (!string.IsNullOrWhiteSpace(game.IgdbId))
                {
                    if (!int.TryParse(game.IgdbId, out var tempIgdbId))
                    {
                        _logger.LogError("Unable to parse igdb_id: `{igdb_id}` to int for game: `{@game}`",
                            game.IgdbId, game);
                        continue;
                    }

                    igdbId = tempIgdbId;
                }

                newGames.Add(new GameModel
                {
                    GameId = gameId,
                    Name = game.Name,
                    BoxArtUrl = game.BoxArtUrl,
                    IgdbId = igdbId
                });
            }

            context.Games.AddRange(newGames);
            var delta = await context.SaveChangesAsync(CancellationToken);
            if (delta != newGames.Count)
            {
                _logger.LogError(
                    "Could not add all new games. Found {new_games} but only added {added_games}. Rolling back",
                    newGames.Count, delta);
                await trans.RollbackAsync(CancellationToken);
                return [];
            }

            _logger.LogInformation("Added {new_games} games to the database", delta);
            await trans.CommitAsync(CancellationToken);
            return newGames;
        }

        private async Task<List<Game>> GetClipGames(List<int> gameIds)
        {
            var gamesResponse = await _api.Helix.Games.GetGamesAsync(gameIds.Select(x => x.ToString()).ToList());
            if (gamesResponse?.Data is null || gamesResponse.Data.Length <= 0)
                throw new UnknownTwitchGamesException(gameIds);

            var games = gamesResponse.Data!;
            var missingGames = gameIds.Select(x => x.ToString()).Except(games.Select(x => x.Id)).ToList();
            if (missingGames.Count > 0)
                _logger.LogWarning("Some games could not be queried from twitch. game_ids : `{@game_ids}`",
                    missingGames);

            return games.ToList();
        }

        private async Task<User> GetTwitchUser(int userId) => await GetTwitchUser(userId.ToString());

        private async Task<User> GetTwitchUser(string userId)
        {
            var usersResponse = await _api.Helix.Users.GetUsersAsync([userId]);
            if (usersResponse?.Users is null || usersResponse.Users.Length <= 0)
                throw new UnknownTwitchUserException(userId);

            return usersResponse.Users.First();
        }

        private async Task<ClipModel> GetCompleteClipModel(string clipId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(CancellationToken);
            var clip = await context.Clips
                .Include(x => x.Broadcaster)
                .Include(x => x.Creator)
                .Include(x => x.Game)
                .FirstOrDefaultAsync(x => x.ClipId == clipId, CancellationToken);

            return clip ?? throw new MissingClipException(clipId);
        }

        private static Embed BuildEmbed(ClipModel clip)
        {
            var embedAuthor = new EmbedAuthorBuilder()
                .WithName(clip.Creator!.Username)
                .WithIconUrl(clip.Creator.ProfileImageUrl)
                .WithUrl($"https://www.twitch.tv/{clip.Creator.Username}");

            var embedFooter = new EmbedFooterBuilder()
                .WithText("Made with ☕, ❤️ and 🤬")
                .WithIconUrl(clip.Broadcaster!.ProfileImageUrl);

            List<EmbedFieldBuilder> fields =
            [
                new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("Spiel")
                    .WithValue(clip.Game!.Name),
                new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("Ersteller/in")
                    .WithValue(clip.Creator!.Username),
                new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("Source")
                    .WithValue("[ℹ️](https://github.com/MitoG/ClipFunc)")
            ];

            return new EmbedBuilder
            {
                Title = clip.Title,
                Description = ":arrow_double_up: **___da klicken___** :arrow_double_up:",
                Url = clip.Url,
                //ThumbnailUrl = clip.ThumbnailUrl,
                ImageUrl = clip.ThumbnailUrl,
                Fields = fields,
                Timestamp = new DateTimeOffset(clip.ClipCreationDate),
                Author = embedAuthor,
                Footer = embedFooter,
            }.Build();
        }

        private async Task PostDiscordWebhook(ClipFuncChannel channel, string clipId)
        {
            var clip = await GetCompleteClipModel(clipId);
            var embed = BuildEmbed(clip);
            var discord = new DiscordWebhookClient(channel.DiscordWebhookUrl);

            _logger.LogInformation("Sending discord message for broadcaster `{broadcaster_name}` and clip `{clip_id}`",
                clip.Broadcaster!.Username, clip.ClipId);

            await discord.SendMessageAsync(
                embeds: [embed],
                username: channel.DiscordWebhookProfileName,
                avatarUrl: clip.Broadcaster!.ProfileImageUrl,
                options: RequestOptions.Default);
        }
    }
}