using System.Data;
using System.Linq.Expressions;
using System.Threading;
using Db4Net.Metadata;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Executes INSERT commands for multiple mapped CLR model instances and updates mapped columns when a conflict target already exists.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class InsertOrUpdateManyCommandBuilder<T>
{
    private readonly DapperCommandExecutor _executor;
    private readonly T[] _entities;
    private readonly Db4NetOptions _options;
    private readonly string _table;
    private ColumnMetadata[] _conflictColumns = [];
    private ColumnMetadata[] _updateColumns = [];

    internal InsertOrUpdateManyCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table, IEnumerable<T> entities, Db4NetExecutionOptions? executionOptions = null)
    {
        _options = options;
        _executor = new DapperCommandExecutor(connection, executionOptions);
        _table = table;
        _entities = ManyCommandBuilderSupport<T>.Materialize(entities);
    }

    /// <summary>
    /// Overrides the conflict target using mapped CLR property selectors.
    /// </summary>
    /// <param name="memberSelectors">Simple member selectors for the conflict target columns.</param>
    /// <returns>The current command builder.</returns>
    public InsertOrUpdateManyCommandBuilder<T> OnConflict(params Expression<Func<T, object?>>[] memberSelectors)
    {
        _conflictColumns = ConflictInsertBuilderSupport<T>.GetColumns(memberSelectors);
        return this;
    }

    /// <summary>
    /// Overrides the columns updated when the conflict target already exists.
    /// </summary>
    /// <param name="memberSelectors">Simple member selectors for the update columns.</param>
    /// <returns>The current command builder.</returns>
    public InsertOrUpdateManyCommandBuilder<T> Update(params Expression<Func<T, object?>>[] memberSelectors)
    {
        _updateColumns = ConflictInsertBuilderSupport<T>.GetColumns(memberSelectors);
        return this;
    }

    /// <summary>
    /// Renders the per-entity SQL commands without executing them.
    /// </summary>
    /// <returns>The rendered SQL commands. Empty entity collections return an empty list.</returns>
    public IReadOnlyList<RenderedSqlCommand> ToCommands()
    {
        if (_entities.Length == 0)
        {
            return [];
        }

        var conflictColumns = ConflictInsertBuilderSupport<T>.ResolveConflictColumns(_conflictColumns);
        var updateColumns = ConflictInsertBuilderSupport<T>.ResolveUpdateColumns(_updateColumns, conflictColumns);
        var renderer = new ConflictInsertSqlRenderer(_options.Dialect);
        return _entities
            .Select(entity => renderer.RenderTemplateCommand(_table, ConflictInsertBehavior.Update, ModelMetadata<T>.InsertColumns, conflictColumns, updateColumns, entity!))
            .ToArray();
    }

    /// <summary>
    /// Executes the INSERT command once for each entity through Dapper and returns the affected row count.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The affected row count returned by Dapper.</returns>
    public int Execute(Db4NetExecutionOptions? options = null)
    {
        if (_entities.Length == 0)
        {
            return 0;
        }

        var conflictColumns = ConflictInsertBuilderSupport<T>.ResolveConflictColumns(_conflictColumns);
        var updateColumns = ConflictInsertBuilderSupport<T>.ResolveUpdateColumns(_updateColumns, conflictColumns);
        var sql = new ConflictInsertSqlRenderer(_options.Dialect)
            .RenderTemplate(_table, ConflictInsertBehavior.Update, ModelMetadata<T>.InsertColumns, conflictColumns, updateColumns);
        return _executor.Execute(sql, _entities, options);
    }

    /// <summary>
    /// Asynchronously executes the INSERT command once for each entity through Dapper and returns the affected row count.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The affected row count returned by Dapper.</returns>
    public Task<int> ExecuteAsync(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_entities.Length == 0)
        {
            return Task.FromResult(0);
        }

        var conflictColumns = ConflictInsertBuilderSupport<T>.ResolveConflictColumns(_conflictColumns);
        var updateColumns = ConflictInsertBuilderSupport<T>.ResolveUpdateColumns(_updateColumns, conflictColumns);
        var sql = new ConflictInsertSqlRenderer(_options.Dialect)
            .RenderTemplate(_table, ConflictInsertBehavior.Update, ModelMetadata<T>.InsertColumns, conflictColumns, updateColumns);
        return _executor.ExecuteAsync(sql, _entities, options, cancellationToken);
    }
}
