namespace Db4Net.Dialects;

internal sealed class SqlServerDialect : ISqlDialect
{
    public bool RenderOffsetBeforeLimit => true;

    public string QuoteIdentifier(string identifier)
    {
        return SqlIdentifier.QuoteParts(identifier, part => $"[{part}]");
    }

    public string RenderPaging(string limitParameterName, string? offsetParameterName)
    {
        if (offsetParameterName is null)
        {
            return $"FETCH NEXT @{limitParameterName} ROWS ONLY";
        }

        return $"OFFSET @{offsetParameterName} ROWS FETCH NEXT @{limitParameterName} ROWS ONLY";
    }
}
