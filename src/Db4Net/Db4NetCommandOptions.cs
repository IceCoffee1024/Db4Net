using System.Data;

namespace Db4Net;

/// <summary>
/// Configures options used when executing a rendered Db4Net command through Dapper.
/// </summary>
public sealed class Db4NetCommandOptions
{
    /// <summary>
    /// Gets or initializes the transaction used by Dapper.
    /// </summary>
    public IDbTransaction? Transaction { get; init; }

    /// <summary>
    /// Gets or initializes the command timeout in seconds.
    /// </summary>
    public int? CommandTimeout { get; init; }

    /// <summary>
    /// Gets or initializes the database command type.
    /// </summary>
    public CommandType? CommandType { get; init; }
}
