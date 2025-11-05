namespace ClipFunc.Exceptions;

public class MissingConfigurationException : Exception
{
    public string ConfigurationName { get; private init; }
    private const string DefaultMessage = "Missing Configuration";

    public MissingConfigurationException(
        string configurationName,
        string? message = null,
        Exception? innerException = null)
        : base(message ?? DefaultMessage, innerException)
    {
        ConfigurationName = configurationName;
    }
}