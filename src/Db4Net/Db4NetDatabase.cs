using System.Data;
using Db4Net.Metadata;

namespace Db4Net;

/// <summary>
/// Entry point for creating Db4Net query and command builders.
/// </summary>
public sealed partial class Db4NetDatabase
{
    private readonly IDbConnection? _connection;
    private readonly Db4NetExecutionOptions? _executionOptions;
    private readonly Db4NetOptions _options;

    private Db4NetDatabase(Db4NetOptions options, IDbConnection? connection = null, Db4NetExecutionOptions? executionOptions = null)
    {
        _options = options;
        _connection = connection;
        _executionOptions = executionOptions;
    }

    /// <summary>
    /// Creates a Db4Net facade that can build SQL without executing it.
    /// </summary>
    /// <param name="options">The SQL generation options to use.</param>
    /// <returns>A Db4Net facade for building queries.</returns>
    public static Db4NetDatabase Create(Db4NetOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        return new Db4NetDatabase(options);
    }

    internal static Db4NetDatabase Create(Db4NetOptions options, IDbConnection connection)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(connection);
        return new Db4NetDatabase(options, connection);
    }

    /// <summary>
    /// Creates a new facade that applies default execution options to terminal methods.
    /// </summary>
    /// <param name="executionOptions">Default Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>A Db4Net facade with default execution options.</returns>
    public Db4NetDatabase WithExecutionOptions(Db4NetExecutionOptions executionOptions)
    {
        ThrowHelper.ThrowIfNull(executionOptions);
        return new Db4NetDatabase(_options, _connection, Db4NetExecutionOptions.Merge(_executionOptions, executionOptions));
    }

    /// <summary>
    /// Creates a new facade that applies an existing transaction to terminal methods.
    /// </summary>
    /// <param name="transaction">The transaction passed to Dapper execution.</param>
    /// <returns>A Db4Net facade with the transaction as a default execution option.</returns>
    public Db4NetDatabase WithTransaction(IDbTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(transaction);
        return WithExecutionOptions(new Db4NetExecutionOptions { Transaction = transaction });
    }

    private static void EnsureEntityType<T>(string? sequenceAlternative = null)
    {
        var type = typeof(T);
        if (sequenceAlternative is not null && ModelMetadataProvider.IsSequenceType(type))
        {
            throw new ArgumentException($"Type '{ModelMetadataProvider.GetDisplayName(type)}' is a sequence type. Use {sequenceAlternative}(...) for multiple entities.");
        }

        ModelMetadataProvider.EnsureMappedModelType(type);
    }

    private IDbConnection RequireConnection()
    {
        return _connection ?? throw new InvalidOperationException("Dapper execution requires an IDbConnection. Use connection.UseDb4Net(options) to create the database facade.");
    }
}
