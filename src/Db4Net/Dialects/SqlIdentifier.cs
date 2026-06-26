using System.Text.RegularExpressions;

namespace Db4Net.Dialects;

internal static partial class SqlIdentifier
{
    public static string QuoteParts(string identifier, Func<string, string> quotePart)
    {
        Validate(identifier);
        return string.Join(".", identifier.Split('.').Select(quotePart));
    }

    private static void Validate(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !IdentifierRegex().IsMatch(identifier))
        {
            throw new ArgumentException($"Invalid SQL identifier: {identifier}", nameof(identifier));
        }
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex IdentifierRegex();
}
