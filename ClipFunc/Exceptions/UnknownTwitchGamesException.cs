namespace ClipFunc.Exceptions;

internal class UnknownTwitchGamesException : Exception
{
    public List<string> UnknownGameIds { get; private init; }

    public UnknownTwitchGamesException(List<string> gameIds) : base(
        message: $"Unknown twitch games: [{string.Join(',', gameIds)}]')")
    {
        UnknownGameIds = gameIds;
    }
}