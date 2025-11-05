namespace ClipFunc.Exceptions;

internal class UnknownTwitchUserException : Exception
{
    public readonly string UnknownTwitchUserId;

    public UnknownTwitchUserException(string userId)
        : base($"User with ID: {userId} could not be found")
    {
        UnknownTwitchUserId = userId;
    }
}