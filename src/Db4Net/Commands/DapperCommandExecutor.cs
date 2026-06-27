using System.Data;
using System.Threading;
using Dapper;

namespace Db4Net.Commands;

internal sealed class DapperCommandExecutor
{
    private readonly IDbConnection? _connection;

    public DapperCommandExecutor(IDbConnection? connection)
    {
        _connection = connection;
    }

    public int Execute(RenderedSqlCommand command, Db4NetExecutionOptions? options = null)
    {
        return Execute(command.Sql, command.Parameters, options);
    }

    public Task<int> ExecuteAsync(RenderedSqlCommand command, Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(command.Sql, command.Parameters, options, cancellationToken);
    }

    public int Execute(string sql, object? parameters, Db4NetExecutionOptions? options = null)
    {
        return RequireConnection().Execute(CreateDapperCommand(sql, parameters, options));
    }

    public Task<int> ExecuteAsync(string sql, object? parameters, Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RequireConnection().ExecuteAsync(CreateDapperCommand(sql, parameters, options, cancellationToken));
    }

    private static CommandDefinition CreateDapperCommand(
        string sql,
        object? parameters,
        Db4NetExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return new CommandDefinition(
            sql,
            parameters,
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
