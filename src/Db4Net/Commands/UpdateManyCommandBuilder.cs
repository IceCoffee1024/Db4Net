using System.Data;
using System.Threading;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Executes UPDATE commands for multiple mapped CLR model instances using key properties for each WHERE clause.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class UpdateManyCommandBuilder<T>
{
    private readonly DapperCommandExecutor _executor;
    private readonly T[] _entities;
    private readonly Db4NetOptions _options;
    private readonly string _table;

    internal UpdateManyCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table, IEnumerable<T> entities)
    {
        _options = options;
        _executor = new DapperCommandExecutor(connection);
        _table = table;
        _entities = ManyCommandBuilderSupport<T>.Materialize(entities);

        if (_entities.Length > 0)
        {
            ManyCommandBuilderSupport<T>.EnsureNonDefaultKeyValues(_entities);
        }
    }

    /// <summary>
    /// Renders the per-entity SQL commands without executing them.
    /// </summary>
    /// <returns>The rendered SQL commands. Empty entity collections return an empty list.</returns>
    public IReadOnlyList<RenderedSqlCommand> ToCommands()
    {
        var renderer = new ManyCommandSqlRenderer(_options.Dialect);
        return _entities.Select(entity => renderer.RenderUpdate(_table, entity)).ToArray();
    }

    /// <summary>
    /// Executes the UPDATE command once for each entity through Dapper and returns the affected row count.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The affected row count returned by Dapper.</returns>
    public int Execute(Db4NetExecutionOptions? options = null)
    {
        if (_entities.Length == 0)
        {
            return 0;
        }

        var sql = new ManyCommandSqlRenderer(_options.Dialect).RenderUpdateTemplate<T>(_table);
        return _executor.Execute(sql, _entities, options);
    }

    /// <summary>
    /// Asynchronously executes the UPDATE command once for each entity through Dapper and returns the affected row count.
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

        var sql = new ManyCommandSqlRenderer(_options.Dialect).RenderUpdateTemplate<T>(_table);
        return _executor.ExecuteAsync(sql, _entities, options, cancellationToken);
    }
}
