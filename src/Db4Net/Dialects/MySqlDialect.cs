using Db4Net.Commands;
using Db4Net.Metadata;

namespace Db4Net.Dialects;

internal sealed class MySqlDialect : ISqlDialect
{
    public bool RenderOffsetBeforeLimit => false;

    public bool RequiresOrderByForPaging => false;

    public string QuoteIdentifier(string identifier)
    {
        return SqlIdentifier.QuoteParts(identifier, part => $"`{part}`");
    }

    public string RenderPaging(string limitParameterName, string? offsetParameterName)
    {
        return offsetParameterName is null
            ? $"LIMIT @{limitParameterName}"
            : $"LIMIT @{limitParameterName} OFFSET @{offsetParameterName}";
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
        var parameters = string.Join(", ", parameterNames.Select(p => $"@{p}"));
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

        // Requires AllowBatch=True (MySqlConnector) or Allow Batch Statements=True (MySql.Data) in the connection string.
        return $"{sql}; SELECT LAST_INSERT_ID()";
    }

    public string RenderConflictInsert(
        ConflictInsertBehavior behavior,
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        var columns = string.Join(", ", insertColumnNames.Select(QuoteIdentifier));
        var parameters = string.Join(", ", parameterNames.Select(p => $"@{p}"));

        if (behavior == ConflictInsertBehavior.Ignore)
        {
            return $"INSERT IGNORE INTO {QuoteIdentifier(table)} ({columns}) VALUES ({parameters})";
        }

        // Row alias syntax (AS _new) requires MySQL >= 8.0.19 or MySQL 8.0.19+.
        // MySQL 5.7, MySQL 8.0.0–8.0.18, and all MariaDB versions will throw a syntax error.
        // If you need compatibility with these versions, use InsertOrIgnore instead,
        // or perform the upsert with a raw Dapper query using the VALUES(col_name) function.
        var insert = $"INSERT INTO {QuoteIdentifier(table)} ({columns}) VALUES ({parameters}) AS _new";
        var updates = string.Join(", ", updateColumns.Select(c => $"{QuoteIdentifier(c.ColumnName)} = _new.{QuoteIdentifier(c.ColumnName)}"));
        return $"{insert} ON DUPLICATE KEY UPDATE {updates}";
    }
}
