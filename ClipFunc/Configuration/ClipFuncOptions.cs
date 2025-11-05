using System.ComponentModel.DataAnnotations;

namespace ClipFunc.Configuration;

public sealed class ClipFuncOptions
{
    [Required] public required TwitchCredentials TwitchCredentials { get; set; }

    [Required, Length(minimumLength: 1, maximumLength: 10)]
    public required List<ClipFuncChannel> WatchedChannels { get; set; } = [];
}

public sealed class ClipFuncChannel
{
    [Required(AllowEmptyStrings = false)]
    [Range(0d, double.MaxValue, MinimumIsExclusive = true, MaximumIsExclusive = true,
        ErrorMessage = "The BroadcasterId must be greater than 0.")]
    public required int BroadcasterId { get; set; }
    
    [Required(AllowEmptyStrings = false), StringLength(60, MinimumLength = 3)]
    public required string DiscordWebhookProfileName { get; set; }

    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^https:\/\/discord\.com\/api\/webhooks\/\d*\/[A-Za-z_0-9-]*$",
        ErrorMessage = "The webhook URL is invalid.")]
    public required string DiscordWebhookUrl { get; set; }
}