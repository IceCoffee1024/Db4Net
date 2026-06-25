using System.ComponentModel.DataAnnotations.Schema;
using Db4Net;

namespace Db4Net.Tests;

public sealed class DialectRenderingTests
{
    [Fact]
    public void Sql_server_quotes_string_identifiers_in_select_from_where_and_order_by()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select("Id", "Name")
            .From<User>("Users")
            .Where("Name", Op.Eq, "Alice")
            .OrderByDescending("Id")
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Name] = @p0 ORDER BY [Id] DESC", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Sqlite_quotes_string_identifiers_in_select_from_where_and_order_by()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .Select("Id", "Name")
            .From<User>("Users")
            .Where("Name", Op.Eq, "Alice")
            .OrderByDescending("Id")
            .ToCommand();

        Assert.Equal("""SELECT "Id", "Name" FROM "Users" WHERE "Name" = @p0 ORDER BY "Id" DESC""", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Postgre_sql_quotes_string_identifiers_in_select_from_where_and_order_by()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.PostgreSql)
            .Select("Id", "Name")
            .From<User>("Users")
            .Where("Name", Op.Eq, "Alice")
            .OrderByDescending("Id")
            .ToCommand();

        Assert.Equal("""SELECT "Id", "Name" FROM "Users" WHERE "Name" = @p0 ORDER BY "Id" DESC""", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void My_sql_quotes_string_identifiers_in_select_from_where_and_order_by()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.MySql)
            .Select("Id", "Name")
            .From<User>("Users")
            .Where("Name", Op.Eq, "Alice")
            .OrderByDescending("Id")
            .ToCommand();

        Assert.Equal("SELECT `Id`, `Name` FROM `Users` WHERE `Name` = @p0 ORDER BY `Id` DESC", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Sql_server_quotes_mapped_columns_and_aliases()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<MappedUser>()
            .ToCommand();

        Assert.Equal("SELECT [Id], [display_name] AS [DisplayName] FROM [app_users]", command.Sql);
    }

    [Fact]
    public void Sqlite_quotes_mapped_columns_and_aliases()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectFrom<MappedUser>()
            .ToCommand();

        Assert.Equal("SELECT \"Id\", \"display_name\" AS \"DisplayName\" FROM \"app_users\"", command.Sql);
    }

    [Fact]
    public void Postgre_sql_quotes_mapped_columns_and_aliases()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.PostgreSql)
            .SelectFrom<MappedUser>()
            .ToCommand();

        Assert.Equal("SELECT \"Id\", \"display_name\" AS \"DisplayName\" FROM \"app_users\"", command.Sql);
    }

    [Fact]
    public void My_sql_quotes_mapped_columns_and_aliases()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.MySql)
            .SelectFrom<MappedUser>()
            .ToCommand();

        Assert.Equal("SELECT `Id`, `display_name` AS `DisplayName` FROM `app_users`", command.Sql);
    }

    [Fact]
    public void Sql_server_paging_uses_offset_before_limit_parameter_order()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select("Id")
            .From<User>("Users")
            .Where("Name", Op.Like, "A%")
            .OrderBy("Id")
            .Offset(20)
            .Limit(10)
            .ToCommand();

        Assert.Equal("SELECT [Id] FROM [Users] WHERE [Name] LIKE @p0 ORDER BY [Id] OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY", command.Sql);
        Assert.Equal("A%", command.Parameters.Get<string>("p0"));
        Assert.Equal(20, command.Parameters.Get<int>("p1"));
        Assert.Equal(10, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Sqlite_paging_uses_limit_before_offset_parameter_order()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .Select("Id")
            .From<User>("Users")
            .Where("Name", Op.Like, "A%")
            .OrderBy("Id")
            .Offset(20)
            .Limit(10)
            .ToCommand();

        Assert.Equal("""SELECT "Id" FROM "Users" WHERE "Name" LIKE @p0 ORDER BY "Id" LIMIT @p1 OFFSET @p2""", command.Sql);
        Assert.Equal("A%", command.Parameters.Get<string>("p0"));
        Assert.Equal(10, command.Parameters.Get<int>("p1"));
        Assert.Equal(20, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Postgre_sql_paging_uses_limit_before_offset_parameter_order()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.PostgreSql)
            .Select("Id")
            .From<User>("Users")
            .Where("Name", Op.Like, "A%")
            .OrderBy("Id")
            .Offset(20)
            .Limit(10)
            .ToCommand();

        Assert.Equal("""SELECT "Id" FROM "Users" WHERE "Name" LIKE @p0 ORDER BY "Id" LIMIT @p1 OFFSET @p2""", command.Sql);
        Assert.Equal("A%", command.Parameters.Get<string>("p0"));
        Assert.Equal(10, command.Parameters.Get<int>("p1"));
        Assert.Equal(20, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void My_sql_paging_uses_limit_before_offset_parameter_order()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.MySql)
            .Select("Id")
            .From<User>("Users")
            .Where("Name", Op.Like, "A%")
            .OrderBy("Id")
            .Offset(20)
            .Limit(10)
            .ToCommand();

        Assert.Equal("SELECT `Id` FROM `Users` WHERE `Name` LIKE @p0 ORDER BY `Id` LIMIT @p1 OFFSET @p2", command.Sql);
        Assert.Equal("A%", command.Parameters.Get<string>("p0"));
        Assert.Equal(10, command.Parameters.Get<int>("p1"));
        Assert.Equal(20, command.Parameters.Get<int>("p2"));
    }

    [Table("app_users")]
    private sealed class MappedUser
    {
        public int Id { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; } = "";
    }

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }
}
