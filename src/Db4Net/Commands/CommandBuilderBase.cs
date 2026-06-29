using System.Data;
using System.Threading;

namespace Db4Net.Commands;

/// <summary>
/// Provides common execution behavior for Db4Net command builders.
/// </summary>
public abstract class CommandBuilderBase
{
    private readonly DapperCommandExecutor _executor;

    internal CommandBuilderBase(IDbConnection? connection, Db4NetExecutionOptions? executionOptions = null)
    {
        _executor = new DapperCommandExecutor(connection, executionOptions);
    }

    /// <summary>
    /// Renders the SQL text and parameters without executing the command.
    /// </summary>
    /// <returns>The rendered SQL command definition.</returns>
    public abstract RenderedSqlCommand ToCommand();

    /// <summary>
    /// Executes the rendered command through Dapper and returns the affected row count.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The affected row count returned by Dapper.</returns>
    public int Execute(Db4NetExecutionOptions? options = null)
    {
        return _executor.Execute(ToCommand(), options);
    }

    /// <summary>
    /// Asynchronously executes the rendered command through Dapper and returns the affected row count.
    /// </summary>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The affected row count returned by Dapper.</returns>
    public Task<int> ExecuteAsync(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _executor.ExecuteAsync(ToCommand(), options, cancellationToken);
    }

    /// <summary>
    /// Executes the rendered command through Dapper and returns the first scalar value.
    /// </summary>
    /// <typeparam name="TResult">The scalar result type returned by the database.</typeparam>
    /// <param name="command">The rendered SQL command to execute.</param>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The scalar value returned by Dapper.</returns>
    protected TResult ExecuteScalar<TResult>(RenderedSqlCommand command, Db4NetExecutionOptions? options = null)
    {
        return _executor.ExecuteScalar<TResult>(command, options);
    }

    /// <summary>
    /// Asynchronously executes the rendered command through Dapper and returns the first scalar value.
    /// </summary>
    /// <typeparam name="TResult">The scalar result type returned by the database.</typeparam>
    /// <param name="command">The rendered SQL command to execute.</param>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The scalar value returned by Dapper.</returns>
    protected Task<TResult> ExecuteScalarAsync<TResult>(
        RenderedSqlCommand command,
        Db4NetExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return _executor.ExecuteScalarAsync<TResult>(command, options, cancellationToken);
    }
}
