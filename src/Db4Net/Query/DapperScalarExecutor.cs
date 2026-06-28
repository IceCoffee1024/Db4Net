using System.Data;
using System.Threading;
using Dapper;

namespace Db4Net.Query;

internal sealed class DapperScalarExecutor
{
    private readonly IDbConnection? _connection;
    private readonly Db4NetExecutionOptions? _executionOptions;

    public DapperScalarExecutor(IDbConnection? connection, Db4NetExecutionOptions? executionOptions = null)
    {
        _connection = connection;
        _executionOptions = executionOptions;
    }

    public TResult Execute<TResult>(RenderedSqlCommand command, Db4NetExecutionOptions? options = null)
    {
        return RequireConnection().ExecuteScalar<TResult>(CreateDapperCommand(command, options))!;
    }

    public Task<TResult> ExecuteAsync<TResult>(
        RenderedSqlCommand command,
        Db4NetExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RequireConnection().ExecuteScalarAsync<TResult>(CreateDapperCommand(command, options, cancellationToken))!;
    }

    private CommandDefinition CreateDapperCommand(
        RenderedSqlCommand command,
        Db4NetExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var executionOptions = Db4NetExecutionOptions.Merge(_executionOptions, options);
        executionOptions?.Validate();
        return new CommandDefinition(
            command.Sql,
            command.Parameters,
            executionOptions?.Transaction,
            executionOptions?.CommandTimeout,
            executionOptions?.CommandType,
            cancellationToken: cancellationToken);
    }

    private IDbConnection RequireConnection()
    {
        return _connection ?? throw new InvalidOperationException("Dapper execution requires an IDbConnection. Use connection.UseDb4Net(options) to create the database facade.");
    }
}
