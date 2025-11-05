namespace ClipFunc.Exceptions;

internal class UnknownTwitchGamesException : Exception
{
    public List<int> UnknownGameIds { get; private init; }

    public UnknownTwitchGamesException(List<int> gameIds) : base(
        message: $"Unknown twitch games. {string.Join(',', gameIds)}')")
    {
        UnknownGameIds = gameIds;
    }
}