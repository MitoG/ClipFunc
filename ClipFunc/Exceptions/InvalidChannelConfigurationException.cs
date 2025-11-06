using System.Diagnostics.CodeAnalysis;

namespace ClipFunc.Exceptions;

internal class InvalidChannelConfigurationException : Exception
{
    protected InvalidChannelConfigurationException(string message) : base(message)
    {
    }

    public static void ThrowIfNullOrWhiteSpace([NotNull] string? value, string configurationName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return;

        throw new InvalidChannelConfigurationException<string?>(value, configurationName, "Must not be empty");
    }

    public static void ThrowIfNotBetween(ulong value, string configurationName, ulong exclusiveMin, ulong exclusiveMax)
    {
        if (value > exclusiveMin && value < exclusiveMax)
            return;

        throw new InvalidChannelConfigurationException<ulong>(value, configurationName,
            $"Must be between {exclusiveMin} and {exclusiveMax}");
    }
}

internal class InvalidChannelConfigurationException<T> : InvalidChannelConfigurationException
{
    public T Value { get; }
    public string ValidationError { get; }
    public string ConfigurationKey { get; }

    public InvalidChannelConfigurationException(T value, string configurationKey, string validationError)
        : base($"Channel validation failed for `{configurationKey}` with error: `{validationError}`")
    {
        Value = value;
        ValidationError = validationError;
        ConfigurationKey = configurationKey;
    }
}