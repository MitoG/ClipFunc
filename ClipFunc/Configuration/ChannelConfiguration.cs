namespace ClipFunc.Configuration;

public record ChannelConfiguration
{
    public string BroadcasterId { get; init; }
    public string DiscordWebhookProfileName { get; init; }
    public string DiscordWebhookUrl { get; init; }
    public bool PreventWebhookOnFirstLoad { get; init; }

    public ChannelConfiguration(
        string? broadcasterId,
        string? discordWebhookUrl,
        string? discordWebhookProfileName,
        bool preventWebhookOnFirstLoad = true)
    {
        Validation.ChannelConfigurationValidator.ThrowIfNotValid(broadcasterId, discordWebhookUrl,
            discordWebhookProfileName);
        BroadcasterId = broadcasterId;
        DiscordWebhookUrl = discordWebhookUrl;
        DiscordWebhookProfileName = discordWebhookProfileName;
        PreventWebhookOnFirstLoad = preventWebhookOnFirstLoad;
    }
}