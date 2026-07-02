using Db4Net.Commands;
using Db4Net.Metadata;

namespace Db4Net.Dialects;

internal sealed class PostgreSqlDialect : AnsiSqlDialectBase
{
    public override bool RenderOffsetBeforeLimit => false;

    public override string RenderPaging(string limitParameterName, string? offsetParameterName)
    {
        return offsetParameterName is null
            ? $"LIMIT @{limitParameterName}"
            : $"LIMIT @{limitParameterName} OFFSET @{offsetParameterName}";
    }

    public override string RenderInsert(
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

        return returnKeyColumnName is null
            ? sql
            : $"{sql} RETURNING {QuoteIdentifier(returnKeyColumnName)}";
    }

    public override string RenderConflictInsert(
        ConflictInsertBehavior behavior,
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        var columns = string.Join(", ", insertColumnNames.Select(QuoteIdentifier));
        var parameters = string.Join(", ", parameterNames.Select(p => $"@{p}"));
        var insert = $"INSERT INTO {QuoteIdentifier(table)} ({columns}) VALUES ({parameters})";
        var conflictTarget = string.Join(", ", conflictColumns.Select(c => QuoteIdentifier(c.ColumnName)));

        if (behavior == ConflictInsertBehavior.Ignore)
        {
            return $"{insert} ON CONFLICT ({conflictTarget}) DO NOTHING";
        }

        var updates = string.Join(", ", updateColumns.Select(c => $"{QuoteIdentifier(c.ColumnName)} = excluded.{QuoteIdentifier(c.ColumnName)}"));
        return $"{insert} ON CONFLICT ({conflictTarget}) DO UPDATE SET {updates}";
    }
}
