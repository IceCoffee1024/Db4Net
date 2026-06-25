using System.Text.RegularExpressions;

namespace Db4Net.Dialects;

internal sealed partial class SqlServerDialect : ISqlDialect
{
    public bool RenderOffsetBeforeLimit => true;

    public string QuoteIdentifier(string identifier)
    {
        ValidateIdentifier(identifier);
        return string.Join(".", identifier.Split('.').Select(part => $"[{part}]"));
    }

    public string RenderPaging(string limitParameterName, string? offsetParameterName)
    {
        if (offsetParameterName is null)
        {
            return $"FETCH NEXT @{limitParameterName} ROWS ONLY";
        }

        return $"OFFSET @{offsetParameterName} ROWS FETCH NEXT @{limitParameterName} ROWS ONLY";
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
