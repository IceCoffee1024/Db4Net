using System.Text.RegularExpressions;

namespace Db4Net.Dialects;

internal static class SqlIdentifier
{
    private static readonly Regex IdentifierRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled);

    public static string QuoteParts(string identifier, Func<string, string> quotePart)
    {
        Validate(identifier);
        return string.Join(".", identifier.Split('.').Select(quotePart));
    }

    private static void Validate(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !IdentifierRegex.IsMatch(identifier))
        {
            throw new ArgumentException($"Invalid SQL identifier: {identifier}", nameof(identifier));
        }
    }
}
