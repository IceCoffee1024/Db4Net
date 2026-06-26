using System.ComponentModel.DataAnnotations.Schema;
using Db4Net;

namespace Db4Net.Tests;

public sealed class ApiContractTests
{
    [Fact]
    public void Typed_string_select_replaces_existing_select_from_columns()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<MappedUser>()
            .Select(new[] { "DisplayName" })
            .ToCommand();

        Assert.Equal("SELECT [display_name] AS [DisplayName] FROM [app_users]", command.Sql);
    }

    [Fact]
    public void String_order_by_with_typed_builder_rejects_column_attribute_names()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<MappedUser>()
                .OrderBy("display_name")
                .ToCommand());

        Assert.Contains("is not a mapped column", ex.Message);
    }

    [Fact]
    public async Task Async_dapper_execution_requires_bound_connection()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .QueryAsync<User>());

        Assert.Contains("Dapper execution requires an IDbConnection", ex.Message);
    }

    [Fact]
    public void Limit_zero_is_allowed_and_rendered_as_parameter()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .OrderBy(u => u.Id)
            .Limit(0)
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] ORDER BY [Id] OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY", command.Sql);
        Assert.Equal(0, command.Parameters.Get<int>("p0"));
        Assert.Equal(0, command.Parameters.Get<int>("p1"));
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
    }
}
