using System.Data;
using System.Linq.Expressions;
using Dapper;
using Db4Net.Metadata;
using Db4Net.Rendering;

namespace Db4Net.Query;

/// <summary>
/// Builds SELECT statements using string-based table and column identifiers.
/// </summary>
public class SelectQueryBuilder
{
    private readonly IDbConnection? _connection;
    private readonly QueryModel _model;
    private readonly Db4NetOptions _options;

    internal SelectQueryBuilder(Db4NetOptions options, IDbConnection? connection, QueryModel? model = null)
    {
        _options = options;
        _connection = connection;
        _model = model ?? new QueryModel();
    }

    /// <summary>
    /// Adds explicit columns to the SELECT list.
    /// </summary>
    /// <param name="columns">The column identifiers. They are validated and quoted by the configured SQL dialect.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder Select(params string[] columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        foreach (var column in columns)
        {
            AddSelectColumn(column);
        }

        return this;
    }

    /// <summary>
    /// Adds explicit columns to the SELECT list.
    /// </summary>
    /// <param name="columns">The column identifiers. They are validated and quoted by the configured SQL dialect.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder Select(IEnumerable<string> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        foreach (var column in columns)
        {
            AddSelectColumn(column);
        }

        return this;
    }

    /// <summary>
    /// Sets the query table from the mapping for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A typed query builder for <typeparamref name="T"/>.</returns>
    public SelectQueryBuilder<T> From<T>()
    {
        _model.Table = ModelMetadataProvider.GetTableName(typeof(T));
        return new SelectQueryBuilder<T>(_options, _connection, _model);
    }

    /// <summary>
    /// Sets the query table from an explicit table identifier.
    /// </summary>
    /// <param name="table">The table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder From(string table)
    {
        _model.Table = table;
        return this;
    }

    /// <summary>
    /// Adds an AND filter using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder Where(string column, Op op, object? value)
    {
        EnsureValidOperatorValue(op, value);
        _model.Filters.Add(new FilterClause("AND", column, op, value));
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder Where(string column, Op op)
    {
        EnsureValueFreeOperator(op);
        _model.Filters.Add(new FilterClause("AND", column, op, null));
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder OrWhere(string column, Op op, object? value)
    {
        EnsureValidOperatorValue(op, value);
        _model.Filters.Add(new FilterClause("OR", column, op, value));
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder OrWhere(string column, Op op)
    {
        EnsureValueFreeOperator(op);
        _model.Filters.Add(new FilterClause("OR", column, op, null));
        return this;
    }

    /// <summary>
    /// Adds an ascending ORDER BY clause using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder OrderBy(string column)
    {
        _model.Orders.Add(new OrderClause(column, false));
        return this;
    }

    /// <summary>
    /// Adds a descending ORDER BY clause using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder OrderByDescending(string column)
    {
        _model.Orders.Add(new OrderClause(column, true));
        return this;
    }

    /// <summary>
    /// Limits the number of rows returned.
    /// </summary>
    /// <param name="count">The maximum row count.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder Limit(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Limit must be greater than or equal to 0.");
        }

        _model.Limit = count;
        return this;
    }

    /// <summary>
    /// Skips a number of rows.
    /// </summary>
    /// <param name="count">The number of rows to skip.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder Offset(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Offset must be greater than or equal to 0.");
        }

        _model.Offset = count;
        return this;
    }

    /// <summary>
    /// Applies one-based page pagination.
    /// </summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of rows per page.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder Page(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
        }

        _model.Limit = pageSize;
        _model.Offset = (pageNumber - 1) * pageSize;
        return this;
    }

    /// <summary>
    /// Renders the SQL text and parameters without executing the query.
    /// </summary>
    /// <returns>The rendered SQL command definition.</returns>
    public SqlCommandDefinition ToCommand()
    {
        return new SqlRenderer(_options.Dialect).Render(_model);
    }

    /// <summary>
    /// Executes the query through Dapper and returns all rows.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <returns>The materialized rows.</returns>
    public IEnumerable<TResult> Query<TResult>()
    {
        var command = ToCommand();
        return RequireConnection().Query<TResult>(command.Sql, command.Parameters);
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns all rows.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <returns>The materialized rows.</returns>
    public Task<IEnumerable<TResult>> QueryAsync<TResult>()
    {
        var command = ToCommand();
        return RequireConnection().QueryAsync<TResult>(command.Sql, command.Parameters);
    }

    /// <summary>
    /// Executes the query through Dapper and returns the first row, or the default value if no row exists.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <returns>The first materialized row, or the default value.</returns>
    public TResult? QueryFirstOrDefault<TResult>()
    {
        var command = ToCommand();
        return RequireConnection().QueryFirstOrDefault<TResult>(command.Sql, command.Parameters);
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns the first row, or the default value if no row exists.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <returns>The first materialized row, or the default value.</returns>
    public Task<TResult?> QueryFirstOrDefaultAsync<TResult>()
    {
        var command = ToCommand();
        return RequireConnection().QueryFirstOrDefaultAsync<TResult>(command.Sql, command.Parameters);
    }

    /// <summary>
    /// Executes the query through Dapper and returns a single row, or the default value if no row exists.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <returns>The single materialized row, or the default value.</returns>
    public TResult? QuerySingleOrDefault<TResult>()
    {
        var command = ToCommand();
        return RequireConnection().QuerySingleOrDefault<TResult>(command.Sql, command.Parameters);
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns a single row, or the default value if no row exists.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <returns>The single materialized row, or the default value.</returns>
    public Task<TResult?> QuerySingleOrDefaultAsync<TResult>()
    {
        var command = ToCommand();
        return RequireConnection().QuerySingleOrDefaultAsync<TResult>(command.Sql, command.Parameters);
    }

    /// <summary>
    /// Executes the rendered command through Dapper and returns the affected row count.
    /// </summary>
    /// <returns>The affected row count returned by Dapper.</returns>
    public int Execute()
    {
        var command = ToCommand();
        return RequireConnection().Execute(command.Sql, command.Parameters);
    }

    /// <summary>
    /// Asynchronously executes the rendered command through Dapper and returns the affected row count.
    /// </summary>
    /// <returns>The affected row count returned by Dapper.</returns>
    public Task<int> ExecuteAsync()
    {
        var command = ToCommand();
        return RequireConnection().ExecuteAsync(command.Sql, command.Parameters);
    }

    private IDbConnection RequireConnection()
    {
        return _connection ?? throw new InvalidOperationException("Dapper execution requires an IDbConnection. Use connection.UseDb4Net(options) to create the database facade.");
    }

    internal SelectQueryBuilder AddSelectColumn(string column, string? alias = null)
    {
        _model.Columns.Add(new SelectColumn(column, alias));
        return this;
    }

    internal void ClearSelectColumns()
    {
        _model.Columns.Clear();
    }

    private static void EnsureValueFreeOperator(Op op)
    {
        if (op is not (Op.IsNull or Op.IsNotNull))
        {
            throw new ArgumentException($"Operator {op} requires a value.", nameof(op));
        }
    }

    private static void EnsureValidOperatorValue(Op op, object? value)
    {
        if (op is (Op.IsNull or Op.IsNotNull) && value is not null)
        {
            throw new ArgumentException($"Operator {op} does not accept a value.", nameof(value));
        }
    }
}

/// <summary>
/// Builds SELECT statements using typed member selectors for column mapping.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class SelectQueryBuilder<T> : SelectQueryBuilder
{
    internal SelectQueryBuilder(Db4NetOptions options, IDbConnection? connection, QueryModel? model = null)
        : base(options, connection, model)
    {
    }

    /// <summary>
    /// Adds explicit columns to the SELECT list using typed member selectors.
    /// </summary>
    /// <param name="memberSelectors">Simple member selectors for the columns to include in the SELECT list.</param>
    /// <returns>The current query builder.</returns>
    public SelectQueryBuilder<T> Select(params Expression<Func<T, object?>>[] memberSelectors)
    {
        ArgumentNullException.ThrowIfNull(memberSelectors);
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
        base.ClearSelectColumns();

        foreach (var column in ModelMetadataProvider.GetColumnMetadata(typeof(T)))
        {
            base.AddSelectColumn(column.ColumnName, column.PropertyName);
        }

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
}
