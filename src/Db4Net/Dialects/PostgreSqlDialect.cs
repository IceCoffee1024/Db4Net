using System.Text.RegularExpressions;

namespace Db4Net.Dialects;

internal sealed partial class PostgreSqlDialect : ISqlDialect
{
    public bool RenderOffsetBeforeLimit => false;

    public string QuoteIdentifier(string identifier)
    {
        ValidateIdentifier(identifier);
        return string.Join(".", identifier.Split('.').Select(part => $"""
            "{part}"
            """.Trim()));
    }

    public string RenderPaging(string limitParameterName, string? offsetParameterName)
    {
        if (offsetParameterName is null)
        {
            return $"LIMIT @{limitParameterName}";
        }

        return $"LIMIT @{limitParameterName} OFFSET @{offsetParameterName}";
    }

    private static void ValidateIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !IdentifierRegex().IsMatch(identifier))
        {
            throw new ArgumentException($"Invalid SQL identifier: {identifier}", nameof(identifier));
        }
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex IdentifierRegex();
}
