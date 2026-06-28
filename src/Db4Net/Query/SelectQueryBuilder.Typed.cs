using System.Data;
using System.Linq.Expressions;
using System.Threading;
using Db4Net.Metadata;

namespace Db4Net.Query;

/// <summary>
/// Builds SELECT statements using typed member selectors for column mapping.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class SelectQueryBuilder<T> : SelectQueryBuilder
{
    internal SelectQueryBuilder(Db4NetOptions options, IDbConnection? connection, SelectQueryModel? model = null)
        : base(options, connection, model)
    {
    }

    /// <summary>
    /// Sets the SELECT list using CLR property names from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyNames">CLR property names to include in the SELECT list.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> Select(params string[] propertyNames)
    {
        ThrowHelper.ThrowIfNull(propertyNames);
        ClearSelectColumns();

        foreach (var propertyName in propertyNames)
        {
            AddMappedSelectColumn(propertyName);
        }

        return this;
    }

    /// <summary>
    /// Sets the SELECT list using CLR property names from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyNames">CLR property names to include in the SELECT list.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> Select(IEnumerable<string> propertyNames)
    {
        ThrowHelper.ThrowIfNull(propertyNames);
        ClearSelectColumns();

        foreach (var propertyName in propertyNames)
        {
            AddMappedSelectColumn(propertyName);
        }

        return this;
    }

    /// <summary>
    /// Adds explicit columns to the SELECT list using typed member selectors.
    /// </summary>
    /// <param name="memberSelectors">Simple member selectors for the columns to include in the SELECT list.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder<T> Select(params Expression<Func<T, object?>>[] memberSelectors)
    {
        ThrowHelper.ThrowIfNull(memberSelectors);
        if (memberSelectors.Length == 0)
        {
            throw new ArgumentException("At least one member selector is required.", nameof(memberSelectors));
        }

        base.ClearSelectColumns();

        foreach (var memberSelector in memberSelectors)
        {
            var column = ModelMetadataProvider.GetColumnMetadata(memberSelector);
            base.AddSelectColumn(column.ColumnName, column.PropertyName);
        }

        return this;
    }

    internal SelectQueryBuilder<T> SelectAllMappedColumns()
    {
        if (ModelMetadata<T>.Columns.Count == 0)
        {
            throw new InvalidOperationException($"Type '{typeof(T).Name}' does not have any mapped columns.");
        }

        base.ClearSelectColumns();

        foreach (var column in ModelMetadata<T>.Columns)
        {
            base.AddSelectColumn(column.ColumnName, column.PropertyName);
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
    public new SelectQueryBuilder<T> Where(string propertyName, Op op, object? value)
    {
        base.Where(ModelMetadata<T>.GetColumn(propertyName).ColumnName, op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> Where(string propertyName, Op op)
    {
        base.Where(ModelMetadata<T>.GetColumn(propertyName).ColumnName, op);
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
    public SelectQueryBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        base.Where(ModelMetadataProvider.GetColumnName(memberSelector), op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        base.Where(ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group using typed member selectors or CLR property names.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder<T> WhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        var group = new FilterGroupBuilder<T>();
        configure(group);
        AddFilterGroup(FilterBooleanOperator.And, group.Filters);
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
    public SelectQueryBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        base.OrWhere(ModelMetadataProvider.GetColumnName(memberSelector), op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        base.OrWhere(ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> OrWhere(string propertyName, Op op, object? value)
    {
        base.OrWhere(ModelMetadata<T>.GetColumn(propertyName).ColumnName, op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> OrWhere(string propertyName, Op op)
    {
        base.OrWhere(ModelMetadata<T>.GetColumn(propertyName).ColumnName, op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized OR filter group using typed member selectors or CLR property names.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder<T> OrWhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        var group = new FilterGroupBuilder<T>();
        configure(group);
        AddFilterGroup(FilterBooleanOperator.Or, group.Filters);
        return this;
    }

    /// <summary>
    /// Adds an ascending ORDER BY clause using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder<T> OrderBy<TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        base.OrderBy(ModelMetadataProvider.GetColumnName(memberSelector));
        return this;
    }

    /// <summary>
    /// Adds an ascending ORDER BY clause using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to order by.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> OrderBy(string propertyName)
    {
        base.OrderBy(ModelMetadata<T>.GetColumn(propertyName).ColumnName);
        return this;
    }

    /// <summary>
    /// Adds a descending ORDER BY clause using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        base.OrderByDescending(ModelMetadataProvider.GetColumnName(memberSelector));
        return this;
    }

    /// <summary>
    /// Adds a descending ORDER BY clause using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to order by.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> OrderByDescending(string propertyName)
    {
        base.OrderByDescending(ModelMetadata<T>.GetColumn(propertyName).ColumnName);
        return this;
    }

    /// <summary>
    /// Limits the number of rows returned.
    /// </summary>
    /// <param name="count">The maximum row count.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> Limit(int count)
    {
        base.Limit(count);
        return this;
    }

    /// <summary>
    /// Skips a number of rows.
    /// </summary>
    /// <param name="count">The number of rows to skip.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> Offset(int count)
    {
        base.Offset(count);
        return this;
    }

    /// <summary>
    /// Applies one-based page pagination.
    /// </summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of rows per page.</param>
    /// <returns>The current query builder.</returns>
    public new SelectQueryBuilder<T> Page(int pageNumber, int pageSize)
    {
        base.Page(pageNumber, pageSize);
        return this;
    }

    /// <summary>
    /// Executes the query through Dapper and returns all rows as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The materialized rows.</returns>
    public IEnumerable<T> Query(Db4NetExecutionOptions? options = null)
    {
        return base.Query<T>(options);
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns all rows as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The materialized rows.</returns>
    public Task<IEnumerable<T>> QueryAsync(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return base.QueryAsync<T>(options, cancellationToken);
    }

    /// <summary>
    /// Executes the query through Dapper and returns the first row as <typeparamref name="T"/>, or the default value if no row exists.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The first materialized row, or the default value.</returns>
    public T? QueryFirstOrDefault(Db4NetExecutionOptions? options = null)
    {
        return base.QueryFirstOrDefault<T>(options);
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns the first row as <typeparamref name="T"/>, or the default value if no row exists.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The first materialized row, or the default value.</returns>
    public Task<T?> QueryFirstOrDefaultAsync(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return base.QueryFirstOrDefaultAsync<T>(options, cancellationToken);
    }

    /// <summary>
    /// Executes the query through Dapper and returns a single row as <typeparamref name="T"/>, or the default value if no row exists.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The single materialized row, or the default value.</returns>
    public T? QuerySingleOrDefault(Db4NetExecutionOptions? options = null)
    {
        return base.QuerySingleOrDefault<T>(options);
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns a single row as <typeparamref name="T"/>, or the default value if no row exists.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The single materialized row, or the default value.</returns>
    public Task<T?> QuerySingleOrDefaultAsync(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return base.QuerySingleOrDefaultAsync<T>(options, cancellationToken);
    }

    private void AddMappedSelectColumn(string propertyName)
    {
        var column = ModelMetadata<T>.GetColumn(propertyName);
        AddSelectColumn(column.ColumnName, column.PropertyName);
    }
}
