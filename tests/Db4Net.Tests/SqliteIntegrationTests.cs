using Db4Net;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations.Schema;

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
    public void Execute_runs_rendered_command_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var affected = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .Execute();

        Assert.Equal(-1, affected);
    }

    [Fact]
    public async Task Execute_async_runs_rendered_command_with_dapper()
    {
        await using var connection = CreateOpenConnection();

        var affected = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .ExecuteAsync();

        Assert.Equal(-1, affected);
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
