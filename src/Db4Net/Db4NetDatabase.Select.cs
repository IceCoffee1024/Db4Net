using System.Linq.Expressions;
using Db4Net.Metadata;
using Db4Net.Query;

namespace Db4Net;

public sealed partial class Db4NetDatabase
{
    /// <summary>
    /// Starts a select query with explicit columns.
    /// </summary>
    /// <param name="columns">The columns to include in the SELECT list.</param>
    /// <returns>A select query builder.</returns>
    public SelectQueryBuilder Select(params string[] columns)
    {
        return new SelectQueryBuilder(_options, _connection, executionOptions: _executionOptions).Select(columns);
    }

    /// <summary>
    /// Starts a select query with explicit columns.
    /// </summary>
    /// <param name="columns">The columns to include in the SELECT list.</param>
    /// <returns>A select query builder.</returns>
    public SelectQueryBuilder Select(IEnumerable<string> columns)
    {
        return new SelectQueryBuilder(_options, _connection, executionOptions: _executionOptions).Select(columns);
    }

    /// <summary>
    /// Starts a typed select query with explicit columns from member selectors.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="memberSelectors">Simple member selectors for the columns to include in the SELECT list.</param>
    /// <returns>A typed select query builder.</returns>
    public SelectQueryBuilder<T> Select<T>(params Expression<Func<T, object?>>[] memberSelectors)
    {
        EnsureEntityType<T>();
        return new SelectQueryBuilder(_options, _connection, executionOptions: _executionOptions).From<T>().Select(memberSelectors);
    }

    /// <summary>
    /// Starts a typed select query using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A typed select query builder.</returns>
    public SelectQueryBuilder<T> SelectFrom<T>()
    {
        EnsureEntityType<T>();
        return new SelectQueryBuilder(_options, _connection, executionOptions: _executionOptions).From<T>().SelectAllMappedColumns();
    }

    /// <summary>
    /// Starts a typed select query using an explicit table or view name while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="table">The table or view identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A typed select query builder.</returns>
    public SelectQueryBuilder<T> SelectFrom<T>(string table)
    {
        EnsureEntityType<T>();
        return new SelectQueryBuilder(_options, _connection, executionOptions: _executionOptions).From<T>(table).SelectAllMappedColumns();
    }

    /// <summary>
    /// Starts a typed SELECT COUNT(*) query using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A typed count query builder.</returns>
    public SelectCountQueryBuilder<T> SelectCountFrom<T>()
    {
        EnsureEntityType<T>();
        return new SelectCountQueryBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, _executionOptions);
    }

    /// <summary>
    /// Starts a typed SELECT COUNT(*) query using an explicit table or view name while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="table">The table or view identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A typed count query builder.</returns>
    public SelectCountQueryBuilder<T> SelectCountFrom<T>(string table)
    {
        EnsureEntityType<T>();
        return new SelectCountQueryBuilder<T>(_options, _connection, table, _executionOptions);
    }

    /// <summary>
    /// Starts a typed SELECT EXISTS query using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A typed existence query builder.</returns>
    public SelectExistsQueryBuilder<T> SelectExistsFrom<T>()
    {
        EnsureEntityType<T>();
        return new SelectExistsQueryBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, _executionOptions);
    }

    /// <summary>
    /// Starts a typed SELECT EXISTS query using an explicit table or view name while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="table">The table or view identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A typed existence query builder.</returns>
    public SelectExistsQueryBuilder<T> SelectExistsFrom<T>(string table)
    {
        EnsureEntityType<T>();
        return new SelectExistsQueryBuilder<T>(_options, _connection, table, _executionOptions);
    }

    /// <summary>
    /// Starts a typed scalar aggregate query using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A typed aggregate query builder.</returns>
    public SelectAggregateQueryBuilder<T> SelectAggregateFrom<T>()
    {
        EnsureEntityType<T>();
        return new SelectAggregateQueryBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, _executionOptions);
    }

    /// <summary>
    /// Starts a typed scalar aggregate query using an explicit table or view name while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="table">The table or view identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A typed aggregate query builder.</returns>
    public SelectAggregateQueryBuilder<T> SelectAggregateFrom<T>(string table)
    {
        EnsureEntityType<T>();
        return new SelectAggregateQueryBuilder<T>(_options, _connection, table, _executionOptions);
    }
}
