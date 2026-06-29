using System.ComponentModel.DataAnnotations;
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

    [SkippableFact]
    public async Task Conflict_insert_conveniences_execute()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("mysql", "conflict_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE `{table}` (`Id` int NOT NULL PRIMARY KEY, `Name` varchar(100) NOT NULL);
                INSERT INTO `{table}` (`Id`, `Name`) VALUES (1, 'Alice'), (2, 'Bob');
                """);
            var db = connection.UseDb4Net(Db4NetOptions.MySql);

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
        var table = ExternalDatabaseTestSupport.CreateTableName("mysql", "conflict_many");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE `{table}` (`Id` int NOT NULL PRIMARY KEY, `Name` varchar(100) NOT NULL);
                INSERT INTO `{table}` (`Id`, `Name`) VALUES (1, 'Alice'), (2, 'Bob');
                """);
            var db = connection.UseDb4Net(Db4NetOptions.MySql);

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
        var table = ExternalDatabaseTestSupport.CreateTableName("mysql", "unique_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE `{table}` (`Id` int NOT NULL PRIMARY KEY, `Email` varchar(200) NOT NULL UNIQUE, `Name` varchar(100) NOT NULL);
                INSERT INTO `{table}` (`Id`, `Email`, `Name`) VALUES (1, 'alice@example.com', 'Alice');
                """);
            var db = connection.UseDb4Net(Db4NetOptions.MySql);

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

    [SkippableFact]
    public async Task Insert_execute_return_key_returns_generated_key()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("mysql", "generated_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE `{table}` (`Id` int NOT NULL AUTO_INCREMENT PRIMARY KEY, `Name` varchar(100) NOT NULL);
                """);
            var db = connection.UseDb4Net(Db4NetOptions.MySql);

            var id = await db
                .Insert(new GeneratedKeyUser { Name = "Alice" }, table)
                .ExecuteReturnKeyAsync<long>();

            var user = await db
                .SelectFrom<GeneratedKeyUser>(table)
                .Where(u => u.Id, Op.Eq, id)
                .QuerySingleOrDefaultAsync();

            Assert.Equal(1L, id);
            Assert.NotNull(user);
            Assert.Equal("Alice", user.Name);
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    [SkippableFact]
    public async Task Insert_return_key_builder_execute_returns_explicit_key()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("mysql", "explicit_key_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE `{table}` (`Id` int NOT NULL PRIMARY KEY, `Name` varchar(100) NOT NULL);
                """);
            var db = connection.UseDb4Net(Db4NetOptions.MySql);

            var id = db
                .InsertInto<User>(table)
                .Values(new User { Id = 7, Name = "Alice" })
                .ReturnKey(u => u.Id)
                .Execute<int>();

            var user = await db
                .SelectFrom<User>(table)
                .Where(u => u.Id, Op.Eq, id)
                .QuerySingleOrDefaultAsync();

            Assert.Equal(7, id);
            Assert.NotNull(user);
            Assert.Equal("Alice", user.Name);
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

    [Table("unique_users")]
    private sealed class UniqueUser
    {
        public int Id { get; set; }

        public string Email { get; set; } = "";

        public string Name { get; set; } = "";
    }

    [Table("generated_users")]
    private sealed class GeneratedKeyUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; } = "";
    }
}
