using System.Diagnostics.CodeAnalysis;

namespace ClipFunc.Exceptions;

internal class InvalidTwitchCredentialsException : Exception
{
    public string? Value { get; }
    public string ValidationError { get; }
    public string ConfigurationName { get; }

    private InvalidTwitchCredentialsException(string? value, string configurationKey, string validationError)
        : base($"Channel validation failed for `{configurationKey}` with error: `{validationError}`")
    {
        Value = value;
        ValidationError = validationError;
        ConfigurationName = configurationKey;
    }

    public static void RegexFailed(string? value, string configurationKey)
    {
        throw new InvalidTwitchCredentialsException(value, configurationKey,
            "Must be exactly 30 lowercase alpha-numeric characters");
    }

    public static void ThrowIfNullOrWhiteSpace([NotNull] string? value, string configurationName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return;

        throw new InvalidTwitchCredentialsException(value, configurationName, "Must not be empty");
    }
}