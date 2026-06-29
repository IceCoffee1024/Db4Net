namespace Db4Net.Dialects;

internal sealed class PostgreSqlDialect : ISqlDialect
{
    public bool RenderOffsetBeforeLimit => false;

    public string QuoteIdentifier(string identifier)
    {
        return SqlIdentifier.QuoteParts(identifier, part => $"""
            "{part}"
            """.Trim());
    }

    public string RenderPaging(string limitParameterName, string? offsetParameterName)
    {
        if (offsetParameterName is null)
        {
            return $"LIMIT @{limitParameterName}";
        }

        return $"LIMIT @{limitParameterName} OFFSET @{offsetParameterName}";
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
        var sql = $"INSERT INTO {QuoteIdentifier(table)} ({columns}) VALUES ({parameters})";

        return returnKeyColumnName is null
            ? sql
            : $"{sql} RETURNING {QuoteIdentifier(returnKeyColumnName)}";
    }
}
