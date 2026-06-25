using System.ComponentModel.DataAnnotations.Schema;
using Db4Net;

namespace Db4Net.Tests;

public sealed class SelectQueryBuilderTests
{
    [Fact]
    public void Typed_select_renders_sql_server_sql_with_parameter()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select("Id", "Name")
            .From<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Select_from_type_renders_sqlite_ordering_and_paging()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.Like, "A%")
            .OrderByDescending(u => u.Id)
            .Page(2, 10)
            .ToCommand();

        Assert.Equal("""SELECT "Id", "Name" FROM "Users" WHERE "Name" LIKE @p0 ORDER BY "Id" DESC LIMIT @p1 OFFSET @p2""", command.Sql);
        Assert.Equal("A%", command.Parameters.Get<string>("p0"));
        Assert.Equal(10, command.Parameters.Get<int>("p1"));
        Assert.Equal(10, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Typed_select_entry_renders_selected_member_columns()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select<User>(u => u.Id, u => u.Name)
            .Where(u => u.Id, Op.Eq, 1)
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Typed_select_entry_uses_column_attribute()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select<MappedUser>(u => u.DisplayName)
            .ToCommand();

        Assert.Equal("SELECT [display_name] AS [DisplayName] FROM [app_users]", command.Sql);
    }

    [Fact]
    public void Typed_select_entry_renders_sqlite_column_alias()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .Select<MappedUser>(u => u.DisplayName)
            .ToCommand();

        Assert.Equal("SELECT \"display_name\" AS \"DisplayName\" FROM \"app_users\"", command.Sql);
    }

    [Fact]
    public void Select_from_type_renders_all_mapped_member_columns()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<MappedUser>()
            .ToCommand();

        Assert.Equal("SELECT [Id], [display_name] AS [DisplayName] FROM [app_users]", command.Sql);
    }

    [Fact]
    public void Typed_select_entry_requires_simple_member_selectors()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Select<User>(u => u.Id + 1)
                .ToCommand());

        Assert.Contains("Only simple member selectors are supported", ex.Message);
    }

    [Fact]
    public void Select_from_type_can_add_selected_member_columns()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Select(u => u.Id, u => u.Name)
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users]", command.Sql);
    }

    [Fact]
    public void String_table_and_column_identifiers_are_validated()
    {
        var db = Db4NetDatabase.Create(Db4NetOptions.Sqlite);

        var ex = Assert.Throws<ArgumentException>(() =>
            db.SelectFrom("Users;drop table Users")
              .Where("Id", Op.Eq, 1)
              .ToCommand());

        Assert.Contains("Invalid SQL identifier", ex.Message);
    }

    [Fact]
    public void In_operator_expands_parameters()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] IN (@p0, @p1, @p2)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal(2, command.Parameters.Get<int>("p1"));
        Assert.Equal(3, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Or_where_renders_uppercase_boolean_operator()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .OrWhere(u => u.Name, Op.Eq, "Alice")
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0 OR [Name] = @p1", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
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
