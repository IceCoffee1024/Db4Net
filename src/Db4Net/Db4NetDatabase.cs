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
    private readonly Db4NetExecutionOptions? _executionOptions;
    private readonly Db4NetOptions _options;

    private Db4NetDatabase(Db4NetOptions options, IDbConnection? connection = null, Db4NetExecutionOptions? executionOptions = null)
    {
        _options = options;
        _connection = connection;
        _executionOptions = executionOptions;
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
    /// Creates a new facade that applies default execution options to terminal methods.
    /// </summary>
    /// <param name="executionOptions">Default Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>A Db4Net facade with default execution options.</returns>
    public Db4NetDatabase WithExecutionOptions(Db4NetExecutionOptions executionOptions)
    {
        ThrowHelper.ThrowIfNull(executionOptions);
        return new Db4NetDatabase(_options, _connection, Db4NetExecutionOptions.Merge(_executionOptions, executionOptions));
    }

    /// <summary>
    /// Creates a new facade that applies an existing transaction to terminal methods.
    /// </summary>
    /// <param name="transaction">The transaction passed to Dapper execution.</param>
    /// <returns>A Db4Net facade with the transaction as a default execution option.</returns>
    public Db4NetDatabase WithTransaction(IDbTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return WithExecutionOptions(new Db4NetExecutionOptions { Transaction = transaction });
    }

    /// <summary>
    /// Begins a transaction on the bound connection and returns a Db4Net transaction facade.
    /// </summary>
    /// <returns>A Db4Net transaction facade. Disposing it without committing rolls back the transaction.</returns>
    public Db4NetTransaction BeginTransaction()
    {
        return new Db4NetTransaction(this, RequireConnection().BeginTransaction());
    }

    /// <summary>
    /// Begins a transaction on the bound connection with the specified isolation level and returns a Db4Net transaction facade.
    /// </summary>
    /// <param name="isolationLevel">The transaction isolation level.</param>
    /// <returns>A Db4Net transaction facade. Disposing it without committing rolls back the transaction.</returns>
    public Db4NetTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        return new Db4NetTransaction(this, RequireConnection().BeginTransaction(isolationLevel));
    }

    /// <summary>
    /// Executes work inside a Db4Net-owned transaction and commits when the delegate succeeds.
    /// </summary>
    /// <param name="work">The work to execute inside the transaction.</param>
    public void ExecuteInTransaction(Action<Db4NetTransaction> work)
    {
        ThrowHelper.ThrowIfNull(work);

        using var transaction = BeginTransaction();
        work(transaction);
        transaction.Commit();
    }

    /// <summary>
    /// Executes work inside a Db4Net-owned transaction and commits when the delegate succeeds.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the delegate.</typeparam>
    /// <param name="work">The work to execute inside the transaction.</param>
    /// <returns>The result returned by the delegate.</returns>
    public TResult ExecuteInTransaction<TResult>(Func<Db4NetTransaction, TResult> work)
    {
        ThrowHelper.ThrowIfNull(work);

        using var transaction = BeginTransaction();
        var result = work(transaction);
        transaction.Commit();
        return result;
    }

    /// <summary>
    /// Asynchronously executes work inside a Db4Net-owned transaction and commits when the delegate succeeds.
    /// </summary>
    /// <param name="work">The work to execute inside the transaction.</param>
    /// <returns>A task that completes when the work has finished and the transaction has committed.</returns>
    public async Task ExecuteInTransactionAsync(Func<Db4NetTransaction, Task> work)
    {
        ThrowHelper.ThrowIfNull(work);

        using var transaction = BeginTransaction();
        await work(transaction).ConfigureAwait(false);
        transaction.Commit();
    }

    /// <summary>
    /// Asynchronously executes work inside a Db4Net-owned transaction and commits when the delegate succeeds.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the delegate.</typeparam>
    /// <param name="work">The work to execute inside the transaction.</param>
    /// <returns>A task containing the result returned by the delegate.</returns>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Db4NetTransaction, Task<TResult>> work)
    {
        ThrowHelper.ThrowIfNull(work);

        using var transaction = BeginTransaction();
        var result = await work(transaction).ConfigureAwait(false);
        transaction.Commit();
        return result;
    }

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

    private static void EnsureEntityType<T>(string? sequenceAlternative = null)
    {
        var type = typeof(T);
        if (sequenceAlternative is not null && ModelMetadataProvider.IsSequenceType(type))
        {
            throw new ArgumentException($"Type '{ModelMetadataProvider.GetDisplayName(type)}' is a sequence type. Use {sequenceAlternative}(...) for multiple entities.");
        }

        ModelMetadataProvider.EnsureMappedModelType(type);
    }

    private IDbConnection RequireConnection()
    {
        return _connection ?? throw new InvalidOperationException("Dapper execution requires an IDbConnection. Use connection.UseDb4Net(options) to create the database facade.");
    }
}
