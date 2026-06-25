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

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }
}
