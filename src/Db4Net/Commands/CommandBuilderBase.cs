using System.Data;
using System.Threading;
using Dapper;

namespace Db4Net.Commands;

/// <summary>
/// Provides common execution behavior for Db4Net command builders.
/// </summary>
public abstract class CommandBuilderBase
{
    private readonly IDbConnection? _connection;

    internal CommandBuilderBase(IDbConnection? connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Renders the SQL text and parameters without executing the command.
    /// </summary>
    /// <returns>The rendered SQL command definition.</returns>
    public abstract SqlCommandDefinition ToCommand();

    /// <summary>
    /// Executes the rendered command through Dapper and returns the affected row count.
    /// </summary>
    /// <returns>The affected row count returned by Dapper.</returns>
    public int Execute(Db4NetCommandOptions? options = null)
    {
        return RequireConnection().Execute(CreateDapperCommand(options));
    }

    /// <summary>
    /// Asynchronously executes the rendered command through Dapper and returns the affected row count.
    /// </summary>
    /// <returns>The affected row count returned by Dapper.</returns>
    public Task<int> ExecuteAsync(Db4NetCommandOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RequireConnection().ExecuteAsync(CreateDapperCommand(options, cancellationToken));
    }

    private CommandDefinition CreateDapperCommand(Db4NetCommandOptions? options = null, CancellationToken cancellationToken = default)
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
