namespace LinkTracker.AiAgent.Services;

public static class AiSummarize
{
    private static readonly int _textLength = 100;

    public static string SummarizeText(this string text)
    {
        return "Summary: " + text[.._textLength] + "...";
    }
}
