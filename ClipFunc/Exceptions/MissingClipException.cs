namespace ClipFunc.Exceptions;

internal class MissingClipException : Exception
{
    public readonly string MissingClipId;

    public MissingClipException(string clipId) : base($"Clip `{clipId}` is missing but should be present.")
    {
        MissingClipId = clipId;
    }
}