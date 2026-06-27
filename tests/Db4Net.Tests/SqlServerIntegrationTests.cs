using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.SqlClient;

namespace Db4Net.Tests;

public sealed class SqlServerIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable = "DB4NET_SQLSERVER_CONNECTION_STRING";

    [SkippableFact]
    public async Task Query_single_or_default_executes_parameterized_sql()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("sqlserver", "users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE [{table}] ([Id] int NOT NULL PRIMARY KEY, [Name] nvarchar(100) NOT NULL);
                INSERT INTO [{table}] ([Id], [Name]) VALUES (1, N'Alice'), (2, N'Bob');
                """);

            var user = await connection
                .UseDb4Net(Db4NetOptions.SqlServer)
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
        var table = ExternalDatabaseTestSupport.CreateTableName("sqlserver", "mapped_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE [{table}] ([Id] int NOT NULL PRIMARY KEY, [display_name] nvarchar(100) NOT NULL);
                INSERT INTO [{table}] ([Id], [display_name]) VALUES (1, N'Alice');
                """);

            var user = await connection
                .UseDb4Net(Db4NetOptions.SqlServer)
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
        var table = ExternalDatabaseTestSupport.CreateTableName("sqlserver", "paged_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE [{table}] ([Id] int NOT NULL PRIMARY KEY, [Name] nvarchar(100) NOT NULL);
                INSERT INTO [{table}] ([Id], [Name]) VALUES (1, N'Alice'), (2, N'Bob'), (3, N'Charlie');
                """);

            var users = (await connection
                .UseDb4Net(Db4NetOptions.SqlServer)
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

    [SkippableFact]
    public async Task Conflict_insert_conveniences_execute()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("sqlserver", "conflict_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE [{table}] ([Id] int NOT NULL PRIMARY KEY, [Name] nvarchar(100) NOT NULL);
                INSERT INTO [{table}] ([Id], [Name]) VALUES (1, N'Alice'), (2, N'Bob');
                """);
            var db = connection.UseDb4Net(Db4NetOptions.SqlServer);

            db.InsertOrIgnore(new User { Id = 1, Name = "Ignored" }, table).Execute();
            db.InsertOrUpdate(new User { Id = 2, Name = "Bobby" }, table).Execute();
            db.InsertOrUpdate(new User { Id = 3, Name = "Charlie" }, table).Execute();

            var users = db
                .SelectFrom<User>(table)
                .OrderBy(u => u.Id)
                .Query()
                .ToList();

            Assert.Collection(
                users,
                user => Assert.Equal("Alice", user.Name),
                user => Assert.Equal("Bobby", user.Name),
                user => Assert.Equal("Charlie", user.Name));
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    [SkippableFact]
    public async Task Conflict_insert_many_conveniences_execute()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("sqlserver", "conflict_many");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE [{table}] ([Id] int NOT NULL PRIMARY KEY, [Name] nvarchar(100) NOT NULL);
                INSERT INTO [{table}] ([Id], [Name]) VALUES (1, N'Alice'), (2, N'Bob');
                """);
            var db = connection.UseDb4Net(Db4NetOptions.SqlServer);

            db.InsertOrIgnoreMany(
            [
                new User { Id = 1, Name = "Ignored" },
                new User { Id = 3, Name = "Charlie" },
            ], table).Execute();
            db.InsertOrUpdateMany(
            [
                new User { Id = 2, Name = "Bobby" },
                new User { Id = 4, Name = "Dana" },
            ], table).Execute();

            var users = db
                .SelectFrom<User>(table)
                .OrderBy(u => u.Id)
                .Query()
                .ToList();

            Assert.Collection(
                users,
                user => Assert.Equal("Alice", user.Name),
                user => Assert.Equal("Bobby", user.Name),
                user => Assert.Equal("Charlie", user.Name),
                user => Assert.Equal("Dana", user.Name));
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    [SkippableFact]
    public async Task Conflict_insert_can_override_table_and_conflict_target()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("sqlserver", "unique_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE [{table}] ([Id] int NOT NULL PRIMARY KEY, [Email] nvarchar(200) NOT NULL UNIQUE, [Name] nvarchar(100) NOT NULL);
                INSERT INTO [{table}] ([Id], [Email], [Name]) VALUES (1, N'alice@example.com', N'Alice');
                """);
            var db = connection.UseDb4Net(Db4NetOptions.SqlServer);

            db.InsertOrUpdate(new UniqueUser { Id = 2, Email = "alice@example.com", Name = "Alicia" }, table)
                .OnConflict(u => u.Email)
                .Update(u => u.Name)
                .Execute();

            var user = db
                .SelectFrom<UniqueUser>(table)
                .Where(u => u.Email, Op.Eq, "alice@example.com")
                .QuerySingleOrDefault();

            Assert.NotNull(user);
            Assert.Equal(1, user.Id);
            Assert.Equal("Alicia", user.Name);
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    private static async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(ExternalDatabaseTestSupport.GetRequiredConnectionString(ConnectionStringEnvironmentVariable));
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static Task DropTableIfExistsAsync(SqlConnection connection, string table)
    {
        return ExecuteAsync(connection, $"DROP TABLE IF EXISTS [{table}];");
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

    [Table("unique_users")]
    private sealed class UniqueUser
    {
        public int Id { get; set; }

        public string Email { get; set; } = "";

        public string Name { get; set; } = "";
    }
}
