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

    public string RenderInsert(
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        string? returnKeyColumnName = null,
        string? returnKeyParameterName = null,
        bool returnKeyIsIdentity = false)
    {
        var columns = string.Join(", ", insertColumnNames.Select(QuoteIdentifier));
        var parameters = string.Join(", ", parameterNames.Select(parameterName => $"@{parameterName}"));
        var output = returnKeyColumnName is null
            ? string.Empty
            : $" OUTPUT INSERTED.{QuoteIdentifier(returnKeyColumnName)}";

        return $"INSERT INTO {QuoteIdentifier(table)} ({columns}){output} VALUES ({parameters})";
    }
}
