using System.ComponentModel.DataAnnotations.Schema;
using Npgsql;

namespace Db4Net.Tests;

public sealed class PostgreSqlIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable = "DB4NET_POSTGRESQL_CONNECTION_STRING";

    [SkippableFact]
    public async Task Query_single_or_default_executes_parameterized_sql()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("postgresql", "users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE "{table}" ("Id" integer PRIMARY KEY, "Name" text NOT NULL);
                INSERT INTO "{table}" ("Id", "Name") VALUES (1, 'Alice'), (2, 'Bob');
                """);

            var user = await connection
                .UseDb4Net(Db4NetOptions.PostgreSql)
                .SelectFrom<User>(table)
                .Where(u => u.Id, Op.Eq, 2)
                .QuerySingleOrDefaultAsync<User>();

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
        var table = ExternalDatabaseTestSupport.CreateTableName("postgresql", "mapped_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE "{table}" ("Id" integer PRIMARY KEY, "display_name" text NOT NULL);
                INSERT INTO "{table}" ("Id", "display_name") VALUES (1, 'Alice');
                """);

            var user = await connection
                .UseDb4Net(Db4NetOptions.PostgreSql)
                .Select("DisplayName")
                .From<MappedUser>(table)
                .QuerySingleOrDefaultAsync<MappedUser>();

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
        var table = ExternalDatabaseTestSupport.CreateTableName("postgresql", "paged_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE "{table}" ("Id" integer PRIMARY KEY, "Name" text NOT NULL);
                INSERT INTO "{table}" ("Id", "Name") VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Charlie');
                """);

            var users = (await connection
                .UseDb4Net(Db4NetOptions.PostgreSql)
                .SelectFrom<User>(table)
                .OrderBy(u => u.Id)
                .Offset(1)
                .Limit(1)
                .QueryAsync<User>())
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

    private static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ExternalDatabaseTestSupport.GetRequiredConnectionString(ConnectionStringEnvironmentVariable));
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static Task DropTableIfExistsAsync(NpgsqlConnection connection, string table)
    {
        return ExecuteAsync(connection, $"""DROP TABLE IF EXISTS "{table}";""");
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
