using System.Data;
using Db4Net.Metadata;
using Db4Net.Rendering;

namespace Db4Net.Query;

/// <summary>
/// Builds SELECT statements using string-based table and column identifiers.
/// </summary>
public partial class SelectQueryBuilder
{
    private readonly IDbConnection? _connection;
    private readonly FilterClauseBuilder _filters;
    private readonly SelectQueryModel _model;
    private readonly Db4NetOptions _options;

    internal SelectQueryBuilder(Db4NetOptions options, IDbConnection? connection, SelectQueryModel? model = null)
    {
        _options = options;
        _connection = connection;
        _model = model ?? new SelectQueryModel();
        _filters = new FilterClauseBuilder(_model.Filters);
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
    /// Existing string-based member references are interpreted as CLR property names for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A typed query builder for <typeparamref name="T"/>.</returns>
    public SelectQueryBuilder<T> From<T>()
    {
        BindToModel<T>(ModelMetadata<T>.TableName);
        return new SelectQueryBuilder<T>(_options, _connection, _model);
    }

    /// <summary>
    /// Sets the query table or view from an explicit identifier while using <typeparamref name="T"/> for member mapping.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="table">The table or view identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A typed query builder for <typeparamref name="T"/>.</returns>
    public SelectQueryBuilder<T> From<T>(string table)
    {
        BindToModel<T>(table);
        return new SelectQueryBuilder<T>(_options, _connection, _model);
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
        _filters.Add("AND", column, op, value);
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
        _filters.AddValueFree("AND", column, op);
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
        _filters.Add("OR", column, op, value);
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
        _filters.AddValueFree("OR", column, op);
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
    public RenderedSqlCommand ToCommand()
    {
        return new SelectSqlRenderer(_options.Dialect).Render(_model);
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

    private void BindToModel<T>(string table)
    {
        _model.Table = table;

        for (var index = 0; index < _model.Columns.Count; index++)
        {
            _model.Columns[index] = MapSelectColumn<T>(_model.Columns[index]);
        }

        for (var index = 0; index < _model.Filters.Count; index++)
        {
            var filter = _model.Filters[index];
            _model.Filters[index] = filter with { Column = MapPropertyName<T>(filter.Column) };
        }

        for (var index = 0; index < _model.Orders.Count; index++)
        {
            var order = _model.Orders[index];
            _model.Orders[index] = order with { Column = MapPropertyName<T>(order.Column) };
        }
    }

    private static SelectColumn MapSelectColumn<T>(SelectColumn column)
    {
        if (column.Alias is not null)
        {
            return column;
        }

        var metadata = ModelMetadata<T>.GetColumn(column.Column);
        return new SelectColumn(metadata.ColumnName, metadata.PropertyName);
    }

    private static string MapPropertyName<T>(string propertyName)
    {
        return ModelMetadata<T>.GetColumn(propertyName).ColumnName;
    }
}
