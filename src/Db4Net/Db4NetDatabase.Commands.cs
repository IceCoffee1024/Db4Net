using Db4Net.Commands;
using Db4Net.Metadata;

namespace Db4Net;

public sealed partial class Db4NetDatabase
{
    /// <summary>
    /// Starts an INSERT command using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>An insert command builder.</returns>
    public InsertCommandBuilder<T> InsertInto<T>()
    {
        EnsureEntityType<T>();
        return new InsertCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, _executionOptions);
    }

    /// <summary>
    /// Starts an INSERT command using an explicit table while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An insert command builder.</returns>
    public InsertCommandBuilder<T> InsertInto<T>(string table)
    {
        EnsureEntityType<T>();
        return new InsertCommandBuilder<T>(_options, _connection, table, _executionOptions);
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
        EnsureEntityType<T>("InsertMany");
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
        EnsureEntityType<T>("InsertMany");
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
        EnsureEntityType<T>("InsertOrIgnoreMany");
        return new InsertOrIgnoreCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entity, _executionOptions);
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
        EnsureEntityType<T>("InsertOrIgnoreMany");
        return new InsertOrIgnoreCommandBuilder<T>(_options, _connection, table, entity, _executionOptions);
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
        EnsureEntityType<T>("InsertOrUpdateMany");
        return new InsertOrUpdateCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entity, _executionOptions);
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
        EnsureEntityType<T>("InsertOrUpdateMany");
        return new InsertOrUpdateCommandBuilder<T>(_options, _connection, table, entity, _executionOptions);
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
        return new InsertManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities, _executionOptions);
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
        return new InsertManyCommandBuilder<T>(_options, _connection, table, entities, _executionOptions);
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
        return new InsertOrIgnoreManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities, _executionOptions);
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
        return new InsertOrIgnoreManyCommandBuilder<T>(_options, _connection, table, entities, _executionOptions);
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
        return new InsertOrUpdateManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities, _executionOptions);
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
        return new InsertOrUpdateManyCommandBuilder<T>(_options, _connection, table, entities, _executionOptions);
    }

    /// <summary>
    /// Starts an UPDATE command using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>An update command builder.</returns>
    public UpdateCommandBuilder<T> Update<T>()
    {
        EnsureEntityType<T>();
        return new UpdateCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, _executionOptions);
    }

    /// <summary>
    /// Starts an UPDATE command using an explicit table while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>An update command builder.</returns>
    public UpdateCommandBuilder<T> Update<T>(string table)
    {
        EnsureEntityType<T>();
        return new UpdateCommandBuilder<T>(_options, _connection, table, _executionOptions);
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
        EnsureEntityType<T>("UpdateMany");
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
        EnsureEntityType<T>("UpdateMany");
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
        return new UpdateManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities, _executionOptions);
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
        return new UpdateManyCommandBuilder<T>(_options, _connection, table, entities, _executionOptions);
    }

    /// <summary>
    /// Starts a DELETE command using the table mapped from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
    /// <returns>A delete command builder.</returns>
    public DeleteCommandBuilder<T> DeleteFrom<T>()
    {
        EnsureEntityType<T>();
        return new DeleteCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, _executionOptions);
    }

    /// <summary>
    /// Starts a DELETE command using an explicit table while mapping columns from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
    /// <param name="table">The target table identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <returns>A delete command builder.</returns>
    public DeleteCommandBuilder<T> DeleteFrom<T>(string table)
    {
        EnsureEntityType<T>();
        return new DeleteCommandBuilder<T>(_options, _connection, table, _executionOptions);
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
        EnsureEntityType<T>("DeleteMany");
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
        EnsureEntityType<T>("DeleteMany");
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
        return new DeleteManyCommandBuilder<T>(_options, _connection, ModelMetadata<T>.TableName, entities, _executionOptions);
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
        return new DeleteManyCommandBuilder<T>(_options, _connection, table, entities, _executionOptions);
    }
}
