using System.Data;
using System.Threading;
using Dapper;

namespace Db4Net.Commands;

internal sealed class DapperCommandExecutor
{
    private readonly IDbConnection? _connection;
    private readonly Db4NetExecutionOptions? _executionOptions;

    public DapperCommandExecutor(IDbConnection? connection, Db4NetExecutionOptions? executionOptions = null)
    {
        _connection = connection;
        _executionOptions = executionOptions;
    }

    public int Execute(RenderedSqlCommand command, Db4NetExecutionOptions? options = null)
    {
        return Execute(command.Sql, command.Parameters, options);
    }

    public Task<int> ExecuteAsync(RenderedSqlCommand command, Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(command.Sql, command.Parameters, options, cancellationToken);
    }

    public TResult ExecuteScalar<TResult>(RenderedSqlCommand command, Db4NetExecutionOptions? options = null)
    {
        var executionOptions = ResolveOptions(options);
        executionOptions?.Validate();
        return RequireConnection().ExecuteScalar<TResult>(CreateDapperCommand(command.Sql, command.Parameters, executionOptions))!;
    }

    public Task<TResult> ExecuteScalarAsync<TResult>(RenderedSqlCommand command, Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var executionOptions = ResolveOptions(options);
        executionOptions?.Validate();
        return RequireConnection().ExecuteScalarAsync<TResult>(CreateDapperCommand(command.Sql, command.Parameters, executionOptions, cancellationToken))!;
    }

    public int Execute(string sql, object? parameters, Db4NetExecutionOptions? options = null)
    {
        var executionOptions = ResolveOptions(options);
        executionOptions?.Validate();
        return RequireConnection().Execute(CreateDapperCommand(sql, parameters, executionOptions));
    }

    public Task<int> ExecuteAsync(string sql, object? parameters, Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var executionOptions = ResolveOptions(options);
        executionOptions?.Validate();
        return RequireConnection().ExecuteAsync(CreateDapperCommand(sql, parameters, executionOptions, cancellationToken));
    }

    private Db4NetExecutionOptions? ResolveOptions(Db4NetExecutionOptions? options)
    {
        return Db4NetExecutionOptions.Merge(_executionOptions, options);
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
