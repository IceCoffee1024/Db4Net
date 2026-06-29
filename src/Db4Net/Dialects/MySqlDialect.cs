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

        if (returnKeyColumnName is null)
        {
            return sql;
        }

        if (returnKeyParameterName is not null)
        {
            return $"{sql}; SELECT @{returnKeyParameterName}";
        }

        if (!returnKeyIsIdentity)
        {
            throw new NotSupportedException("MySQL insert key return requires an auto-increment identity key or an explicitly inserted key value.");
        }

        return $"{sql}; SELECT LAST_INSERT_ID()";
    }
}
