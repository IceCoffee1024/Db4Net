using System.ComponentModel.DataAnnotations.Schema;
using Db4Net;

namespace Db4Net.Tests;

public sealed class SelectQueryBuilderEdgeCaseTests
{
    [Fact]
    public void String_based_select_renders_sqlite_identifiers_and_ascending_order()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .Select(new[] { "Users.Id", "Users.Name" })
            .From("Users")
            .Where("Users.Name", Op.NotEq, null)
            .OrderBy("Users.Id")
            .Limit(5)
            .ToCommand();

        Assert.Equal("""SELECT "Users"."Id", "Users"."Name" FROM "Users" WHERE "Users"."Name" IS NOT NULL ORDER BY "Users"."Id" LIMIT @p0""", command.Sql);
        Assert.Equal(5, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Type_without_table_attribute_uses_type_name()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<PlainUser>()
            .Where(u => u.Id, Op.Eq, 1)
            .ToCommand();

        Assert.Equal("SELECT * FROM [PlainUser] WHERE [Id] = @p0", command.Sql);
    }

    [Fact]
    public void Sql_server_identifier_validation_rejects_invalid_column_names()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom("Users")
                .Where("Id] DROP TABLE Users", Op.Eq, 1)
                .ToCommand());

        Assert.Contains("Invalid SQL identifier", ex.Message);
    }

    [Fact]
    public void Create_rejects_null_options()
    {
        Assert.Throws<ArgumentNullException>(() => Db4NetDatabase.Create(null!));
    }

    [Fact]
    public void Select_rejects_null_column_collection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Select((IEnumerable<string>)null!));
    }

    [Fact]
    public void Sql_server_paging_renders_offset_fetch()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .OrderBy(u => u.Id)
            .Offset(20)
            .Limit(10)
            .ToCommand();

        Assert.Equal("SELECT * FROM [Users] ORDER BY [Id] OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY", command.Sql);
        Assert.Equal(20, command.Parameters.Get<int>("p0"));
        Assert.Equal(10, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Column_attribute_is_used_for_typed_member_selectors()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<MappedUser>()
            .Where(u => u.DisplayName, Op.Eq, "Alice")
            .ToCommand();

        Assert.Equal("SELECT * FROM [app_users] WHERE [display_name] = @p0", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Theory]
    [InlineData(Op.NotEq, "<>")]
    [InlineData(Op.Gt, ">")]
    [InlineData(Op.Gte, ">=")]
    [InlineData(Op.Lt, "<")]
    [InlineData(Op.Lte, "<=")]
    public void Comparison_operators_render_expected_sql(Op op, string sqlOperator)
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Id, op, 5)
            .ToCommand();

        Assert.Equal($"SELECT * FROM [Users] WHERE [Id] {sqlOperator} @p0", command.Sql);
        Assert.Equal(5, command.Parameters.Get<int>("p0"));
    }

    [Theory]
    [InlineData(Op.Eq, "IS NULL")]
    [InlineData(Op.NotEq, "IS NOT NULL")]
    [InlineData(Op.IsNull, "IS NULL")]
    [InlineData(Op.IsNotNull, "IS NOT NULL")]
    public void Null_operators_render_without_parameters(Op op, string expectedOperator)
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Name, op, null)
            .ToCommand();

        Assert.Equal($"SELECT * FROM [Users] WHERE [Name] {expectedOperator}", command.Sql);
        Assert.Empty(command.Parameters.ParameterNames);
    }

    [Fact]
    public void Is_null_operator_has_value_free_string_overload()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom("Users")
            .Where("Name", Op.IsNull)
            .ToCommand();

        Assert.Equal("SELECT * FROM [Users] WHERE [Name] IS NULL", command.Sql);
        Assert.Empty(command.Parameters.ParameterNames);
    }

    [Fact]
    public void Is_not_null_operator_has_value_free_typed_overload()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.IsNotNull)
            .ToCommand();

        Assert.Equal("SELECT * FROM [Users] WHERE [Name] IS NOT NULL", command.Sql);
        Assert.Empty(command.Parameters.ParameterNames);
    }

    [Fact]
    public void Or_where_supports_value_free_null_operator()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .OrWhere(u => u.Name, Op.IsNull)
            .ToCommand();

        Assert.Equal("SELECT * FROM [Users] WHERE [Id] = @p0 OR [Name] IS NULL", command.Sql);
    }

    [Fact]
    public void String_based_or_where_supports_value_free_null_operator()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom("Users")
            .Where("Id", Op.Eq, 1)
            .OrWhere("Name", Op.IsNotNull)
            .ToCommand();

        Assert.Equal("SELECT * FROM [Users] WHERE [Id] = @p0 OR [Name] IS NOT NULL", command.Sql);
    }

    [Fact]
    public void Value_free_where_rejects_non_null_operator()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Name, Op.Eq)
                .ToCommand());

        Assert.Contains("requires a value", ex.Message);
    }

    [Fact]
    public void Is_not_null_rejects_non_null_value()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Name, Op.IsNotNull, "Alice")
                .ToCommand());

        Assert.Contains("does not accept a value", ex.Message);
    }

    [Fact]
    public void To_command_requires_table()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Select("Id")
                .ToCommand());

        Assert.Contains("A table must be specified", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    public void Limit_rejects_negative_values(int count)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Limit(count));

        Assert.Equal("count", ex.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    public void Offset_rejects_negative_values(int count)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Offset(count));

        Assert.Equal("count", ex.ParamName);
    }

    [Theory]
    [InlineData(0, 10, "pageNumber")]
    [InlineData(1, 0, "pageSize")]
    public void Page_rejects_invalid_values(int pageNumber, int pageSize, string expectedParamName)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Page(pageNumber, pageSize));

        Assert.Equal(expectedParamName, ex.ParamName);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(1)]
    public void In_operator_rejects_non_enumerable_values(object value)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Id, Op.In, value)
                .ToCommand());

        Assert.Contains("Op.In requires a non-string enumerable value.", ex.Message);
    }

    [Fact]
    public void In_operator_rejects_empty_enumerable()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Id, Op.In, Array.Empty<int>())
                .ToCommand());

        Assert.Contains("Op.In requires at least one value.", ex.Message);
    }

    [Fact]
    public void Typed_member_selector_must_be_simple_member_access()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Id + 1, Op.Eq, 2)
                .ToCommand());

        Assert.Contains("Only simple member selectors are supported", ex.Message);
    }

    [Fact]
    public void Dapper_execution_requires_bound_connection()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Query<User>()
                .ToList());

        Assert.Contains("Dapper execution requires an IDbConnection", ex.Message);
    }

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    private sealed class PlainUser
    {
        public int Id { get; set; }
    }

    [Table("app_users")]
    private sealed class MappedUser
    {
        public int Id { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; } = "";
    }
}
