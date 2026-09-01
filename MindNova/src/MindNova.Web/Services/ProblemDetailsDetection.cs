namespace MindNova.Web.Services;

internal static class ProblemDetailsDetection
{
    // RFC 7807 members are lowercase on the wire (see ADR 0011), but older responses and hand-built
    // payloads may be PascalCase, so match either.
    public static bool IsProblemDetails(string content)
    {
        return content != null
            && content.Contains("\"status\"", StringComparison.OrdinalIgnoreCase)
            && content.Contains("\"title\"", StringComparison.OrdinalIgnoreCase);
    }
}
