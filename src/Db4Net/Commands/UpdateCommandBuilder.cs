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
    private readonly FilterClauseBuilder _filters;
    private readonly Db4NetOptions _options;

    internal UpdateCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table, Db4NetExecutionOptions? executionOptions = null)
        : base(connection, executionOptions)
    {
        _options = options;
        _model = new UpdateCommandModel { Table = table };
        _filters = new FilterClauseBuilder(_model.Filters);
    }

    /// <summary>
    /// Adds a SET assignment using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The mapped CLR property name to update.</param>
    /// <param name="value">The value to pass as a Dapper parameter.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> Set(string propertyName, object? value)
    {
        _model.Assignments.Add(new AssignmentClause(MapPropertyName(propertyName), value));
        return this;
    }

    /// <summary>
    /// Adds a SET assignment using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Name</c>.</param>
    /// <param name="value">The value to pass as a Dapper parameter.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> Set<TValue>(Expression<Func<T, TValue>> memberSelector, object? value)
    {
        _model.Assignments.Add(new AssignmentClause(ModelMetadataProvider.GetColumnName(memberSelector), value));
        return this;
    }

    internal UpdateCommandBuilder<T> SetEntityValues(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);

        foreach (var column in ModelMetadata<T>.UpdateColumns)
        {
            _model.Assignments.Add(new AssignmentClause(column.ColumnName, column.GetValue(entity)));
        }

        return this;
    }

    /// <summary>
    /// Adds an AND filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The mapped CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to pass as a Dapper parameter. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> Where(string propertyName, Op op, object? value)
    {
        _filters.Add(FilterBooleanOperator.And, () => MapPropertyName(propertyName), op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The mapped CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> Where(string propertyName, Op op)
    {
        _filters.AddValueFree(FilterBooleanOperator.And, () => MapPropertyName(propertyName), op);
        return this;
    }

    /// <summary>
    /// Adds an AND filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to pass as a Dapper parameter. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        _filters.Add(FilterBooleanOperator.And, () => ModelMetadataProvider.GetColumnName(memberSelector), op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.DeletedAt</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _filters.AddValueFree(FilterBooleanOperator.And, () => ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> WhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        AddGroup(FilterBooleanOperator.And, configure);
        return this;
    }

    /// <summary>
    /// Adds AND filters for the key properties of an entity instance.
    /// </summary>
    /// <param name="entity">The entity instance to read key values from.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> WhereKey(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);

        foreach (var column in ModelMetadata<T>.RequireKeyColumns())
        {
            var value = column.GetValue(entity);
            EnsureNonDefaultKeyValue(column, value);
            _filters.Add(FilterBooleanOperator.And, () => column.ColumnName, Op.Eq, value);
        }

        return this;
    }

    /// <summary>
    /// Adds an OR filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The mapped CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to pass as a Dapper parameter. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> OrWhere(string propertyName, Op op, object? value)
    {
        _filters.Add(FilterBooleanOperator.Or, () => MapPropertyName(propertyName), op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The mapped CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> OrWhere(string propertyName, Op op)
    {
        _filters.AddValueFree(FilterBooleanOperator.Or, () => MapPropertyName(propertyName), op);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to pass as a Dapper parameter. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        _filters.Add(FilterBooleanOperator.Or, () => ModelMetadataProvider.GetColumnName(memberSelector), op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.DeletedAt</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _filters.AddValueFree(FilterBooleanOperator.Or, () => ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized OR filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> OrWhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        AddGroup(FilterBooleanOperator.Or, configure);
        return this;
    }

    /// <summary>
    /// Allows rendering an UPDATE statement without a WHERE clause.
    /// </summary>
    /// <returns>The current command builder.</returns>
    public UpdateCommandBuilder<T> AllowAllRows()
    {
        _model.AllowAllRows = true;
        return this;
    }

    /// <inheritdoc />
    public override RenderedSqlCommand ToCommand()
    {
        return new CommandSqlRenderer(_options.Dialect).Render(_model);
    }

    private static string MapPropertyName(string propertyName)
    {
        return ModelMetadata<T>.GetColumn(propertyName).ColumnName;
    }

    private void AddGroup(FilterBooleanOperator booleanOperator, Action<FilterGroupBuilder<T>> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        var group = new FilterGroupBuilder<T>();
        configure(group);
        _filters.AddGroup(booleanOperator, group.Filters);
    }

    private static void EnsureNonDefaultKeyValue(ColumnMetadata column, object? value)
    {
        if (value is null || value.Equals(GetDefaultValue(column.Property.PropertyType)))
        {
            throw new InvalidOperationException($"Key '{typeof(T).Name}.{column.PropertyName}' has the default key value.");
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

}
