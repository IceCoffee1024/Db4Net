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
    public void String_select_with_typed_from_uses_property_names_and_column_attribute()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select("DisplayName")
            .From<MappedUser>()
            .Where("DisplayName", Op.Eq, "Alice")
            .OrderByDescending("DisplayName")
            .ToCommand();

        Assert.Equal("SELECT [display_name] AS [DisplayName] FROM [app_users] WHERE [display_name] = @p0 ORDER BY [display_name] DESC", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void String_select_with_typed_from_rejects_column_attribute_names()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Select("display_name")
                .From<MappedUser>()
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public void String_where_with_typed_builder_rejects_column_attribute_names()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<MappedUser>()
                .Where("display_name", Op.Eq, "Alice")
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public void Select_from_type_can_override_table_or_view_name()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<MappedUser>("app_users_view")
            .Where("DisplayName", Op.Eq, "Alice")
            .ToCommand();

        Assert.Equal("SELECT [Id], [display_name] AS [DisplayName] FROM [app_users_view] WHERE [display_name] = @p0", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Select_from_type_supports_named_table_argument()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<MappedUser>(table: "app_users_view")
            .Where("DisplayName", Op.Eq, "Alice")
            .ToCommand();

        Assert.Equal("SELECT [Id], [display_name] AS [DisplayName] FROM [app_users_view] WHERE [display_name] = @p0", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
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
    public void Typed_select_entry_rejects_not_mapped_member_selectors()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Select<MappedUser>(u => u.Ignored)
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public void Typed_where_rejects_not_mapped_member_selectors()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<MappedUser>()
                .Where(u => u.Ignored, Op.Eq, "value")
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public void Typed_order_by_rejects_not_mapped_member_selectors()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<MappedUser>()
                .OrderBy(u => u.Ignored)
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
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
    public void String_table_identifiers_are_validated()
    {
        var db = Db4NetDatabase.Create(Db4NetOptions.Sqlite);

        var ex = Assert.Throws<ArgumentException>(() =>
            db.SelectFrom<User>("Users;drop table Users")
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

    [Fact]
    public void Where_group_renders_parenthesized_filters()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .WhereGroup(group => group
                .Where(u => u.Id, Op.Eq, 1)
                .OrWhere(u => u.Name, Op.Eq, "Alice"))
            .Where(u => u.Name, Op.Like, "A%")
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE ([Id] = @p0 OR [Name] = @p1) AND [Name] LIKE @p2", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
        Assert.Equal("A%", command.Parameters.Get<string>("p2"));
    }

    [Fact]
    public void Or_where_group_renders_parenthesized_filters()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .OrWhereGroup(group => group
                .Where(u => u.Name, Op.Eq, "Alice")
                .Where(u => u.Id, Op.Gt, 10))
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0 OR ([Name] = @p1 AND [Id] > @p2)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
        Assert.Equal(10, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Where_group_renders_nested_groups()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Gt, 0)
            .WhereGroup(group => group
                .Where(u => u.Name, Op.Like, "A%")
                .OrWhereGroup(nested => nested
                    .Where(u => u.Name, Op.Like, "B%")
                    .Where(u => u.Id, Op.Lt, 10)))
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] > @p0 AND ([Name] LIKE @p1 OR ([Name] LIKE @p2 AND [Id] < @p3))", command.Sql);
        Assert.Equal(0, command.Parameters.Get<int>("p0"));
        Assert.Equal("A%", command.Parameters.Get<string>("p1"));
        Assert.Equal("B%", command.Parameters.Get<string>("p2"));
        Assert.Equal(10, command.Parameters.Get<int>("p3"));
    }

    [Fact]
    public void Where_group_rejects_empty_group()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .WhereGroup(_ => { })
                .ToCommand());

        Assert.Contains("Filter group requires at least one filter", ex.Message);
    }

    [Fact]
    public void String_where_group_before_typed_from_uses_property_mapping()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select("DisplayName")
            .WhereGroup(group => group
                .Where("DisplayName", Op.Eq, "Alice")
                .OrWhere("Id", Op.Eq, 1))
            .From<MappedUser>()
            .ToCommand();

        Assert.Equal("SELECT [display_name] AS [DisplayName] FROM [app_users] WHERE ([display_name] = @p0 OR [Id] = @p1)", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
        Assert.Equal(1, command.Parameters.Get<int>("p1"));
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
