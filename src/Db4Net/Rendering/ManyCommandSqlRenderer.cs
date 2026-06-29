using Dapper;
using Db4Net.Commands;
using Db4Net.Dialects;
using Db4Net.Metadata;

namespace Db4Net.Rendering;

internal sealed class ManyCommandSqlRenderer
{
    private readonly ISqlDialect _dialect;

    public ManyCommandSqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public string RenderInsertTemplate<T>(string table)
    {
        if (ModelMetadata<T>.InsertColumns.Count == 0)
        {
            throw new InvalidOperationException("INSERT requires at least one value.");
        }

        var columns = string.Join(", ", ModelMetadata<T>.InsertColumns.Select(column => _dialect.QuoteIdentifier(column.ColumnName)));
        var parameters = string.Join(", ", ModelMetadata<T>.InsertColumns.Select(column => $"@{column.PropertyName}"));
        return $"INSERT INTO {_dialect.QuoteIdentifier(table)} ({columns}) VALUES ({parameters})";
    }

    public string RenderUpdateTemplate<T>(string table)
    {
        if (ModelMetadata<T>.UpdateColumns.Count == 0)
        {
            throw new InvalidOperationException("UPDATE requires at least one SET assignment.");
        }

        var keyColumn = ManyCommandBuilderSupport<T>.RequireSingleKeyColumn();
        var assignments = string.Join(", ", ModelMetadata<T>.UpdateColumns.Select(column => $"{_dialect.QuoteIdentifier(column.ColumnName)} = @{column.PropertyName}"));
        return $"UPDATE {_dialect.QuoteIdentifier(table)} SET {assignments} WHERE {_dialect.QuoteIdentifier(keyColumn.ColumnName)} = @{keyColumn.PropertyName}";
    }

    public string RenderDeleteTemplate<T>(string table)
    {
        var keyColumn = ManyCommandBuilderSupport<T>.RequireSingleKeyColumn();
        return $"DELETE FROM {_dialect.QuoteIdentifier(table)} WHERE {_dialect.QuoteIdentifier(keyColumn.ColumnName)} = @{keyColumn.PropertyName}";
    }

    public RenderedSqlCommand RenderInsert<T>(string table, T entity)
    {
        var parameters = new DynamicParameters();
        foreach (var column in ModelMetadata<T>.InsertColumns)
        {
            parameters.Add(column.PropertyName, column.GetValue(entity!));
        }

        return new RenderedSqlCommand(RenderInsertTemplate<T>(table), parameters);
    }

    public RenderedSqlCommand RenderUpdate<T>(string table, T entity)
    {
        var parameters = new DynamicParameters();
        foreach (var column in ModelMetadata<T>.UpdateColumns)
        {
            parameters.Add(column.PropertyName, column.GetValue(entity!));
        }

        var keyColumn = ManyCommandBuilderSupport<T>.RequireSingleKeyColumn();
        parameters.Add(keyColumn.PropertyName, keyColumn.GetValue(entity!));

        return new RenderedSqlCommand(RenderUpdateTemplate<T>(table), parameters);
    }

    public RenderedSqlCommand RenderDelete<T>(string table, T entity)
    {
        var parameters = new DynamicParameters();
        var keyColumn = ManyCommandBuilderSupport<T>.RequireSingleKeyColumn();
        parameters.Add(keyColumn.PropertyName, keyColumn.GetValue(entity!));

        return new RenderedSqlCommand(RenderDeleteTemplate<T>(table), parameters);
    }
}
