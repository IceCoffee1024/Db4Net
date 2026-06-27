using System.Data;
using System.Threading;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Executes INSERT commands for multiple mapped CLR model instances.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class InsertManyCommandBuilder<T>
{
    private readonly DapperCommandExecutor _executor;
    private readonly T[] _entities;
    private readonly Db4NetOptions _options;
    private readonly string _table;

    internal InsertManyCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table, IEnumerable<T> entities)
    {
        _options = options;
        _executor = new DapperCommandExecutor(connection);
        _table = table;
        _entities = ManyCommandBuilderSupport<T>.Materialize(entities);
    }

    /// <summary>
    /// Renders the per-entity SQL commands without executing them.
    /// </summary>
    /// <returns>The rendered SQL commands. Empty entity collections return an empty list.</returns>
    public IReadOnlyList<RenderedSqlCommand> ToCommands()
    {
        var renderer = new ManyCommandSqlRenderer(_options.Dialect);
        return _entities.Select(entity => renderer.RenderInsert(_table, entity)).ToArray();
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

        var sql = new ManyCommandSqlRenderer(_options.Dialect).RenderInsertTemplate<T>(_table);
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

        var sql = new ManyCommandSqlRenderer(_options.Dialect).RenderInsertTemplate<T>(_table);
        return _executor.ExecuteAsync(sql, _entities, options, cancellationToken);
    }
}
