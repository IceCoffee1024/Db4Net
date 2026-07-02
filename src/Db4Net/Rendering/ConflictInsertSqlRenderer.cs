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
        var sql = _dialect.RenderConflictInsert(
            behavior,
            table,
            values.Select(v => v.Column).ToArray(),
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
        return _dialect.RenderConflictInsert(
            behavior,
            table,
            insertColumns.Select(c => c.ColumnName).ToArray(),
            insertColumns.Select(c => c.PropertyName).ToArray(),
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
}
