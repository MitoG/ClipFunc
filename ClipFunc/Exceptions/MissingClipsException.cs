namespace ClipFunc.Exceptions;

internal class MissingClipsException : Exception
{
    public readonly List<string> MissingClipIds;

    public MissingClipsException(List<string> missingClipIds) : base($"Missing clips for IDs: `[{string.Join(',', missingClipIds)}]`")
    {
        MissingClipIds = missingClipIds;
    }
}