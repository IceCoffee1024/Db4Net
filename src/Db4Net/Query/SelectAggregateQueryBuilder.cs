using System.Data;
using System.Linq.Expressions;
using Db4Net.Metadata;

namespace Db4Net.Query;

/// <summary>
/// Starts typed scalar aggregate queries.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class SelectAggregateQueryBuilder<T>
{
    private readonly IDbConnection? _connection;
    private readonly Db4NetExecutionOptions? _executionOptions;
    private readonly Db4NetOptions _options;
    private readonly string _table;

    internal SelectAggregateQueryBuilder(Db4NetOptions options, IDbConnection? connection, string table, Db4NetExecutionOptions? executionOptions = null)
    {
        _options = options;
        _connection = connection;
        _table = table;
        _executionOptions = executionOptions;
    }

    /// <summary>
    /// Selects the maximum value of a mapped value-type column.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <returns>A scalar aggregate query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TValue?> Max<TValue>(Expression<Func<T, TValue>> memberSelector)
        where TValue : struct
    {
        return Create<TValue?, TValue>(ScalarProjectionKind.Max, memberSelector);
    }

    /// <summary>
    /// Selects the minimum value of a mapped value-type column.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <returns>A scalar aggregate query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, TValue?> Min<TValue>(Expression<Func<T, TValue>> memberSelector)
        where TValue : struct
    {
        return Create<TValue?, TValue>(ScalarProjectionKind.Min, memberSelector);
    }

    /// <summary>
    /// Selects the number of distinct non-null values in a mapped column.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Name</c>.</param>
    /// <returns>A scalar aggregate query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T, long> CountDistinct<TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        return Create<long, TValue>(ScalarProjectionKind.CountDistinct, memberSelector);
    }

    /// <summary>
    /// Selects the sum of a mapped column.
    /// </summary>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Amount</c>.</param>
    /// <returns>A scalar aggregate query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> Sum(Expression<Func<T, object?>> memberSelector)
    {
        return Create<object?>(ScalarProjectionKind.Sum, memberSelector);
    }

    /// <summary>
    /// Selects the average of a mapped column.
    /// </summary>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Quantity</c>.</param>
    /// <returns>A scalar aggregate query builder.</returns>
    public SelectAggregateScalarQueryBuilder<T> Average(Expression<Func<T, object?>> memberSelector)
    {
        return Create<object?>(ScalarProjectionKind.Average, memberSelector);
    }

    private SelectAggregateScalarQueryBuilder<T, TResult> Create<TResult, TValue>(
        ScalarProjectionKind projectionKind,
        Expression<Func<T, TValue>> memberSelector)
    {
        var column = ModelMetadataProvider.GetColumnName(memberSelector);
        return new SelectAggregateScalarQueryBuilder<T, TResult>(_options, _connection, _table, projectionKind, column, _executionOptions);
    }

    private SelectAggregateScalarQueryBuilder<T> Create<TValue>(
        ScalarProjectionKind projectionKind,
        Expression<Func<T, TValue>> memberSelector)
    {
        var column = ModelMetadataProvider.GetColumnName(memberSelector);
        return new SelectAggregateScalarQueryBuilder<T>(_options, _connection, _table, projectionKind, column, _executionOptions);
    }
}
