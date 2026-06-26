using Db4Net;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace Db4Net.Tests;

public sealed class SqliteIntegrationTests
{
    [Fact]
    public void Query_single_or_default_executes_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 2)
            .QuerySingleOrDefault<User>();

        Assert.NotNull(user);
        Assert.Equal(2, user.Id);
        Assert.Equal("Bob", user.Name);
    }

    [Fact]
    public async Task Query_single_or_default_async_executes_parameterized_sql_with_dapper()
    {
        await using var connection = CreateOpenConnection();

        var user = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 2)
            .QuerySingleOrDefaultAsync<User>();

        Assert.NotNull(user);
        Assert.Equal(2, user.Id);
        Assert.Equal("Bob", user.Name);
    }

    [Fact]
    public void Query_executes_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var users = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Gt, 0)
            .OrderBy(u => u.Id)
            .Query<User>()
            .ToList();

        Assert.Collection(
            users,
            user => Assert.Equal("Alice", user.Name),
            user => Assert.Equal("Bob", user.Name));
    }

    [Fact]
    public async Task Query_async_executes_parameterized_sql_with_dapper()
    {
        await using var connection = CreateOpenConnection();

        var users = (await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Gt, 0)
            .OrderBy(u => u.Id)
            .QueryAsync<User>())
            .ToList();

        Assert.Collection(
            users,
            user => Assert.Equal("Alice", user.Name),
            user => Assert.Equal("Bob", user.Name));
    }

    [Fact]
    public void Query_uses_transaction_from_command_options()
    {
        using var connection = CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "insert into Users (Id, Name) values (3, 'Charlie');";
        insert.ExecuteNonQuery();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault<User>(new Db4NetCommandOptions { Transaction = transaction });

        Assert.NotNull(user);
        Assert.Equal("Charlie", user.Name);

        transaction.Rollback();

        var afterRollback = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault<User>();

        Assert.Null(afterRollback);
    }

    [Fact]
    public async Task Query_async_uses_cancellation_token()
    {
        await using var connection = CreateOpenConnection();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connection
                .UseDb4Net(Db4NetOptions.Sqlite)
                .SelectFrom<User>()
                .QueryAsync<User>(cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task Query_async_accepts_command_options_and_cancellation_token()
    {
        await using var connection = CreateOpenConnection();
        await using var transaction = await connection.BeginTransactionAsync();

        var user = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .QuerySingleOrDefaultAsync<User>(
                new Db4NetCommandOptions { Transaction = transaction },
                CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void Query_first_or_default_returns_default_when_no_row_exists()
    {
        using var connection = CreateOpenConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 99)
            .QueryFirstOrDefault<User>();

        Assert.Null(user);
    }

    [Fact]
    public async Task Query_first_or_default_async_returns_default_when_no_row_exists()
    {
        await using var connection = CreateOpenConnection();

        var user = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 99)
            .QueryFirstOrDefaultAsync<User>();

        Assert.Null(user);
    }

    [Fact]
    public void Insert_command_executes_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var affected = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .InsertInto<User>()
            .Value(u => u.Id, 3)
            .Value(u => u.Name, "Charlie")
            .Execute();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault<User>();

        Assert.Equal(1, affected);
        Assert.NotNull(user);
        Assert.Equal("Charlie", user.Name);
    }

    [Fact]
    public void Update_command_executes_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var affected = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .Update<User>()
            .Set(u => u.Name, "Alicia")
            .Where(u => u.Id, Op.Eq, 1)
            .Execute();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .QuerySingleOrDefault<User>();

        Assert.Equal(1, affected);
        Assert.NotNull(user);
        Assert.Equal("Alicia", user.Name);
    }

    [Fact]
    public async Task Delete_command_async_executes_parameterized_sql_with_dapper()
    {
        await using var connection = CreateOpenConnection();

        var affected = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .DeleteFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .ExecuteAsync();

        var user = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .QuerySingleOrDefaultAsync<User>();

        Assert.Equal(1, affected);
        Assert.Null(user);
    }

    [Fact]
    public void Command_table_overrides_execute_against_explicit_tables_with_model_mapping()
    {
        using var connection = CreateOpenShardedConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var inserted = db
            .InsertInto<MappedUser>("app_users_staging")
            .Value(u => u.Id, 1)
            .Value(u => u.DisplayName, "Alice")
            .Execute();

        var updated = db
            .Update<MappedUser>("app_users_staging")
            .Set(u => u.DisplayName, "Alicia")
            .Where(u => u.Id, Op.Eq, 1)
            .Execute();

        var user = db
            .SelectFrom<MappedUser>("app_users_staging")
            .QuerySingleOrDefault<MappedUser>();

        var deleted = db
            .DeleteFrom<MappedUser>("app_users_staging")
            .Where(u => u.Id, Op.Eq, 1)
            .Execute();

        var afterDelete = db
            .SelectFrom<MappedUser>("app_users_staging")
            .QuerySingleOrDefault<MappedUser>();

        Assert.Equal(1, inserted);
        Assert.Equal(1, updated);
        Assert.NotNull(user);
        Assert.Equal("Alicia", user.DisplayName);
        Assert.Equal(1, deleted);
        Assert.Null(afterDelete);
    }

    [Fact]
    public void Typed_select_with_column_attribute_maps_result_to_property()
    {
        using var connection = CreateOpenMappedConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .Select<MappedUser>(u => u.DisplayName)
            .QuerySingleOrDefault<MappedUser>();

        Assert.NotNull(user);
        Assert.Equal("Alice", user.DisplayName);
    }

    [Fact]
    public void Select_from_type_with_column_attribute_maps_result_to_property()
    {
        using var connection = CreateOpenMappedConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<MappedUser>()
            .QuerySingleOrDefault<MappedUser>();

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.DisplayName);
        Assert.Equal("", user.Ignored);
    }

    [Fact]
    public void Select_from_type_excludes_not_mapped_property_even_when_table_has_matching_column()
    {
        using var connection = CreateOpenMappedConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<MappedUser>()
            .QuerySingleOrDefault<MappedUser>();

        Assert.NotNull(user);
        Assert.Equal("", user.Ignored);
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = """
            create table Users (Id integer primary key, Name text not null);
            insert into Users (Id, Name) values (1, 'Alice');
            insert into Users (Id, Name) values (2, 'Bob');
            """;
        setup.ExecuteNonQuery();
        return connection;
    }

    private static SqliteConnection CreateOpenMappedConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = """
            create table app_users (Id integer primary key, display_name text not null, Ignored text not null);
            insert into app_users (Id, display_name, Ignored) values (1, 'Alice', 'should-not-map');
            """;
        setup.ExecuteNonQuery();
        return connection;
    }

    private static SqliteConnection CreateOpenShardedConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = """
            create table app_users_staging (Id integer primary key, display_name text not null, Ignored text not null default '');
            """;
        setup.ExecuteNonQuery();
        return connection;
    }

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    [Table("app_users")]
    private sealed class MappedUser
    {
        public int Id { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; } = "";

        [NotMapped]
        public string Ignored { get; set; } = "";
    }
}
