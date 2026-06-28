using System.Data;
using System.Linq.Expressions;
using System.Threading;
using Dapper;
using Db4Net.Metadata;
using Db4Net.Rendering;

namespace Db4Net.Query;

/// <summary>
/// Builds SELECT EXISTS statements using typed member selectors for filtering.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class SelectExistsQueryBuilder<T>
{
    private readonly IDbConnection? _connection;
    private readonly Db4NetExecutionOptions? _executionOptions;
    private readonly FilterClauseBuilder _filters;
    private readonly SelectExistsQueryModel _model;
    private readonly Db4NetOptions _options;

    internal SelectExistsQueryBuilder(Db4NetOptions options, IDbConnection? connection, string table, Db4NetExecutionOptions? executionOptions = null)
    {
        _options = options;
        _connection = connection;
        _executionOptions = executionOptions;
        _model = new SelectExistsQueryModel { Table = table };
        _filters = new FilterClauseBuilder(_model.Filters);
    }

    /// <summary>
    /// Adds an AND filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> Where(string propertyName, Op op, object? value)
    {
        _filters.Add(FilterBooleanOperator.And, () => MapPropertyName(propertyName), op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> Where(string propertyName, Op op)
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
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
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
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _filters.AddValueFree(FilterBooleanOperator.And, () => ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> WhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        AddGroup(FilterBooleanOperator.And, configure);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> OrWhere(string propertyName, Op op, object? value)
    {
        _filters.Add(FilterBooleanOperator.Or, () => MapPropertyName(propertyName), op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> OrWhere(string propertyName, Op op)
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
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
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
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _filters.AddValueFree(FilterBooleanOperator.Or, () => ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized OR filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectExistsQueryBuilder<T> OrWhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        AddGroup(FilterBooleanOperator.Or, configure);
        return this;
    }

    /// <summary>
    /// Renders the SQL text and parameters without executing the query.
    /// </summary>
    /// <returns>The rendered SQL command definition.</returns>
    public RenderedSqlCommand ToCommand()
    {
        return new SelectExistsSqlRenderer(_options.Dialect).Render(_model);
    }

    /// <summary>
    /// Executes the existence query through Dapper.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns><see langword="true"/> when at least one row matches; otherwise, <see langword="false"/>.</returns>
    public bool Execute(Db4NetExecutionOptions? options = null)
    {
        return RequireConnection().ExecuteScalar<int>(CreateDapperCommand(options)) != 0;
    }

    /// <summary>
    /// Asynchronously executes the existence query through Dapper.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns><see langword="true"/> when at least one row matches; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ExecuteAsync(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await RequireConnection().ExecuteScalarAsync<int>(CreateDapperCommand(options, cancellationToken)).ConfigureAwait(false) != 0;
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

    private CommandDefinition CreateDapperCommand(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var command = ToCommand();
        var executionOptions = Db4NetExecutionOptions.Merge(_executionOptions, options);
        executionOptions?.Validate();
        return new CommandDefinition(
            command.Sql,
            command.Parameters,
            executionOptions?.Transaction,
            executionOptions?.CommandTimeout,
            executionOptions?.CommandType,
            cancellationToken: cancellationToken);
    }

    private IDbConnection RequireConnection()
    {
        return _connection ?? throw new InvalidOperationException("Dapper execution requires an IDbConnection. Use connection.UseDb4Net(options) to create the database facade.");
    }
}
