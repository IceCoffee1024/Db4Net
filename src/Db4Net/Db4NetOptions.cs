using Db4Net.Dialects;

namespace Db4Net;

/// <summary>
/// Configures Db4Net SQL generation behavior.
/// </summary>
public sealed class Db4NetOptions
{
    private Db4NetOptions(ISqlDialect dialect)
    {
        Dialect = dialect;
    }

    internal ISqlDialect Dialect { get; }

    /// <summary>
    /// Uses SQL Server identifier quoting and paging syntax.
    /// </summary>
    public static Db4NetOptions SqlServer { get; } = new(new SqlServerDialect());

    /// <summary>
    /// Uses SQLite identifier quoting and paging syntax.
    /// </summary>
    public static Db4NetOptions Sqlite { get; } = new(new SqliteDialect());

    /// <summary>
    /// Uses PostgreSQL identifier quoting and paging syntax.
    /// </summary>
    public static Db4NetOptions PostgreSql { get; } = new(new PostgreSqlDialect());

    /// <summary>
    /// Uses MySQL identifier quoting and paging syntax.
    /// </summary>
    public static Db4NetOptions MySql { get; } = new(new MySqlDialect());
}
