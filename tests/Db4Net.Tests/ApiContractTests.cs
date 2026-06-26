using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Db4Net.Commands;
using Db4Net.Query;
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

    [Fact]
    public void Select_query_builder_does_not_expose_execute_methods()
    {
        Assert.DoesNotContain(PublicInstanceMethods(typeof(SelectQueryBuilder)), method => method.Name is "Execute" or "ExecuteAsync");
    }

    [Fact]
    public void Insert_command_builder_exposes_command_api()
    {
        AssertPublicInstanceMethods(typeof(InsertCommandBuilder<>), "Value", "ToCommand", "Execute", "ExecuteAsync");
    }

    [Fact]
    public void Update_command_builder_exposes_update_api()
    {
        AssertPublicInstanceMethods(typeof(UpdateCommandBuilder<>), "Set", "Where", "AllowAllRows");
    }

    [Fact]
    public void Delete_command_builder_exposes_delete_api()
    {
        AssertPublicInstanceMethods(typeof(DeleteCommandBuilder<>), "Where", "AllowAllRows");
    }

    [Fact]
    public void Database_does_not_expose_non_generic_string_only_command_entry_points()
    {
        Assert.DoesNotContain(PublicInstanceMethods(typeof(Db4NetDatabase)), IsNonGenericStringOnlyCommandEntryPoint);
    }

    [Fact]
    public void Public_surface_uses_execution_options_and_rendered_sql_command_names()
    {
        var exportedTypes = typeof(Db4NetDatabase).Assembly.GetExportedTypes();

        Assert.Contains(exportedTypes, type => type == typeof(Db4NetExecutionOptions));
        Assert.Contains(exportedTypes, type => type == typeof(RenderedSqlCommand));
        Assert.DoesNotContain(exportedTypes, type => type.Name is "Db4NetCommandOptions" or "SqlCommandDefinition");
    }

    private static void AssertPublicInstanceMethods(Type type, params string[] methodNames)
    {
        var publicInstanceMethods = PublicInstanceMethods(type);

        foreach (var methodName in methodNames)
        {
            Assert.Contains(publicInstanceMethods, method => method.Name == methodName);
        }
    }

    private static MethodInfo[] PublicInstanceMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
    }

    private static bool IsNonGenericStringOnlyCommandEntryPoint(MethodInfo method)
    {
        return method.Name is "InsertInto" or "Update" or "DeleteFrom"
            && !method.IsGenericMethodDefinition
            && method.GetParameters() is [{ ParameterType: var parameterType }]
            && parameterType == typeof(string);
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
