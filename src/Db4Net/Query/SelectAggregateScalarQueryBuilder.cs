using System.Linq.Expressions;
using System.Threading;
using Db4Net.Rendering;

namespace Db4Net.Query;

/// <summary>
/// Builds and executes a scalar aggregate query whose result type is chosen by the terminal method.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class SelectAggregateScalarQueryBuilder<T>
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
    /// Applies additional query configuration only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to invoke <paramref name="configure"/>.</param>
    /// <param name="configure">Configures the current query builder.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> When(bool condition, Action<SelectAggregateScalarQueryBuilder<T>> configure)
    {
        ThrowHelper.ThrowIfNull(configure);
        if (condition)
        {
            configure(this);
        }

        return this;
    }

    /// <summary>
    /// Adds an AND filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> Where(string propertyName, Op op, object? value)
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
    public SelectAggregateScalarQueryBuilder<T> Where(string propertyName, Op op)
    {
        _state.AddValueFreeFilter(FilterBooleanOperator.And, propertyName, op);
        return this;
    }

    /// <summary>
    /// Adds an AND filter using a CLR property name only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> WhereIf(bool condition, string propertyName, Op op, object? value)
    {
        return condition ? Where(propertyName, op, value) : this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a CLR property name only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> WhereIf(bool condition, string propertyName, Op op)
    {
        return condition ? Where(propertyName, op) : this;
    }

    /// <summary>
    /// Adds an AND filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
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
    public SelectAggregateScalarQueryBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _state.AddValueFreeFilter(FilterBooleanOperator.And, memberSelector, op);
        return this;
    }

    /// <summary>
    /// Adds an AND filter only when <paramref name="condition"/> is true.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> WhereIf<TValue>(bool condition, Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        return condition ? Where(memberSelector, op, value) : this;
    }

    /// <summary>
    /// Adds an AND null-check filter only when <paramref name="condition"/> is true.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.DeletedAt</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> WhereIf<TValue>(bool condition, Expression<Func<T, TValue>> memberSelector, Op op)
    {
        return condition ? Where(memberSelector, op) : this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> WhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        AddGroup(FilterBooleanOperator.And, configure);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the group.</param>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> WhereGroupIf(bool condition, Action<FilterGroupBuilder<T>> configure)
    {
        ThrowHelper.ThrowIfNull(configure);
        if (!condition)
        {
            return this;
        }

        _state.AddGroupIfNotEmpty(FilterBooleanOperator.And, configure);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhere(string propertyName, Op op, object? value)
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
    public SelectAggregateScalarQueryBuilder<T> OrWhere(string propertyName, Op op)
    {
        _state.AddValueFreeFilter(FilterBooleanOperator.Or, propertyName, op);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a CLR property name only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhereIf(bool condition, string propertyName, Op op, object? value)
    {
        return condition ? OrWhere(propertyName, op, value) : this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a CLR property name only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhereIf(bool condition, string propertyName, Op op)
    {
        return condition ? OrWhere(propertyName, op) : this;
    }

    /// <summary>
    /// Adds an OR filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
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
    public SelectAggregateScalarQueryBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _state.AddValueFreeFilter(FilterBooleanOperator.Or, memberSelector, op);
        return this;
    }

    /// <summary>
    /// Adds an OR filter only when <paramref name="condition"/> is true.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhereIf<TValue>(bool condition, Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        return condition ? OrWhere(memberSelector, op, value) : this;
    }

    /// <summary>
    /// Adds an OR null-check filter only when <paramref name="condition"/> is true.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.DeletedAt</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhereIf<TValue>(bool condition, Expression<Func<T, TValue>> memberSelector, Op op)
    {
        return condition ? OrWhere(memberSelector, op) : this;
    }

    /// <summary>
    /// Adds a parenthesized OR filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        AddGroup(FilterBooleanOperator.Or, configure);
        return this;
    }

    /// <summary>Adds an AND <c>BETWEEN</c> filter using a CLR property name from <typeparamref name="T"/>.</summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="low">The inclusive lower bound. Must not be null.</param>
    /// <param name="high">The inclusive upper bound. Must not be null.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> WhereBetween(string propertyName, object? low, object? high)
    {
        _state.AddBetween(FilterBooleanOperator.And, propertyName, low, high);
        return this;
    }

    /// <summary>Adds an AND <c>BETWEEN</c> filter using a typed member selector.</summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Age</c>.</param>
    /// <param name="low">The inclusive lower bound. Must not be null.</param>
    /// <param name="high">The inclusive upper bound. Must not be null.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> WhereBetween<TValue>(Expression<Func<T, TValue>> memberSelector, object? low, object? high)
    {
        _state.AddBetween(FilterBooleanOperator.And, memberSelector, low, high);
        return this;
    }

    /// <summary>Adds an AND <c>BETWEEN</c> filter only when <paramref name="condition"/> is true.</summary>
    public SelectAggregateScalarQueryBuilder<T> WhereBetweenIf(bool condition, string propertyName, object? low, object? high)
        => condition ? WhereBetween(propertyName, low, high) : this;

    /// <summary>Adds an AND <c>BETWEEN</c> filter only when <paramref name="condition"/> is true, using a typed member selector.</summary>
    public SelectAggregateScalarQueryBuilder<T> WhereBetweenIf<TValue>(bool condition, Expression<Func<T, TValue>> memberSelector, object? low, object? high)
        => condition ? WhereBetween(memberSelector, low, high) : this;

    /// <summary>Adds an OR <c>BETWEEN</c> filter using a CLR property name from <typeparamref name="T"/>.</summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="low">The inclusive lower bound. Must not be null.</param>
    /// <param name="high">The inclusive upper bound. Must not be null.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhereBetween(string propertyName, object? low, object? high)
    {
        _state.AddBetween(FilterBooleanOperator.Or, propertyName, low, high);
        return this;
    }

    /// <summary>Adds an OR <c>BETWEEN</c> filter using a typed member selector.</summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Age</c>.</param>
    /// <param name="low">The inclusive lower bound. Must not be null.</param>
    /// <param name="high">The inclusive upper bound. Must not be null.</param>
    /// <returns>The current query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> OrWhereBetween<TValue>(Expression<Func<T, TValue>> memberSelector, object? low, object? high)
    {
        _state.AddBetween(FilterBooleanOperator.Or, memberSelector, low, high);
        return this;
    }

    /// <summary>Adds an OR <c>BETWEEN</c> filter only when <paramref name="condition"/> is true.</summary>
    public SelectAggregateScalarQueryBuilder<T> OrWhereBetweenIf(bool condition, string propertyName, object? low, object? high)
        => condition ? OrWhereBetween(propertyName, low, high) : this;

    /// <summary>Adds an OR <c>BETWEEN</c> filter only when <paramref name="condition"/> is true, using a typed member selector.</summary>
    public SelectAggregateScalarQueryBuilder<T> OrWhereBetweenIf<TValue>(bool condition, Expression<Func<T, TValue>> memberSelector, object? low, object? high)
        => condition ? OrWhereBetween(memberSelector, low, high) : this;

    /// <summary>
    /// Renders the SQL text and parameters without executing the query.
    /// </summary>
    /// <returns>The rendered SQL command definition.</returns>
    public RenderedSqlCommand ToCommand()
    {
        return new ScalarSqlRenderer(_options.Dialect).Render(_state.Model);
    }

    /// <summary>
    /// Executes the scalar aggregate query through Dapper and returns the scalar result.
    /// </summary>
    /// <typeparam name="TResult">The scalar result type returned by the aggregate query.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The aggregate result returned by the database.</returns>
    public TResult ExecuteScalar<TResult>(Db4NetExecutionOptions? options = null)
    {
        return _executor.Execute<TResult>(ToCommand(), options);
    }

    /// <summary>
    /// Asynchronously executes the scalar aggregate query through Dapper and returns the scalar result.
    /// </summary>
    /// <typeparam name="TResult">The scalar result type returned by the aggregate query.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The aggregate result returned by the database.</returns>
    public Task<TResult> ExecuteScalarAsync<TResult>(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _executor.ExecuteAsync<TResult>(ToCommand(), options, cancellationToken);
    }

    private void AddGroup(FilterBooleanOperator booleanOperator, Action<FilterGroupBuilder<T>> configure)
    {
        _state.AddGroup(booleanOperator, configure);
    }
}
