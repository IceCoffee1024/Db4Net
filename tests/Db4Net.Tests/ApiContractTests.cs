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
                .QueryAsync());

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
    public void Select_query_builders_expose_filter_group_api()
    {
        AssertPublicInstanceMethods(typeof(SelectQueryBuilder), "WhereGroup", "OrWhereGroup");
        AssertPublicInstanceMethods(typeof(SelectQueryBuilder<>), "WhereGroup", "OrWhereGroup");
    }

    [Fact]
    public void Filter_group_builders_expose_filter_only_api()
    {
        AssertPublicInstanceMethods(typeof(FilterGroupBuilder), "Where", "OrWhere", "WhereGroup", "OrWhereGroup");
        AssertPublicInstanceMethods(typeof(FilterGroupBuilder<>), "Where", "OrWhere", "WhereGroup", "OrWhereGroup");

        Assert.DoesNotContain(PublicInstanceMethods(typeof(FilterGroupBuilder)), method => method.Name is "OrderBy" or "Limit" or "Offset" or "Page" or "ToCommand");
        Assert.DoesNotContain(PublicInstanceMethods(typeof(FilterGroupBuilder<>)), method => method.Name is "OrderBy" or "Limit" or "Offset" or "Page" or "ToCommand");
    }

    [Fact]
    public void Typed_select_query_builder_exposes_typed_terminal_methods()
    {
        AssertPublicNonGenericInstanceMethods(
            typeof(SelectQueryBuilder<>),
            "Query",
            "QueryAsync",
            "QueryFirstOrDefault",
            "QueryFirstOrDefaultAsync",
            "QuerySingleOrDefault",
            "QuerySingleOrDefaultAsync");
    }

    [Fact]
    public void Select_count_query_builder_exposes_count_api()
    {
        AssertPublicInstanceMethods(
            typeof(SelectCountQueryBuilder<>),
            "Where",
            "OrWhere",
            "WhereGroup",
            "OrWhereGroup",
            "ToCommand",
            "Execute",
            "ExecuteAsync");
    }

    [Fact]
    public void Select_count_query_builder_does_not_expose_row_query_or_paging_api()
    {
        Assert.DoesNotContain(PublicInstanceMethods(typeof(SelectCountQueryBuilder<>)), method =>
            method.Name is "Select"
                or "OrderBy"
                or "OrderByDescending"
                or "Limit"
                or "Offset"
                or "Page"
                or "Query"
                or "QueryAsync"
                or "QuerySingle"
                or "QuerySingleAsync"
                or "QuerySingleOrDefault"
                or "QuerySingleOrDefaultAsync"
                or "QueryFirstOrDefault"
                or "QueryFirstOrDefaultAsync");
    }

    [Fact]
    public void Insert_command_builder_exposes_command_api()
    {
        AssertPublicInstanceMethods(typeof(InsertCommandBuilder<>), "Value", "Values", "ToCommand", "Execute", "ExecuteAsync");
        AssertBuilderEntityMethodSignature(typeof(InsertCommandBuilder<>), "Values");
    }

    [Fact]
    public void Update_command_builder_exposes_update_api()
    {
        AssertPublicInstanceMethods(typeof(UpdateCommandBuilder<>), "Set", "Where", "WhereGroup", "OrWhereGroup", "WhereKey", "AllowAllRows");
        AssertNoBuilderEntityMethodSignature(typeof(UpdateCommandBuilder<>), "Set");
        AssertBuilderEntityMethodSignature(typeof(UpdateCommandBuilder<>), "WhereKey");
    }

    [Fact]
    public void Delete_command_builder_exposes_delete_api()
    {
        AssertPublicInstanceMethods(typeof(DeleteCommandBuilder<>), "Where", "WhereGroup", "OrWhereGroup", "WhereKey", "AllowAllRows");
        AssertBuilderEntityMethodSignature(typeof(DeleteCommandBuilder<>), "WhereKey");
    }

    [Fact]
    public void Database_exposes_entity_command_convenience_entry_points()
    {
        AssertPublicInstanceMethods(typeof(Db4NetDatabase), "Insert", "InsertOrIgnore", "InsertOrUpdate", "Update", "Delete");

        AssertGenericEntityMethodSignature(typeof(Db4NetDatabase), "Insert", typeof(InsertCommandBuilder<>));
        AssertGenericEntityWithTableMethodSignature(typeof(Db4NetDatabase), "Insert", typeof(InsertCommandBuilder<>));
        AssertGenericEntityMethodSignature(typeof(Db4NetDatabase), "InsertOrIgnore", typeof(InsertOrIgnoreCommandBuilder<>));
        AssertGenericEntityWithTableMethodSignature(typeof(Db4NetDatabase), "InsertOrIgnore", typeof(InsertOrIgnoreCommandBuilder<>));
        AssertGenericEntityMethodSignature(typeof(Db4NetDatabase), "InsertOrUpdate", typeof(InsertOrUpdateCommandBuilder<>));
        AssertGenericEntityWithTableMethodSignature(typeof(Db4NetDatabase), "InsertOrUpdate", typeof(InsertOrUpdateCommandBuilder<>));
        AssertGenericEntityMethodSignature(typeof(Db4NetDatabase), "Update", typeof(UpdateCommandBuilder<>));
        AssertGenericEntityWithTableMethodSignature(typeof(Db4NetDatabase), "Update", typeof(UpdateCommandBuilder<>));
        AssertGenericEntityMethodSignature(typeof(Db4NetDatabase), "Delete", typeof(DeleteCommandBuilder<>));
        AssertGenericEntityWithTableMethodSignature(typeof(Db4NetDatabase), "Delete", typeof(DeleteCommandBuilder<>));
    }

    [Fact]
    public void Database_exposes_many_entity_command_convenience_entry_points()
    {
        AssertPublicInstanceMethods(typeof(Db4NetDatabase), "InsertMany", "InsertOrIgnoreMany", "InsertOrUpdateMany", "UpdateMany", "DeleteMany");

        AssertGenericEnumerableMethodSignature(typeof(Db4NetDatabase), "InsertMany", typeof(InsertManyCommandBuilder<>));
        AssertGenericEnumerableWithTableMethodSignature(typeof(Db4NetDatabase), "InsertMany", typeof(InsertManyCommandBuilder<>));
        AssertGenericEnumerableMethodSignature(typeof(Db4NetDatabase), "InsertOrIgnoreMany", typeof(InsertOrIgnoreManyCommandBuilder<>));
        AssertGenericEnumerableWithTableMethodSignature(typeof(Db4NetDatabase), "InsertOrIgnoreMany", typeof(InsertOrIgnoreManyCommandBuilder<>));
        AssertGenericEnumerableMethodSignature(typeof(Db4NetDatabase), "InsertOrUpdateMany", typeof(InsertOrUpdateManyCommandBuilder<>));
        AssertGenericEnumerableWithTableMethodSignature(typeof(Db4NetDatabase), "InsertOrUpdateMany", typeof(InsertOrUpdateManyCommandBuilder<>));
        AssertGenericEnumerableMethodSignature(typeof(Db4NetDatabase), "UpdateMany", typeof(UpdateManyCommandBuilder<>));
        AssertGenericEnumerableWithTableMethodSignature(typeof(Db4NetDatabase), "UpdateMany", typeof(UpdateManyCommandBuilder<>));
        AssertGenericEnumerableMethodSignature(typeof(Db4NetDatabase), "DeleteMany", typeof(DeleteManyCommandBuilder<>));
        AssertGenericEnumerableWithTableMethodSignature(typeof(Db4NetDatabase), "DeleteMany", typeof(DeleteManyCommandBuilder<>));
    }

    [Fact]
    public void Database_exposes_lightweight_transaction_api()
    {
        AssertPublicInstanceMethods(
            typeof(Db4NetDatabase),
            "WithExecutionOptions",
            "WithTransaction",
            "BeginTransaction",
            "ExecuteInTransaction",
            "ExecuteInTransactionAsync");

        AssertPublicInstanceMethods(typeof(Db4NetTransaction), "Commit", "Rollback");
        Assert.NotNull(typeof(Db4NetTransaction).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(Db4NetTransaction).GetProperty("Transaction", BindingFlags.Public | BindingFlags.Instance));
        AssertPublicStaticMethods(
            typeof(Db4NetTransactionExtensions),
            "Select",
            "SelectCountFrom",
            "SelectFrom",
            "Insert",
            "InsertInto",
            "InsertMany",
            "InsertOrIgnore",
            "InsertOrIgnoreMany",
            "InsertOrUpdate",
            "InsertOrUpdateMany",
            "Update",
            "UpdateMany",
            "Delete",
            "DeleteFrom",
            "DeleteMany");
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(Db4NetTransaction)));
        Assert.Single(PublicInstanceMethods(typeof(Db4NetDatabase)), method => method.Name == "BeginTransaction" && method.GetParameters().Length == 0 && method.ReturnType == typeof(Db4NetTransaction));
        Assert.Contains(PublicInstanceMethods(typeof(Db4NetDatabase)), method => method.Name == "BeginTransaction" && method.GetParameters() is [{ ParameterType: var parameterType }] && parameterType == typeof(System.Data.IsolationLevel));
        Assert.Contains(PublicInstanceMethods(typeof(Db4NetDatabase)), method => method.Name == "ExecuteInTransaction" && method.GetParameters() is [{ ParameterType: var parameterType }] && parameterType == typeof(Action<Db4NetTransaction>));
        Assert.Contains(typeof(Db4NetConnectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static), method => method.Name == "UseDb4Net"
            && method.GetParameters() is [{ ParameterType: var connection }, { ParameterType: var options }, { ParameterType: var executionOptions }]
            && connection == typeof(System.Data.IDbConnection)
            && options == typeof(Db4NetOptions)
            && executionOptions == typeof(Db4NetExecutionOptions));

        var exportedTypes = typeof(Db4NetDatabase).Assembly.GetExportedTypes();
        Assert.Contains(exportedTypes, type => type == typeof(Db4NetTransaction));
    }

    [Fact]
    public void Many_command_builders_expose_execution_api()
    {
        AssertPublicInstanceMethods(typeof(InsertManyCommandBuilder<>), "ToCommands", "Execute", "ExecuteAsync");
        AssertPublicInstanceMethods(typeof(InsertOrIgnoreManyCommandBuilder<>), "OnConflict", "ToCommands", "Execute", "ExecuteAsync");
        AssertPublicInstanceMethods(typeof(InsertOrUpdateManyCommandBuilder<>), "OnConflict", "Update", "ToCommands", "Execute", "ExecuteAsync");
        AssertPublicInstanceMethods(typeof(UpdateManyCommandBuilder<>), "ToCommands", "Execute", "ExecuteAsync");
        AssertPublicInstanceMethods(typeof(DeleteManyCommandBuilder<>), "ToCommands", "Execute", "ExecuteAsync");

        Assert.DoesNotContain(PublicInstanceMethods(typeof(InsertManyCommandBuilder<>)), method => method.Name == "ToCommand");
        Assert.DoesNotContain(PublicInstanceMethods(typeof(InsertOrIgnoreManyCommandBuilder<>)), method => method.Name == "ToCommand");
        Assert.DoesNotContain(PublicInstanceMethods(typeof(InsertOrUpdateManyCommandBuilder<>)), method => method.Name == "ToCommand");
        Assert.DoesNotContain(PublicInstanceMethods(typeof(UpdateManyCommandBuilder<>)), method => method.Name == "ToCommand");
        Assert.DoesNotContain(PublicInstanceMethods(typeof(DeleteManyCommandBuilder<>)), method => method.Name == "ToCommand");
    }

    [Fact]
    public void Conflict_insert_command_builders_expose_conflict_api()
    {
        AssertPublicInstanceMethods(typeof(InsertOrIgnoreCommandBuilder<>), "OnConflict", "ToCommand", "Execute", "ExecuteAsync");
        AssertPublicInstanceMethods(typeof(InsertOrUpdateCommandBuilder<>), "OnConflict", "Update", "ToCommand", "Execute", "ExecuteAsync");
    }

    [Fact]
    public void Public_surface_does_not_use_bulk_naming()
    {
        var exportedTypes = typeof(Db4NetDatabase).Assembly.GetExportedTypes();

        Assert.DoesNotContain(exportedTypes, type => type.Name.Contains("Bulk", StringComparison.Ordinal));
        Assert.DoesNotContain(PublicInstanceMethods(typeof(Db4NetDatabase)), method => method.Name.Contains("Bulk", StringComparison.Ordinal));
    }

    [Fact]
    public void Public_surface_does_not_use_orm_or_merge_naming()
    {
        var disallowedNames = new[] { "Save", "SaveChanges", "Merge", "Upsert", "UnitOfWork" };
        var exportedTypes = typeof(Db4NetDatabase).Assembly.GetExportedTypes();

        foreach (var name in disallowedNames)
        {
            Assert.DoesNotContain(exportedTypes, type => type.Name.Contains(name, StringComparison.Ordinal));
            Assert.DoesNotContain(PublicInstanceMethods(typeof(Db4NetDatabase)), method => method.Name.Contains(name, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Database_exposes_sql_shaped_command_builder_entry_points()
    {
        AssertGenericParameterlessMethodSignature(typeof(Db4NetDatabase), "SelectCountFrom", typeof(SelectCountQueryBuilder<>));
        AssertGenericStringMethodSignature(typeof(Db4NetDatabase), "SelectCountFrom", typeof(SelectCountQueryBuilder<>));

        AssertGenericParameterlessMethodSignature(typeof(Db4NetDatabase), "InsertInto", typeof(InsertCommandBuilder<>));
        AssertGenericStringMethodSignature(typeof(Db4NetDatabase), "InsertInto", typeof(InsertCommandBuilder<>));

        AssertGenericParameterlessMethodSignature(typeof(Db4NetDatabase), "Update", typeof(UpdateCommandBuilder<>));
        AssertGenericStringMethodSignature(typeof(Db4NetDatabase), "Update", typeof(UpdateCommandBuilder<>));

        AssertGenericParameterlessMethodSignature(typeof(Db4NetDatabase), "DeleteFrom", typeof(DeleteCommandBuilder<>));
        AssertGenericStringMethodSignature(typeof(Db4NetDatabase), "DeleteFrom", typeof(DeleteCommandBuilder<>));
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

    [Fact]
    public void Internal_filter_clause_uses_enum_for_boolean_operator()
    {
        var filterClauseType = typeof(Db4NetDatabase).Assembly.GetType("Db4Net.Query.FilterClause", throwOnError: true)!;
        var booleanOperator = filterClauseType.GetProperty("BooleanOperator", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(booleanOperator);
        Assert.True(booleanOperator.PropertyType.IsEnum);
        Assert.NotEqual(typeof(string), booleanOperator.PropertyType);
    }

    private static void AssertPublicInstanceMethods(Type type, params string[] methodNames)
    {
        var publicInstanceMethods = PublicInstanceMethods(type);

        foreach (var methodName in methodNames)
        {
            Assert.Contains(publicInstanceMethods, method => method.Name == methodName);
        }
    }

    private static void AssertPublicStaticMethods(Type type, params string[] methodNames)
    {
        var publicStaticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

        foreach (var methodName in methodNames)
        {
            Assert.Contains(publicStaticMethods, method => method.Name == methodName);
        }
    }

    private static void AssertPublicNonGenericInstanceMethods(Type type, params string[] methodNames)
    {
        var publicInstanceMethods = PublicInstanceMethods(type);

        foreach (var methodName in methodNames)
        {
            Assert.Contains(publicInstanceMethods, method => method.Name == methodName && !method.IsGenericMethod);
        }
    }

    private static MethodInfo[] PublicInstanceMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
    }

    private static void AssertGenericParameterlessMethodSignature(Type declaringType, string methodName, Type genericReturnTypeDefinition)
    {
        Assert.Single(
            PublicInstanceMethods(declaringType),
            candidate => IsGenericMethodWithReturn(candidate, methodName, genericReturnTypeDefinition)
                && candidate.GetParameters().Length == 0);
    }

    private static void AssertGenericStringMethodSignature(Type declaringType, string methodName, Type genericReturnTypeDefinition)
    {
        Assert.Single(
            PublicInstanceMethods(declaringType),
            candidate => IsGenericMethodWithReturn(candidate, methodName, genericReturnTypeDefinition)
                && candidate.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == typeof(string));
    }

    private static void AssertGenericEntityMethodSignature(Type declaringType, string methodName, Type genericReturnTypeDefinition)
    {
        var method = Assert.Single(
            PublicInstanceMethods(declaringType),
            candidate => IsGenericMethodWithReturn(candidate, methodName, genericReturnTypeDefinition)
                && candidate.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == candidate.GetGenericArguments()[0]);

        Assert.Equal(method.GetGenericArguments()[0], method.GetParameters()[0].ParameterType);
    }

    private static void AssertBuilderEntityMethodSignature(Type builderType, string methodName)
    {
        var modelType = builderType.GetGenericArguments()[0];
        var method = Assert.Single(
            PublicInstanceMethods(builderType),
            candidate => candidate.Name == methodName
                && candidate.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == modelType);

        Assert.Equal(builderType, method.ReturnType);
    }

    private static void AssertNoBuilderEntityMethodSignature(Type builderType, string methodName)
    {
        var modelType = builderType.GetGenericArguments()[0];
        Assert.DoesNotContain(
            PublicInstanceMethods(builderType),
            candidate => candidate.Name == methodName
                && candidate.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == modelType);
    }

    private static void AssertGenericEntityWithTableMethodSignature(Type declaringType, string methodName, Type genericReturnTypeDefinition)
    {
        var method = Assert.Single(
            PublicInstanceMethods(declaringType),
            candidate => IsGenericMethodWithReturn(candidate, methodName, genericReturnTypeDefinition)
                && candidate.GetParameters() is [{ ParameterType: var entityParameterType }, { ParameterType: var tableParameterType }]
                && entityParameterType == candidate.GetGenericArguments()[0]
                && tableParameterType == typeof(string));

        Assert.Equal("table", method.GetParameters()[1].Name);
    }

    private static void AssertGenericEnumerableMethodSignature(Type declaringType, string methodName, Type genericReturnTypeDefinition)
    {
        var method = Assert.Single(
            PublicInstanceMethods(declaringType),
            candidate => IsGenericMethodWithReturn(candidate, methodName, genericReturnTypeDefinition)
                && candidate.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType.IsGenericType
                && parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                && parameterType.GetGenericArguments()[0] == candidate.GetGenericArguments()[0]);

        Assert.Equal(method.GetGenericArguments()[0], method.GetParameters()[0].ParameterType.GetGenericArguments()[0]);
    }

    private static void AssertGenericEnumerableWithTableMethodSignature(Type declaringType, string methodName, Type genericReturnTypeDefinition)
    {
        var method = Assert.Single(
            PublicInstanceMethods(declaringType),
            candidate => IsGenericMethodWithReturn(candidate, methodName, genericReturnTypeDefinition)
                && candidate.GetParameters() is [{ ParameterType: var entitiesParameterType }, { ParameterType: var tableParameterType }]
                && entitiesParameterType.IsGenericType
                && entitiesParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                && entitiesParameterType.GetGenericArguments()[0] == candidate.GetGenericArguments()[0]
                && tableParameterType == typeof(string));

        Assert.Equal("table", method.GetParameters()[1].Name);
    }

    private static bool IsGenericMethodWithReturn(MethodInfo method, string methodName, Type genericReturnTypeDefinition)
    {
        return method.Name == methodName
            && method.IsGenericMethodDefinition
            && method.ReturnType.IsGenericType
            && method.ReturnType.GetGenericTypeDefinition() == genericReturnTypeDefinition;
    }

    private static bool IsNonGenericStringOnlyCommandEntryPoint(MethodInfo method)
    {
        return method.Name is "InsertInto" or "InsertOrIgnore" or "InsertOrUpdate" or "InsertOrIgnoreMany" or "InsertOrUpdateMany" or "Update" or "DeleteFrom"
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
