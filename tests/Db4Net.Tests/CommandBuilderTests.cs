using System.ComponentModel.DataAnnotations.Schema;
using Db4Net;

namespace Db4Net.Tests;

public sealed class CommandBuilderTests
{
    [Fact]
    public void Delete_from_type_renders_where_clause()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .ToCommand();

        Assert.Equal("DELETE FROM [Users] WHERE [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Delete_from_type_rejects_missing_where_by_default()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .DeleteFrom<User>()
                .ToCommand());

        Assert.Contains("DELETE requires a WHERE clause", ex.Message);
    }

    [Fact]
    public void Delete_from_type_can_allow_all_rows_explicitly()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<User>()
            .AllowAllRows()
            .ToCommand();

        Assert.Equal("DELETE FROM [Users]", command.Sql);
    }

    [Fact]
    public void Delete_from_type_uses_column_attribute_for_string_properties()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<MappedUser>()
            .Where("DisplayName", Op.Eq, "Alice")
            .ToCommand();

        Assert.Equal("DELETE FROM [app_users] WHERE [display_name] = @p0", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Delete_from_type_rejects_column_attribute_names_for_string_properties()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .DeleteFrom<MappedUser>()
                .Where("display_name", Op.Eq, "Alice")
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public void Delete_from_type_rejects_not_mapped_member_selector()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .DeleteFrom<MappedUser>()
                .Where(u => u.Ignored, Op.Eq, "value")
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public void Delete_from_type_expands_in_operator_parameters_in_order()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<User>()
            .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
            .ToCommand();

        Assert.Equal("DELETE FROM [Users] WHERE [Id] IN (@p0, @p1, @p2)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal(2, command.Parameters.Get<int>("p1"));
        Assert.Equal(3, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Update_type_renders_set_before_where_parameters()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update<User>()
            .Set(u => u.Name, "Alice")
            .Where(u => u.Id, Op.Eq, 1)
            .ToCommand();

        Assert.Equal("UPDATE [Users] SET [Name] = @p0 WHERE [Id] = @p1", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
        Assert.Equal(1, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Update_type_rejects_missing_set()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update<User>()
                .Where(u => u.Id, Op.Eq, 1)
                .ToCommand());

        Assert.Contains("UPDATE requires at least one SET assignment", ex.Message);
    }

    [Fact]
    public void Update_type_rejects_missing_where_by_default()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update<User>()
                .Set(u => u.Name, "Alice")
                .ToCommand());

        Assert.Contains("UPDATE requires a WHERE clause", ex.Message);
    }

    [Fact]
    public void Update_type_can_allow_all_rows_explicitly()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update<User>()
            .Set(u => u.Name, "Alice")
            .AllowAllRows()
            .ToCommand();

        Assert.Equal("UPDATE [Users] SET [Name] = @p0", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Update_type_uses_column_attribute_for_string_properties()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update<MappedUser>()
            .Set("DisplayName", "Alice")
            .Where("Id", Op.Eq, 1)
            .ToCommand();

        Assert.Equal("UPDATE [app_users] SET [display_name] = @p0 WHERE [Id] = @p1", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
        Assert.Equal(1, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Update_type_rejects_column_attribute_names_for_string_properties()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update<MappedUser>()
                .Set("display_name", "Alice")
                .Where("Id", Op.Eq, 1)
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public void Update_type_rejects_not_mapped_set_member_selector()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update<MappedUser>()
                .Set(u => u.Ignored, "value")
                .Where(u => u.Id, Op.Eq, 1)
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public void Update_type_renders_null_operators_without_parameters()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update<User>()
            .Set(u => u.Name, "Unknown")
            .Where(u => u.Name, Op.IsNull)
            .ToCommand();

        Assert.Equal("UPDATE [Users] SET [Name] = @p0 WHERE [Name] IS NULL", command.Sql);
        Assert.Equal("Unknown", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Insert_into_type_renders_values()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertInto<User>()
            .Value(u => u.Id, 1)
            .Value(u => u.Name, "Alice")
            .ToCommand();

        Assert.Equal("INSERT INTO [Users] ([Id], [Name]) VALUES (@p0, @p1)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Insert_into_type_uses_column_attribute_for_string_properties()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertInto<MappedUser>()
            .Value("DisplayName", "Alice")
            .ToCommand();

        Assert.Equal("INSERT INTO [app_users] ([display_name]) VALUES (@p0)", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Insert_into_type_rejects_column_attribute_names_for_string_properties()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .InsertInto<MappedUser>()
                .Value("display_name", "Alice")
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public async Task Command_execution_requires_bound_connection()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .DeleteFrom<User>()
                .AllowAllRows()
                .ExecuteAsync());

        Assert.Contains("Dapper execution requires an IDbConnection", ex.Message);
    }

    [Fact]
    public void Insert_into_type_rejects_missing_values()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .InsertInto<User>()
                .ToCommand());

        Assert.Contains("INSERT requires at least one value", ex.Message);
    }

    [Fact]
    public void Insert_into_type_rejects_not_mapped_value_member_selector()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .InsertInto<MappedUser>()
                .Value(u => u.Ignored, "value")
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
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
