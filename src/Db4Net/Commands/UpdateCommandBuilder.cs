using System.Data;
using System.Linq.Expressions;
using Db4Net.Metadata;
using Db4Net.Query;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Builds UPDATE statements for a mapped CLR model.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class UpdateCommandBuilder<T> : CommandBuilderBase
{
    private readonly UpdateCommandModel _model;
    private readonly FilterBuilder _filters;
    private readonly Db4NetOptions _options;

    internal UpdateCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table)
        : base(connection)
    {
        _options = options;
        _model = new UpdateCommandModel { Table = table };
        _filters = new FilterBuilder(_model.Filters);
    }

    /// <summary>
    /// Adds a SET assignment using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public UpdateCommandBuilder<T> Set(string propertyName, object? value)
    {
        _model.Assignments.Add(new AssignmentClause(MapPropertyName(propertyName), value));
        return this;
    }

    /// <summary>
    /// Adds a SET assignment using a typed member selector.
    /// </summary>
    public UpdateCommandBuilder<T> Set<TValue>(Expression<Func<T, TValue>> memberSelector, object? value)
    {
        _model.Assignments.Add(new AssignmentClause(ModelMetadataProvider.GetColumnName(memberSelector), value));
        return this;
    }

    /// <summary>
    /// Adds an AND filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public UpdateCommandBuilder<T> Where(string propertyName, Op op, object? value)
    {
        _filters.Add("AND", () => MapPropertyName(propertyName), op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public UpdateCommandBuilder<T> Where(string propertyName, Op op)
    {
        _filters.AddValueFree("AND", () => MapPropertyName(propertyName), op);
        return this;
    }

    /// <summary>
    /// Adds an AND filter using a typed member selector.
    /// </summary>
    public UpdateCommandBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        _filters.Add("AND", () => ModelMetadataProvider.GetColumnName(memberSelector), op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a typed member selector.
    /// </summary>
    public UpdateCommandBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _filters.AddValueFree("AND", () => ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public UpdateCommandBuilder<T> OrWhere(string propertyName, Op op, object? value)
    {
        _filters.Add("OR", () => MapPropertyName(propertyName), op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public UpdateCommandBuilder<T> OrWhere(string propertyName, Op op)
    {
        _filters.AddValueFree("OR", () => MapPropertyName(propertyName), op);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a typed member selector.
    /// </summary>
    public UpdateCommandBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        _filters.Add("OR", () => ModelMetadataProvider.GetColumnName(memberSelector), op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a typed member selector.
    /// </summary>
    public UpdateCommandBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _filters.AddValueFree("OR", () => ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Allows rendering an UPDATE statement without a WHERE clause.
    /// </summary>
    public UpdateCommandBuilder<T> AllowAllRows()
    {
        _model.AllowAllRows = true;
        return this;
    }

    /// <inheritdoc />
    public override SqlCommandDefinition ToCommand()
    {
        return new CommandSqlRenderer(_options.Dialect).Render(_model);
    }

    private static string MapPropertyName(string propertyName)
    {
        return ModelMetadata<T>.GetColumn(propertyName).ColumnName;
    }

}
