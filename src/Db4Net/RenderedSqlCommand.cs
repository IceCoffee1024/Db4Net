using Dapper;

namespace Db4Net;

/// <summary>
/// Represents SQL rendered by Db4Net and the parameters to pass to Dapper.
/// </summary>
public sealed class RenderedSqlCommand
{
    /// <summary>
    /// Initializes a rendered command definition.
    /// </summary>
    /// <param name="sql">The SQL text.</param>
    /// <param name="parameters">The Dapper parameters for the SQL text.</param>
    public RenderedSqlCommand(string sql, DynamicParameters parameters)
    {
        Sql = sql;
        Parameters = parameters;
    }

    /// <summary>
    /// Gets the rendered SQL text.
    /// </summary>
    public string Sql { get; }

    /// <summary>
    /// Gets the Dapper parameters associated with <see cref="Sql"/>.
    /// </summary>
    public DynamicParameters Parameters { get; }
}
