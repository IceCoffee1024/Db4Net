using System.Data;
using System.Threading;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Builds and executes a single-row INSERT command that returns one mapped key value.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class InsertReturnKeyCommandBuilder<T>
{
    private readonly DapperCommandExecutor _executor;
    private readonly InsertCommandModel _model;
    private readonly Db4NetOptions _options;

    internal InsertReturnKeyCommandBuilder(
        Db4NetOptions options,
        IDbConnection? connection,
        InsertCommandModel model,
        Db4NetExecutionOptions? executionOptions = null)
    {
        _options = options;
        _executor = new DapperCommandExecutor(connection, executionOptions);
        _model = model;
    }

    /// <summary>
    /// Renders the SQL text and parameters without executing the insert command.
    /// </summary>
    /// <returns>The rendered SQL command definition.</returns>
    public RenderedSqlCommand ToCommand()
    {
        return new CommandSqlRenderer(_options.Dialect).Render(_model);
    }

    /// <summary>
    /// Executes the insert command through Dapper and returns the selected key value.
    /// </summary>
    /// <typeparam name="TResult">The scalar key result type returned by the database.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The key value returned by the database.</returns>
    public TResult Execute<TResult>(Db4NetExecutionOptions? options = null)
    {
        return _executor.ExecuteScalar<TResult>(ToCommand(), options);
    }

    /// <summary>
    /// Asynchronously executes the insert command through Dapper and returns the selected key value.
    /// </summary>
    /// <typeparam name="TResult">The scalar key result type returned by the database.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The key value returned by the database.</returns>
    public Task<TResult> ExecuteAsync<TResult>(
        Db4NetExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return _executor.ExecuteScalarAsync<TResult>(ToCommand(), options, cancellationToken);
    }
}
