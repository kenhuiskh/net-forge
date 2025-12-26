namespace NetForge.Core.Utils;

public static class ResponseParser
{
    public static string? ExtractCsvFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var trimmed = response.Trim();

        const string fence = "```";
        if (trimmed.StartsWith(fence, StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            if (firstNewLine >= 0)
            {
                trimmed = trimmed[(firstNewLine + 1)..];
            }
        }

        if (trimmed.EndsWith(fence, StringComparison.Ordinal))
        {
            trimmed = trimmed[..^fence.Length];
        }

        trimmed = trimmed.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}