namespace Db4Net.Dialects;

internal sealed class MySqlDialect : ISqlDialect
{
    public bool RenderOffsetBeforeLimit => false;

    public string QuoteIdentifier(string identifier)
    {
        return SqlIdentifier.QuoteParts(identifier, part => $"`{part}`");
    }

    public string RenderPaging(string limitParameterName, string? offsetParameterName)
    {
        if (offsetParameterName is null)
        {
            return $"LIMIT @{limitParameterName}";
        }

        return $"LIMIT @{limitParameterName} OFFSET @{offsetParameterName}";
    }
}
