using System.ComponentModel.DataAnnotations.Schema;
using MySqlConnector;

namespace Db4Net.Tests;

public sealed class MySqlIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable = "DB4NET_MYSQL_CONNECTION_STRING";

    [SkippableFact]
    public async Task Query_single_or_default_executes_parameterized_sql()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("mysql", "users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE `{table}` (`Id` int NOT NULL PRIMARY KEY, `Name` varchar(100) NOT NULL);
                INSERT INTO `{table}` (`Id`, `Name`) VALUES (1, 'Alice'), (2, 'Bob');
                """);

            var user = await connection
                .UseDb4Net(Db4NetOptions.MySql)
                .SelectFrom<User>(table)
                .Where(u => u.Id, Op.Eq, 2)
                .QuerySingleOrDefaultAsync();

            Assert.NotNull(user);
            Assert.Equal(2, user.Id);
            Assert.Equal("Bob", user.Name);
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    [SkippableFact]
    public async Task String_select_with_column_attribute_maps_result_to_property()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("mysql", "mapped_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE `{table}` (`Id` int NOT NULL PRIMARY KEY, `display_name` varchar(100) NOT NULL);
                INSERT INTO `{table}` (`Id`, `display_name`) VALUES (1, 'Alice');
                """);

            var user = await connection
                .UseDb4Net(Db4NetOptions.MySql)
                .Select("DisplayName")
                .From<MappedUser>(table)
                .QuerySingleOrDefaultAsync();

            Assert.NotNull(user);
            Assert.Equal("Alice", user.DisplayName);
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    [SkippableFact]
    public async Task Query_with_ordering_and_paging_executes()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("mysql", "paged_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE `{table}` (`Id` int NOT NULL PRIMARY KEY, `Name` varchar(100) NOT NULL);
                INSERT INTO `{table}` (`Id`, `Name`) VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Charlie');
                """);

            var users = (await connection
                .UseDb4Net(Db4NetOptions.MySql)
                .SelectFrom<User>(table)
                .OrderBy(u => u.Id)
                .Offset(1)
                .Limit(1)
                .QueryAsync())
                .ToList();

            var user = Assert.Single(users);
            Assert.Equal(2, user.Id);
            Assert.Equal("Bob", user.Name);
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    private static async Task<MySqlConnection> OpenConnectionAsync()
    {
        var connection = new MySqlConnection(ExternalDatabaseTestSupport.GetRequiredConnectionString(ConnectionStringEnvironmentVariable));
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static Task DropTableIfExistsAsync(MySqlConnection connection, string table)
    {
        return ExecuteAsync(connection, $"DROP TABLE IF EXISTS `{table}`;");
    }

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    private sealed class MappedUser
    {
        public int Id { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; } = "";
    }
}
