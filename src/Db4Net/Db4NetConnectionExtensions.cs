using System.Data;

namespace Db4Net;

/// <summary>
/// Extension methods for creating Db4Net query builders from database connections.
/// </summary>
public static class Db4NetConnectionExtensions
{
    /// <summary>
    /// Creates a Db4Net facade bound to an <see cref="IDbConnection"/> so terminal methods can execute through Dapper.
    /// </summary>
    /// <param name="connection">The connection Dapper will execute against.</param>
    /// <param name="options">The SQL generation options to use.</param>
    /// <returns>A Db4Net facade for building and executing queries.</returns>
    public static Db4NetDatabase UseDb4Net(this IDbConnection connection, Db4NetOptions options)
    {
        return Db4NetDatabase.Create(options, connection);
    }
}
