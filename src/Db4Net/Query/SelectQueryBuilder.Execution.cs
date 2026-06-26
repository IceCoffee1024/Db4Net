using System.Data;
using System.Threading;
using Dapper;

namespace Db4Net.Query;

public partial class SelectQueryBuilder
{
    /// <summary>
    /// Executes the query through Dapper and returns all rows.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The materialized rows.</returns>
    public IEnumerable<TResult> Query<TResult>(Db4NetExecutionOptions? options = null)
    {
        return RequireConnection().Query<TResult>(CreateDapperCommand(options));
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns all rows.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The materialized rows.</returns>
    public Task<IEnumerable<TResult>> QueryAsync<TResult>(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RequireConnection().QueryAsync<TResult>(CreateDapperCommand(options, cancellationToken));
    }

    /// <summary>
    /// Executes the query through Dapper and returns the first row, or the default value if no row exists.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The first materialized row, or the default value.</returns>
    public TResult? QueryFirstOrDefault<TResult>(Db4NetExecutionOptions? options = null)
    {
        return RequireConnection().QueryFirstOrDefault<TResult>(CreateDapperCommand(options));
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns the first row, or the default value if no row exists.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The first materialized row, or the default value.</returns>
    public Task<TResult?> QueryFirstOrDefaultAsync<TResult>(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RequireConnection().QueryFirstOrDefaultAsync<TResult>(CreateDapperCommand(options, cancellationToken));
    }

    /// <summary>
    /// Executes the query through Dapper and returns a single row, or the default value if no row exists.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The single materialized row, or the default value.</returns>
    public TResult? QuerySingleOrDefault<TResult>(Db4NetExecutionOptions? options = null)
    {
        return RequireConnection().QuerySingleOrDefault<TResult>(CreateDapperCommand(options));
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns a single row, or the default value if no row exists.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The single materialized row, or the default value.</returns>
    public Task<TResult?> QuerySingleOrDefaultAsync<TResult>(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RequireConnection().QuerySingleOrDefaultAsync<TResult>(CreateDapperCommand(options, cancellationToken));
    }

    private CommandDefinition CreateDapperCommand(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var command = ToCommand();
        return new CommandDefinition(
            command.Sql,
            command.Parameters,
            options?.Transaction,
            options?.CommandTimeout,
            options?.CommandType,
            cancellationToken: cancellationToken);
    }

    private IDbConnection RequireConnection()
    {
        return _connection ?? throw new InvalidOperationException("Dapper execution requires an IDbConnection. Use connection.UseDb4Net(options) to create the database facade.");
    }

}
