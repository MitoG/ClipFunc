using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ClipFunc.Configuration;
using ClipFunc.Exceptions;

namespace ClipFunc.Validation;

public static partial class ChannelConfigurationValidator
{
    [GeneratedRegex(@"^https://discord\.com/api/webhooks/\d*/[A-Za-z_0-9-]*$")]
    private static partial Regex DiscordWebhookRegex();

    [GeneratedRegex(@"^\d{1,30}$")]
    private static partial Regex BroadcasterIdRegex();

    public static void ThrowIfNotValid([NotNull] string? broadcasterId,
        [NotNull] string? webhookUrl,
        [NotNull] string? webhookProfileName)
    {
        InvalidChannelConfigurationException.ThrowIfNullOrWhiteSpace(webhookProfileName,
            ConfigurationKeys.DiscordWebhookProfileName);

        InvalidChannelConfigurationException.ThrowIfNullOrWhiteSpace(broadcasterId,
            ConfigurationKeys.BroadcasterId);

        InvalidChannelConfigurationException.ThrowIfNullOrWhiteSpace(webhookUrl,
            ConfigurationKeys.DiscordWebhookUrl);

        if (!DiscordWebhookRegex().IsMatch(webhookUrl))
            throw new InvalidChannelConfigurationException<string>(webhookUrl,
                ConfigurationKeys.DiscordWebhookUrl,
                "Must be a valid discord webhook url");

        if (!BroadcasterIdRegex().IsMatch(broadcasterId))
            throw new InvalidChannelConfigurationException<string>(broadcasterId,
                ConfigurationKeys.BroadcasterId,
                "Must be a valid twitch user id");
    }
}