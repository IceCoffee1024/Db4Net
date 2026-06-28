using System.Data;
using System.Linq.Expressions;
using Db4Net.Commands;
using Db4Net.Metadata;
using Db4Net.Query;

namespace Db4Net;

/// <summary>
/// Entry point for creating Db4Net query and command builders.
/// </summary>
public sealed class Db4NetDatabase
{
    private readonly IDbConnection? _connection;
    private readonly Db4NetOptions _options;

    private Db4NetDatabase(Db4NetOptions options, IDbConnection? connection = null)
    {
        _options = options;
        _connection = connection;
    }

    /// <summary>
    /// Creates a Db4Net facade that can build SQL without executing it.
    /// </summary>
    /// <param name="options">The SQL generation options to use.</param>
    /// <returns>A Db4Net facade for building queries.</returns>
    public static Db4NetDatabase Create(Db4NetOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        return new Db4NetDatabase(options);
    }

    internal static Db4NetDatabase Create(Db4NetOptions options, IDbConnection connection)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(connection);
        return new Db4NetDatabase(options, connection);
    }

    /// <summary>
    /// Starts a select query with explicit columns.
    /// </summary>
    /// <param name="columns">The columns to include in the SELECT list.</param>
    /// <returns>A select query builder.</returns>
    public SelectQueryBuilder Select(params string[] columns)
    {
        return new SelectQueryBuilder(_options, _connection).Select(columns);
    }

    /// <summary>
    /// Starts a select query with explicit columns.
    /// </summary>
    /// <param name="columns">The columns to include in the SELECT list.</param>
    /// <returns>A select query builder.</returns>
    public SelectQueryBuilder Select(IEnumerable<string> columns)
    {
        return new SelectQueryBuilder(_options, _connection).Select(columns);
    }

    /// <summary>
    /// Starts a typed select query with explicit columns from member selectors.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="memberSelectors">Simple member selectors for the columns to include in the SELECT list.</param>
    /// <returns>A typed select query builder.</returns>
    public SelectQueryBuilder<T> Select<T>(params Expression<Func<T, object?>>[] memberSelectors)
    {
        return new SelectQueryBuilder(_options, _connection).From<T>().Select(memberSelectors);
    }

    /// <summary>
    /// Starts a typed select query using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A typed select query builder.</returns>
    public SelectQueryBuilder<T> SelectFrom<T>()
    {
        return new SelectQueryBuilder(_options, _connection).From<T>().SelectAllMappedColumns();
    }

    /// <summary>
    /// Starts a typed select query using an explicit table or view name while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="table">The table or view identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A typed select query builder.</returns>
    public SelectQueryBuilder<T> SelectFrom<T>(string table)
    {
        return new SelectQueryBuilder(_options, _connection).From<T>(table).SelectAllMappedColumns();
    }

    /// <summary>
    /// Starts an INSERT command using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>An insert command builder.</returns>
    public InsertCommandBuilder<T> InsertInto<T>()
    {
        return new InsertCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName);
    }

    /// <summary>
    /// Starts an INSERT command using an explicit table while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An insert command builder.</returns>
    public InsertCommandBuilder<T> InsertInto<T>(string table)
    {
        return new InsertCommandBuilder<T>(_options, _connection, table);
    }

    /// <summary>
    /// Starts an INSERT command for an entity instance using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entity">The entity instance to insert.</param>
    /// <returns>An insert command builder.</returns>
    public InsertCommandBuilder<T> Insert<T>(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return InsertInto<T>().Values(entity);
    }

    /// <summary>
    /// Starts an INSERT command for an entity instance using an explicit table while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entity">The entity instance to insert.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An insert command builder.</returns>
    public InsertCommandBuilder<T> Insert<T>(T entity, string table)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return InsertInto<T>(table).Values(entity);
    }

    /// <summary>
    /// Starts an INSERT command that ignores the row when the conflict target already exists.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entity">The entity instance to insert.</param>
    /// <returns>An insert-or-ignore command builder.</returns>
    public InsertOrIgnoreCommandBuilder<T> InsertOrIgnore<T>(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return new InsertOrIgnoreCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entity);
    }

    /// <summary>
    /// Starts an INSERT command that ignores the row when the conflict target already exists, using an explicit target table.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entity">The entity instance to insert.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An insert-or-ignore command builder.</returns>
    public InsertOrIgnoreCommandBuilder<T> InsertOrIgnore<T>(T entity, string table)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return new InsertOrIgnoreCommandBuilder<T>(_options, _connection, table, entity);
    }

    /// <summary>
    /// Starts an INSERT command that updates mapped columns when the conflict target already exists.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entity">The entity instance to insert or update.</param>
    /// <returns>An insert-or-update command builder.</returns>
    public InsertOrUpdateCommandBuilder<T> InsertOrUpdate<T>(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return new InsertOrUpdateCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entity);
    }

    /// <summary>
    /// Starts an INSERT command that updates mapped columns when the conflict target already exists, using an explicit target table.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entity">The entity instance to insert or update.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An insert-or-update command builder.</returns>
    public InsertOrUpdateCommandBuilder<T> InsertOrUpdate<T>(T entity, string table)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return new InsertOrUpdateCommandBuilder<T>(_options, _connection, table, entity);
    }

    /// <summary>
    /// Starts INSERT commands for multiple entity instances using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entities">The entity instances to insert.</param>
    /// <returns>An insert-many command builder.</returns>
    public InsertManyCommandBuilder<T> InsertMany<T>(IEnumerable<T> entities)
    {
        EnsureEntityType<T>();
        return new InsertManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities);
    }

    /// <summary>
    /// Starts INSERT commands for multiple entity instances using an explicit table while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entities">The entity instances to insert.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An insert-many command builder.</returns>
    public InsertManyCommandBuilder<T> InsertMany<T>(IEnumerable<T> entities, string table)
    {
        EnsureEntityType<T>();
        return new InsertManyCommandBuilder<T>(_options, _connection, table, entities);
    }

    /// <summary>
    /// Starts INSERT commands for multiple entity instances and ignores rows whose conflict target already exists.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entities">The entity instances to insert.</param>
    /// <returns>An insert-or-ignore-many command builder.</returns>
    public InsertOrIgnoreManyCommandBuilder<T> InsertOrIgnoreMany<T>(IEnumerable<T> entities)
    {
        EnsureEntityType<T>();
        return new InsertOrIgnoreManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities);
    }

    /// <summary>
    /// Starts INSERT commands for multiple entity instances and ignores rows whose conflict target already exists, using an explicit target table.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entities">The entity instances to insert.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An insert-or-ignore-many command builder.</returns>
    public InsertOrIgnoreManyCommandBuilder<T> InsertOrIgnoreMany<T>(IEnumerable<T> entities, string table)
    {
        EnsureEntityType<T>();
        return new InsertOrIgnoreManyCommandBuilder<T>(_options, _connection, table, entities);
    }

    /// <summary>
    /// Starts INSERT commands for multiple entity instances and updates mapped columns when the conflict target already exists.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entities">The entity instances to insert or update.</param>
    /// <returns>An insert-or-update-many command builder.</returns>
    public InsertOrUpdateManyCommandBuilder<T> InsertOrUpdateMany<T>(IEnumerable<T> entities)
    {
        EnsureEntityType<T>();
        return new InsertOrUpdateManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities);
    }

    /// <summary>
    /// Starts INSERT commands for multiple entity instances and updates mapped columns when the conflict target already exists, using an explicit target table.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entities">The entity instances to insert or update.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An insert-or-update-many command builder.</returns>
    public InsertOrUpdateManyCommandBuilder<T> InsertOrUpdateMany<T>(IEnumerable<T> entities, string table)
    {
        EnsureEntityType<T>();
        return new InsertOrUpdateManyCommandBuilder<T>(_options, _connection, table, entities);
    }

    /// <summary>
    /// Starts an UPDATE command using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>An update command builder.</returns>
    public UpdateCommandBuilder<T> Update<T>()
    {
        return new UpdateCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName);
    }

    /// <summary>
    /// Starts an UPDATE command using an explicit table while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An update command builder.</returns>
    public UpdateCommandBuilder<T> Update<T>(string table)
    {
        return new UpdateCommandBuilder<T>(_options, _connection, table);
    }

    /// <summary>
    /// Starts an UPDATE command for an entity instance using key properties for the WHERE clause.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entity">The entity instance to update.</param>
    /// <returns>An update command builder.</returns>
    public UpdateCommandBuilder<T> Update<T>(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return Update<T>().SetEntityValues(entity).WhereKey(entity);
    }

    /// <summary>
    /// Starts an UPDATE command for an entity instance using an explicit table and key properties for the WHERE clause.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entity">The entity instance to update.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An update command builder.</returns>
    public UpdateCommandBuilder<T> Update<T>(T entity, string table)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return Update<T>(table).SetEntityValues(entity).WhereKey(entity);
    }

    /// <summary>
    /// Starts UPDATE commands for multiple entity instances using key properties for each WHERE clause.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entities">The entity instances to update.</param>
    /// <returns>An update-many command builder.</returns>
    public UpdateManyCommandBuilder<T> UpdateMany<T>(IEnumerable<T> entities)
    {
        EnsureEntityType<T>();
        return new UpdateManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities);
    }

    /// <summary>
    /// Starts UPDATE commands for multiple entity instances using an explicit table and key properties for each WHERE clause.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entities">The entity instances to update.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An update-many command builder.</returns>
    public UpdateManyCommandBuilder<T> UpdateMany<T>(IEnumerable<T> entities, string table)
    {
        EnsureEntityType<T>();
        return new UpdateManyCommandBuilder<T>(_options, _connection, table, entities);
    }

    /// <summary>
    /// Starts a DELETE command using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A delete command builder.</returns>
    public DeleteCommandBuilder<T> DeleteFrom<T>()
    {
        return new DeleteCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName);
    }

    /// <summary>
    /// Starts a DELETE command using an explicit table while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A delete command builder.</returns>
    public DeleteCommandBuilder<T> DeleteFrom<T>(string table)
    {
        return new DeleteCommandBuilder<T>(_options, _connection, table);
    }

    /// <summary>
    /// Starts a DELETE command for an entity instance using key properties for the WHERE clause.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entity">The entity instance to delete.</param>
    /// <returns>A delete command builder.</returns>
    public DeleteCommandBuilder<T> Delete<T>(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return DeleteFrom<T>().WhereKey(entity);
    }

    /// <summary>
    /// Starts a DELETE command for an entity instance using an explicit table and key properties for the WHERE clause.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entity">The entity instance to delete.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A delete command builder.</returns>
    public DeleteCommandBuilder<T> Delete<T>(T entity, string table)
    {
        ThrowHelper.ThrowIfNull(entity);
        EnsureEntityType<T>();
        return DeleteFrom<T>(table).WhereKey(entity);
    }

    /// <summary>
    /// Starts DELETE commands for multiple entity instances using key properties for each WHERE clause.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <param name="entities">The entity instances to delete.</param>
    /// <returns>A delete-many command builder.</returns>
    public DeleteManyCommandBuilder<T> DeleteMany<T>(IEnumerable<T> entities)
    {
        EnsureEntityType<T>();
        return new DeleteManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities);
    }

    /// <summary>
    /// Starts DELETE commands for multiple entity instances using an explicit table and key properties for each WHERE clause.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="entities">The entity instances to delete.</param>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A delete-many command builder.</returns>
    public DeleteManyCommandBuilder<T> DeleteMany<T>(IEnumerable<T> entities, string table)
    {
        EnsureEntityType<T>();
        return new DeleteManyCommandBuilder<T>(_options, _connection, table, entities);
    }

    private static void EnsureEntityType<T>()
    {
        var type = typeof(T);
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        {
            throw new ArgumentException($"Type '{type.Name}' does not have any mapped columns.");
        }
    }
}
