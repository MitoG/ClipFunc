using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ClipFunc.Configuration;
using ClipFunc.Exceptions;

namespace ClipFunc.Validation;

public static partial class TwitchCredentialsValidator
{
    [GeneratedRegex("^[a-z0-9]{30}$")]
    private static partial Regex CredentialRegex();

    public static void ThrowIfInvalid([NotNull] string? clientId, [NotNull] string? clientSecret)
    {
        InvalidTwitchCredentialsException.ThrowIfNullOrWhiteSpace(clientId,
            ConfigurationKeys.TwitchClientId);
        
        InvalidTwitchCredentialsException.ThrowIfNullOrWhiteSpace(clientSecret,
            ConfigurationKeys.TwitchClientSecret);

        if (!CredentialRegex().IsMatch(clientId))
            InvalidTwitchCredentialsException.RegexFailed(clientId,
                ConfigurationKeys.TwitchClientId);

        if (!CredentialRegex().IsMatch(clientSecret))
            InvalidTwitchCredentialsException.RegexFailed(clientSecret,
                ConfigurationKeys.TwitchClientSecret);
    }
}