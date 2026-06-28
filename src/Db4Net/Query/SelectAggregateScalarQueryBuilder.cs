using System.Linq.Expressions;
using System.Threading;
using Db4Net.Rendering;

namespace Db4Net.Query;

/// <summary>
/// Builds and executes a typed scalar aggregate query.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
/// <typeparam name="TResult">The scalar result type returned by the aggregate query.</typeparam>
public sealed class SelectAggregateScalarQueryBuilder<T, TResult>
{
    private readonly DapperScalarExecutor _executor;
    private readonly Db4NetOptions _options;
    private readonly ScalarQueryBuilderState<T> _state;

    internal SelectAggregateScalarQueryBuilder(
        Db4NetOptions options,
        System.Data.IDbConnection? connection,
        string table,
        ScalarProjectionKind projectionKind,
        string column,
        Db4NetExecutionOptions? executionOptions = null)
    {
        _options = options;
        _executor = new DapperScalarExecutor(connection, executionOptions);
        _state = new ScalarQueryBuilderState<T>(table, projectionKind, column);
    }

    /// <summary>
    /// Adds an AND filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TResult> Where(string propertyName, Op op, object? value)
    {
        _state.AddFilter(FilterBooleanOperator.And, propertyName, op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TResult> Where(string propertyName, Op op)
    {
        _state.AddValueFreeFilter(FilterBooleanOperator.And, propertyName, op);
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
    public SelectAggregateScalarQueryBuilder<T, TResult> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        _state.AddFilter(FilterBooleanOperator.And, memberSelector, op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.DeletedAt</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TResult> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _state.AddValueFreeFilter(FilterBooleanOperator.And, memberSelector, op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TResult> WhereGroup(Action<FilterGroupBuilder<T>> configure)
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
    public SelectAggregateScalarQueryBuilder<T, TResult> OrWhere(string propertyName, Op op, object? value)
    {
        _state.AddFilter(FilterBooleanOperator.Or, propertyName, op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TResult> OrWhere(string propertyName, Op op)
    {
        _state.AddValueFreeFilter(FilterBooleanOperator.Or, propertyName, op);
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
    public SelectAggregateScalarQueryBuilder<T, TResult> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        _state.AddFilter(FilterBooleanOperator.Or, memberSelector, op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.DeletedAt</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TResult> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _state.AddValueFreeFilter(FilterBooleanOperator.Or, memberSelector, op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized OR filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TResult> OrWhereGroup(Action<FilterGroupBuilder<T>> configure)
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
        return new ScalarSqlRenderer(_options.Dialect).Render(_state.Model);
    }

    /// <summary>
    /// Executes the scalar aggregate query through Dapper.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The aggregate result returned by the database.</returns>
    public TResult Execute(Db4NetExecutionOptions? options = null)
    {
        return _executor.Execute<TResult>(ToCommand(), options);
    }

    /// <summary>
    /// Asynchronously executes the scalar aggregate query through Dapper.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The aggregate result returned by the database.</returns>
    public Task<TResult> ExecuteAsync(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _executor.ExecuteAsync<TResult>(ToCommand(), options, cancellationToken);
    }

    private void AddGroup(FilterBooleanOperator booleanOperator, Action<FilterGroupBuilder<T>> configure)
    {
        _state.AddGroup(booleanOperator, configure);
    }
}
