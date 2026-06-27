using Db4Net.Commands;
using Db4Net.Dialects;
using Db4Net.Metadata;

namespace Db4Net.Rendering;

internal sealed class ConflictInsertSqlRenderer
{
    private readonly ISqlDialect _dialect;

    public ConflictInsertSqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public RenderedSqlCommand Render(
        string table,
        ConflictInsertBehavior behavior,
        IReadOnlyList<AssignmentClause> values,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        var parameterWriter = new SqlParameterWriter();
        var parameterNames = values.Select(value => parameterWriter.Add(value.Value)).ToArray();
        var sql = RenderSql(
            table,
            behavior,
            values.Select(value => value.Column).ToArray(),
            parameterNames,
            conflictColumns,
            updateColumns);

        return new RenderedSqlCommand(sql, parameterWriter.Parameters);
    }

    public string RenderTemplate(
        string table,
        ConflictInsertBehavior behavior,
        IReadOnlyList<ColumnMetadata> insertColumns,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        return RenderSql(
            table,
            behavior,
            insertColumns.Select(column => column.ColumnName).ToArray(),
            insertColumns.Select(column => column.PropertyName).ToArray(),
            conflictColumns,
            updateColumns);
    }

    public RenderedSqlCommand RenderTemplateCommand(
        string table,
        ConflictInsertBehavior behavior,
        IReadOnlyList<ColumnMetadata> insertColumns,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns,
        object entity)
    {
        var parameters = new Dapper.DynamicParameters();
        foreach (var column in insertColumns)
        {
            parameters.Add(column.PropertyName, column.GetValue(entity));
        }

        return new RenderedSqlCommand(
            RenderTemplate(table, behavior, insertColumns, conflictColumns, updateColumns),
            parameters);
    }

    private string RenderSql(
        string table,
        ConflictInsertBehavior behavior,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        if (insertColumnNames.Count == 0)
        {
            throw new InvalidOperationException("INSERT requires at least one value.");
        }

        return _dialect switch
        {
            SqlServerDialect => RenderSqlServer(table, behavior, insertColumnNames, parameterNames, conflictColumns, updateColumns),
            MySqlDialect => RenderMySql(table, behavior, insertColumnNames, parameterNames, updateColumns),
            _ => RenderOnConflict(table, behavior, insertColumnNames, parameterNames, conflictColumns, updateColumns),
        };
    }

    private string RenderOnConflict(
        string table,
        ConflictInsertBehavior behavior,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        var insert = RenderInsertPrefix(table, insertColumnNames, parameterNames);
        var conflictTarget = string.Join(", ", conflictColumns.Select(column => _dialect.QuoteIdentifier(column.ColumnName)));

        if (behavior == ConflictInsertBehavior.Ignore)
        {
            return $"{insert} ON CONFLICT ({conflictTarget}) DO NOTHING";
        }

        var updates = string.Join(", ", updateColumns.Select(column => $"{_dialect.QuoteIdentifier(column.ColumnName)} = excluded.{_dialect.QuoteIdentifier(column.ColumnName)}"));
        return $"{insert} ON CONFLICT ({conflictTarget}) DO UPDATE SET {updates}";
    }

    private string RenderMySql(
        string table,
        ConflictInsertBehavior behavior,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        var insert = RenderInsertPrefix(table, insertColumnNames, parameterNames);
        if (behavior == ConflictInsertBehavior.Ignore)
        {
            var noOpColumn = _dialect.QuoteIdentifier(insertColumnNames[0]);
            return $"{insert} ON DUPLICATE KEY UPDATE {noOpColumn} = {noOpColumn}";
        }

        var updates = string.Join(", ", updateColumns.Select(column => $"{_dialect.QuoteIdentifier(column.ColumnName)} = VALUES({_dialect.QuoteIdentifier(column.ColumnName)})"));
        return $"{insert} ON DUPLICATE KEY UPDATE {updates}";
    }

    private string RenderSqlServer(
        string table,
        ConflictInsertBehavior behavior,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns)
    {
        var quotedInsertColumns = insertColumnNames.Select(_dialect.QuoteIdentifier).ToArray();
        var values = string.Join(", ", parameterNames.Select(parameterName => $"@{parameterName}"));
        var sourceColumns = string.Join(", ", quotedInsertColumns);
        var on = string.Join(" AND ", conflictColumns.Select(column => $"target.{_dialect.QuoteIdentifier(column.ColumnName)} = source.{_dialect.QuoteIdentifier(column.ColumnName)}"));
        var insertColumns = string.Join(", ", quotedInsertColumns);
        var insertValues = string.Join(", ", insertColumnNames.Select(column => $"source.{_dialect.QuoteIdentifier(column)}"));
        var insert = $"MERGE INTO {_dialect.QuoteIdentifier(table)} WITH (HOLDLOCK) AS target USING (VALUES ({values})) AS source ({sourceColumns}) ON {on}";

        if (behavior == ConflictInsertBehavior.Update)
        {
            var updates = string.Join(", ", updateColumns.Select(column => $"{_dialect.QuoteIdentifier(column.ColumnName)} = source.{_dialect.QuoteIdentifier(column.ColumnName)}"));
            insert += $" WHEN MATCHED THEN UPDATE SET {updates}";
        }

        return $"{insert} WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({insertValues});";
    }

    private string RenderInsertPrefix(
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames)
    {
        var columns = string.Join(", ", insertColumnNames.Select(_dialect.QuoteIdentifier));
        var parameters = string.Join(", ", parameterNames.Select(parameterName => $"@{parameterName}"));
        return $"INSERT INTO {_dialect.QuoteIdentifier(table)} ({columns}) VALUES ({parameters})";
    }
}
