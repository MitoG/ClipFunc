using ClipFunc.Validation;

namespace ClipFunc.Configuration;

public sealed record TwitchCredentials
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    public TwitchCredentials(string? clientId, string? clientSecret)
    {
        TwitchCredentialsValidator.ThrowIfInvalid(clientId, clientSecret);
        ClientId = clientId;
        ClientSecret = clientSecret;
    }
}