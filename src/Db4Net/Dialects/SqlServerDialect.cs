using Db4Net.Commands;
using Db4Net.Metadata;

namespace Db4Net.Dialects;

internal sealed class SqlServerDialect : ISqlDialect
{
    public bool RenderOffsetBeforeLimit => true;

    public bool RequiresOrderByForPaging => true;

    public string QuoteIdentifier(string identifier)
    {
        return SqlIdentifier.QuoteParts(identifier, part => $"[{part}]");
    }

    public string RenderPaging(string limitParameterName, string? offsetParameterName)
    {
        var offset = offsetParameterName is null ? "0" : $"@{offsetParameterName}";
        return $"OFFSET {offset} ROWS FETCH NEXT @{limitParameterName} ROWS ONLY";
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

    public string RenderConflictInsert(
        ConflictInsertBehavior behavior,
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        var quotedInsertColumns = insertColumnNames.Select(QuoteIdentifier).ToArray();
        var values = string.Join(", ", parameterNames.Select(p => $"@{p}"));
        var sourceColumns = string.Join(", ", quotedInsertColumns);
        var on = string.Join(" AND ", conflictColumns.Select(c => $"target.{QuoteIdentifier(c.ColumnName)} = source.{QuoteIdentifier(c.ColumnName)}"));
        var insertCols = string.Join(", ", quotedInsertColumns);
        var insertVals = string.Join(", ", insertColumnNames.Select(c => $"source.{QuoteIdentifier(c)}"));
        var merge = $"MERGE INTO {QuoteIdentifier(table)} WITH (HOLDLOCK) AS target USING (VALUES ({values})) AS source ({sourceColumns}) ON {on}";

        if (behavior == ConflictInsertBehavior.Update)
        {
            var updates = string.Join(", ", updateColumns.Select(c => $"{QuoteIdentifier(c.ColumnName)} = source.{QuoteIdentifier(c.ColumnName)}"));
            merge += $" WHEN MATCHED THEN UPDATE SET {updates}";
        }

        return $"{merge} WHEN NOT MATCHED THEN INSERT ({insertCols}) VALUES ({insertVals});";
    }
}
