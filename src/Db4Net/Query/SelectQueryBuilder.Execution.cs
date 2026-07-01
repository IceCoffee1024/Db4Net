using System.Data;
using System.Threading;
using Dapper;
using Db4Net.Rendering;

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
    /// Executes the query through Dapper and returns the first row.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The first materialized row.</returns>
    public TResult QueryFirst<TResult>(Db4NetExecutionOptions? options = null)
    {
        return RequireConnection().QueryFirst<TResult>(CreateDapperCommand(options));
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns the first row.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The first materialized row.</returns>
    public Task<TResult> QueryFirstAsync<TResult>(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RequireConnection().QueryFirstAsync<TResult>(CreateDapperCommand(options, cancellationToken));
    }

    /// <summary>
    /// Executes a count query and a paged row query through Dapper.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of rows per page.</param>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The paged result and total row count.</returns>
    public PagedResult<TResult> QueryPage<TResult>(
        int pageNumber,
        int pageSize,
        Db4NetExecutionOptions? options = null)
    {
        var commands = CreatePageCommands(pageNumber, pageSize);
        var connection = RequireConnection();
        var totalCount = connection.ExecuteScalar<long>(CreateDapperCommand(commands.CountCommand, options));
        var items = connection.Query<TResult>(CreateDapperCommand(commands.PageCommand, options)).ToArray();
        return new PagedResult<TResult>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Asynchronously executes a count query and a paged row query through Dapper.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of rows per page.</param>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The paged result and total row count.</returns>
    public async Task<PagedResult<TResult>> QueryPageAsync<TResult>(
        int pageNumber,
        int pageSize,
        Db4NetExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var commands = CreatePageCommands(pageNumber, pageSize);
        var connection = RequireConnection();
        var totalCount = await connection.ExecuteScalarAsync<long>(CreateDapperCommand(commands.CountCommand, options, cancellationToken)).ConfigureAwait(false);
        var items = (await connection.QueryAsync<TResult>(CreateDapperCommand(commands.PageCommand, options, cancellationToken)).ConfigureAwait(false)).ToArray();
        return new PagedResult<TResult>(items, totalCount, pageNumber, pageSize);
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
    /// Executes the query through Dapper and returns exactly one row.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The single materialized row.</returns>
    public TResult QuerySingle<TResult>(Db4NetExecutionOptions? options = null)
    {
        return RequireConnection().QuerySingle<TResult>(CreateDapperCommand(options));
    }

    /// <summary>
    /// Asynchronously executes the query through Dapper and returns exactly one row.
    /// </summary>
    /// <typeparam name="TResult">The result type Dapper should materialize.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The single materialized row.</returns>
    public Task<TResult> QuerySingleAsync<TResult>(Db4NetExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RequireConnection().QuerySingleAsync<TResult>(CreateDapperCommand(options, cancellationToken));
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
        return CreateDapperCommand(command, options, cancellationToken);
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

    private PageCommands CreatePageCommands(int pageNumber, int pageSize)
    {
        ValidatePageArguments(pageNumber, pageSize);
        if (_model.Limit is not null || _model.Offset is not null)
        {
            throw new InvalidOperationException("QueryPage applies paging itself. Do not call Limit(), Offset(), or Page() before QueryPage().");
        }

        var pageModel = _model.Clone();
        pageModel.Limit = pageSize;
        pageModel.Offset = (pageNumber - 1) * pageSize;

        return new PageCommands(
            new ScalarSqlRenderer(_options.Dialect).Render(_model.ToCountModel()),
            new SelectSqlRenderer(_options.Dialect).Render(pageModel));
    }

    private static void ValidatePageArguments(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
        }
    }

    private IDbConnection RequireConnection()
    {
        return _connection ?? throw new InvalidOperationException("Dapper execution requires an IDbConnection. Use connection.UseDb4Net(options) to create the database facade.");
    }

    private sealed record PageCommands(RenderedSqlCommand CountCommand, RenderedSqlCommand PageCommand);

}
