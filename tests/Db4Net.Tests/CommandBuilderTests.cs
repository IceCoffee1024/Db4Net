using System.ComponentModel.DataAnnotations;
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
    public void Delete_from_type_can_override_target_table()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<User>("users_2026")
            .Where(u => u.Id, Op.Eq, 1)
            .ToCommand();

        Assert.Equal("DELETE FROM [users_2026] WHERE [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Delete_from_type_supports_named_table_argument()
    {
        var user = new User { Id = 1, Name = "Alice" };

        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<User>(table: "users_2026")
            .Where(u => u.Id, Op.Eq, user.Id)
            .ToCommand();

        Assert.Equal("DELETE FROM [users_2026] WHERE [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Delete_from_type_with_table_override_rejects_invalid_table_identifier()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .DeleteFrom<User>("users;drop table Users")
                .Where(u => u.Id, Op.Eq, 1)
                .ToCommand());

        Assert.Contains("Invalid SQL identifier", ex.Message);
    }

    [Fact]
    public void Delete_from_type_with_table_override_still_rejects_missing_where_by_default()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .DeleteFrom<User>("users_2026")
                .ToCommand());

        Assert.Contains("DELETE requires a WHERE clause", ex.Message);
    }

    [Fact]
    public void Delete_from_type_with_table_override_can_allow_all_rows_explicitly()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<User>("users_2026")
            .AllowAllRows()
            .ToCommand();

        Assert.Equal("DELETE FROM [users_2026]", command.Sql);
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
    public void Delete_from_type_renders_where_group()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<User>()
            .WhereGroup(group => group
                .Where(u => u.Id, Op.Eq, 1)
                .OrWhere(u => u.Name, Op.Eq, "Alice"))
            .Where(u => u.Name, Op.Like, "A%")
            .ToCommand();

        Assert.Equal("DELETE FROM [Users] WHERE ([Id] = @p0 OR [Name] = @p1) AND [Name] LIKE @p2", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
        Assert.Equal("A%", command.Parameters.Get<string>("p2"));
    }

    [Fact]
    public void Delete_from_type_rejects_empty_where_group()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .DeleteFrom<User>()
                .WhereGroup(_ => { })
                .ToCommand());

        Assert.Contains("Filter group requires at least one filter", ex.Message);
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
    public void Update_type_can_override_target_table()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update<MappedUser>("users_2026")
            .Set(u => u.DisplayName, "Alice")
            .Where(u => u.Id, Op.Eq, 1)
            .ToCommand();

        Assert.Equal("UPDATE [users_2026] SET [display_name] = @p0 WHERE [Id] = @p1", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
        Assert.Equal(1, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Update_type_supports_named_table_argument()
    {
        var user = new User { Id = 1, Name = "Alice" };

        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update<User>(table: "users_2026")
            .Set(u => u.Name, "Alice")
            .Where(u => u.Id, Op.Eq, user.Id)
            .ToCommand();

        Assert.Equal("UPDATE [users_2026] SET [Name] = @p0 WHERE [Id] = @p1", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
        Assert.Equal(1, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Update_type_with_table_override_still_rejects_missing_where_by_default()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update<User>("users_2026")
                .Set(u => u.Name, "Alice")
                .ToCommand());

        Assert.Contains("UPDATE requires a WHERE clause", ex.Message);
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
    public void Update_type_renders_where_group_after_set_parameters()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update<User>()
            .Set(u => u.Name, "Updated")
            .Where(u => u.Id, Op.Eq, 1)
            .OrWhereGroup(group => group
                .Where(u => u.Name, Op.Eq, "Alice")
                .Where(u => u.Id, Op.Gt, 10))
            .ToCommand();

        Assert.Equal("UPDATE [Users] SET [Name] = @p0 WHERE [Id] = @p1 OR ([Name] = @p2 AND [Id] > @p3)", command.Sql);
        Assert.Equal("Updated", command.Parameters.Get<string>("p0"));
        Assert.Equal(1, command.Parameters.Get<int>("p1"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p2"));
        Assert.Equal(10, command.Parameters.Get<int>("p3"));
    }

    [Fact]
    public void Update_type_rejects_empty_where_group()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update<User>()
                .Set(u => u.Name, "Updated")
                .WhereGroup(_ => { })
                .ToCommand());

        Assert.Contains("Filter group requires at least one filter", ex.Message);
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
    public void Update_type_with_table_override_rejects_invalid_table_identifier()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update<User>("users;drop table Users")
                .Set(u => u.Name, "Alice")
                .Where(u => u.Id, Op.Eq, 1)
                .ToCommand());

        Assert.Contains("Invalid SQL identifier", ex.Message);
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
    public void Insert_into_type_can_expand_entity_values()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertInto<User>()
            .Values(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("INSERT INTO [Users] ([Id], [Name]) VALUES (@p0, @p1)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Insert_into_entity_entry_point_expands_values()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Insert(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("INSERT INTO [Users] ([Id], [Name]) VALUES (@p0, @p1)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Insert_entity_entry_point_can_override_target_table()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Insert(new User { Id = 1, Name = "Alice" }, table: "users_staging")
            .ToCommand();

        Assert.Equal("INSERT INTO [users_staging] ([Id], [Name]) VALUES (@p0, @p1)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Insert_many_entity_entry_point_renders_command_for_each_entity()
    {
        var commands = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertMany(
            [
                new User { Id = 1, Name = "Alice" },
                new User { Id = 2, Name = "Bob" },
            ])
            .ToCommands();

        Assert.Collection(
            commands,
            command =>
            {
                Assert.Equal("INSERT INTO [Users] ([Id], [Name]) VALUES (@Id, @Name)", command.Sql);
                Assert.Equal(1, command.Parameters.Get<int>("Id"));
                Assert.Equal("Alice", command.Parameters.Get<string>("Name"));
            },
            command =>
            {
                Assert.Equal("INSERT INTO [Users] ([Id], [Name]) VALUES (@Id, @Name)", command.Sql);
                Assert.Equal(2, command.Parameters.Get<int>("Id"));
                Assert.Equal("Bob", command.Parameters.Get<string>("Name"));
            });
    }

    [Fact]
    public void Insert_many_entity_entry_point_can_override_target_table()
    {
        var commands = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertMany(
            [
                new User { Id = 1, Name = "Alice" },
            ],
            table: "users_staging")
            .ToCommands();

        var command = Assert.Single(commands);
        Assert.Equal("INSERT INTO [users_staging] ([Id], [Name]) VALUES (@Id, @Name)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("Id"));
        Assert.Equal("Alice", command.Parameters.Get<string>("Name"));
    }

    [Fact]
    public void Insert_many_skips_database_generated_values_for_each_entity()
    {
        var commands = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertMany(
            [
                new GeneratedKeyUser { Id = 1, Name = "Alice" },
                new GeneratedKeyUser { Id = 2, Name = "Bob" },
            ])
            .ToCommands();

        Assert.Collection(
            commands,
            command =>
            {
                Assert.Equal("INSERT INTO [generated_users] ([Name]) VALUES (@Name)", command.Sql);
                Assert.Equal("Alice", command.Parameters.Get<string>("Name"));
            },
            command =>
            {
                Assert.Equal("INSERT INTO [generated_users] ([Name]) VALUES (@Name)", command.Sql);
                Assert.Equal("Bob", command.Parameters.Get<string>("Name"));
            });
    }

    [Fact]
    public void Insert_or_ignore_entity_entry_point_can_override_target_table()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .InsertOrIgnore(new User { Id = 1, Name = "Alice" }, table: "users_staging")
            .ToCommand();

        Assert.Equal("""INSERT INTO "users_staging" ("Id", "Name") VALUES (@p0, @p1) ON CONFLICT ("Id") DO NOTHING""", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Insert_or_ignore_can_use_explicit_composite_conflict_target()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .InsertOrIgnore(new CompositeKeyUser { TenantId = 1, UserId = 2, Name = "Alice" })
            .OnConflict(u => u.TenantId, u => u.UserId)
            .ToCommand();

        Assert.Equal("""INSERT INTO "CompositeKeyUser" ("TenantId", "UserId", "Name") VALUES (@p0, @p1, @p2) ON CONFLICT ("TenantId", "UserId") DO NOTHING""", command.Sql);
    }

    [Fact]
    public void Insert_or_update_rejects_missing_key_without_explicit_conflict_target()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.Sqlite)
                .InsertOrUpdate(new NoKeyUser { Name = "Alice" })
                .ToCommand());

        Assert.Contains("does not have a key", ex.Message);
    }

    [Fact]
    public void Insert_or_update_rejects_generated_update_columns()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.Sqlite)
                .InsertOrUpdate(new GeneratedAuditUser { Id = 1, Name = "Alice" })
                .Update(u => u.UpdatedAt)
                .ToCommand());

        Assert.Contains("database-generated", ex.Message);
    }

    [Fact]
    public void Conflict_insert_rejects_generated_conflict_columns()
    {
        var ignoreEx = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.Sqlite)
                .InsertOrIgnore(new GeneratedKeyUser { Id = 1, Name = "Alice" })
                .ToCommand());
        var updateEx = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.Sqlite)
                .InsertOrUpdate(new GeneratedKeyUser { Id = 1, Name = "Alice" })
                .ToCommand());

        Assert.Contains("database-generated", ignoreEx.Message);
        Assert.Contains("database-generated", updateEx.Message);
    }

    [Fact]
    public void Insert_or_update_many_renders_command_for_each_entity()
    {
        var commands = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .InsertOrUpdateMany(
            [
                new User { Id = 1, Name = "Alice" },
                new User { Id = 2, Name = "Bob" },
            ])
            .ToCommands();

        Assert.Collection(
            commands,
            command =>
            {
                Assert.Equal("INSERT INTO \"Users\" (\"Id\", \"Name\") VALUES (@Id, @Name) ON CONFLICT (\"Id\") DO UPDATE SET \"Name\" = excluded.\"Name\"", command.Sql);
                Assert.Equal(1, command.Parameters.Get<int>("Id"));
                Assert.Equal("Alice", command.Parameters.Get<string>("Name"));
            },
            command =>
            {
                Assert.Equal("INSERT INTO \"Users\" (\"Id\", \"Name\") VALUES (@Id, @Name) ON CONFLICT (\"Id\") DO UPDATE SET \"Name\" = excluded.\"Name\"", command.Sql);
                Assert.Equal(2, command.Parameters.Get<int>("Id"));
                Assert.Equal("Bob", command.Parameters.Get<string>("Name"));
            });
    }

    [Fact]
    public void Empty_conflict_many_entity_command_conveniences_return_no_commands()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.Sqlite);

        Assert.Empty(database.InsertOrIgnoreMany(Array.Empty<User>()).ToCommands());
        Assert.Empty(database.InsertOrUpdateMany(Array.Empty<User>()).ToCommands());
        Assert.Empty(database.InsertOrIgnoreMany(Array.Empty<NoKeyUser>()).ToCommands());
        Assert.Empty(database.InsertOrUpdateMany(Array.Empty<NoKeyUser>()).ToCommands());
    }

    [Fact]
    public void Insert_into_type_skips_database_generated_key_values_from_entity()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertInto<GeneratedKeyUser>()
            .Values(new GeneratedKeyUser { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("INSERT INTO [generated_users] ([Name]) VALUES (@p0)", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Insert_into_type_skips_database_generated_non_key_values_from_entity()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertInto<GeneratedAuditUser>()
            .Values(new GeneratedAuditUser { Id = 1, Name = "Alice", UpdatedAt = DateTime.UtcNow })
            .ToCommand();

        Assert.Equal("INSERT INTO [generated_audit_users] ([Id], [Name]) VALUES (@p0, @p1)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Insert_entity_entry_point_skips_database_generated_key_values()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Insert(new GeneratedKeyUser { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("INSERT INTO [generated_users] ([Name]) VALUES (@p0)", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Insert_into_type_can_override_target_table()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertInto<MappedUser>("users_staging")
            .Value(u => u.DisplayName, "Alice")
            .ToCommand();

        Assert.Equal("INSERT INTO [users_staging] ([display_name]) VALUES (@p0)", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Fact]
    public void Insert_into_type_supports_named_table_argument()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertInto<User>(table: "users_staging")
            .Value(u => u.Id, 1)
            .Value(u => u.Name, "Alice")
            .ToCommand();

        Assert.Equal("INSERT INTO [users_staging] ([Id], [Name]) VALUES (@p0, @p1)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Insert_into_type_with_table_override_rejects_invalid_table_identifier()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .InsertInto<User>("users;drop table Users")
                .Value(u => u.Id, 1)
                .ToCommand());

        Assert.Contains("Invalid SQL identifier", ex.Message);
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

    [Fact]
    public void Update_entity_entry_point_sets_non_key_values_and_filters_by_key()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("UPDATE [Users] SET [Name] = @p0 WHERE [Id] = @p1", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
        Assert.Equal(1, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Update_entity_entry_point_can_override_target_table()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update(new User { Id = 1, Name = "Alice" }, table: "users_2026")
            .ToCommand();

        Assert.Equal("UPDATE [users_2026] SET [Name] = @p0 WHERE [Id] = @p1", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
        Assert.Equal(1, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Delete_entity_entry_point_filters_by_key()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Delete(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("DELETE FROM [Users] WHERE [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Delete_entity_entry_point_can_override_target_table()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Delete(new User { Id = 1, Name = "Alice" }, table: "users_2026")
            .ToCommand();

        Assert.Equal("DELETE FROM [users_2026] WHERE [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Update_many_entity_entry_point_sets_non_key_values_and_filters_each_entity_by_key()
    {
        var commands = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .UpdateMany(
            [
                new User { Id = 1, Name = "Alice" },
                new User { Id = 2, Name = "Bob" },
            ])
            .ToCommands();

        Assert.Collection(
            commands,
            command =>
            {
                Assert.Equal("UPDATE [Users] SET [Name] = @Name WHERE [Id] = @Id", command.Sql);
                Assert.Equal(1, command.Parameters.Get<int>("Id"));
                Assert.Equal("Alice", command.Parameters.Get<string>("Name"));
            },
            command =>
            {
                Assert.Equal("UPDATE [Users] SET [Name] = @Name WHERE [Id] = @Id", command.Sql);
                Assert.Equal(2, command.Parameters.Get<int>("Id"));
                Assert.Equal("Bob", command.Parameters.Get<string>("Name"));
            });
    }

    [Fact]
    public void Update_many_entity_entry_point_can_override_target_table()
    {
        var commands = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .UpdateMany(
            [
                new User { Id = 1, Name = "Alice" },
            ],
            table: "users_2026")
            .ToCommands();

        var command = Assert.Single(commands);
        Assert.Equal("UPDATE [users_2026] SET [Name] = @Name WHERE [Id] = @Id", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("Id"));
        Assert.Equal("Alice", command.Parameters.Get<string>("Name"));
    }

    [Fact]
    public void Delete_many_entity_entry_point_filters_each_entity_by_key()
    {
        var commands = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteMany(
            [
                new User { Id = 1, Name = "Alice" },
                new User { Id = 2, Name = "Bob" },
            ])
            .ToCommands();

        Assert.Collection(
            commands,
            command =>
            {
                Assert.Equal("DELETE FROM [Users] WHERE [Id] = @Id", command.Sql);
                Assert.Equal(1, command.Parameters.Get<int>("Id"));
            },
            command =>
            {
                Assert.Equal("DELETE FROM [Users] WHERE [Id] = @Id", command.Sql);
                Assert.Equal(2, command.Parameters.Get<int>("Id"));
            });
    }

    [Fact]
    public void Delete_many_entity_entry_point_can_override_target_table()
    {
        var commands = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteMany(
            [
                new User { Id = 1, Name = "Alice" },
            ],
            table: "users_2026")
            .ToCommands();

        var command = Assert.Single(commands);
        Assert.Equal("DELETE FROM [users_2026] WHERE [Id] = @Id", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("Id"));
    }

    [Fact]
    public void Delete_from_type_where_key_filters_by_convention()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .DeleteFrom<User>()
            .WhereKey(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("DELETE FROM [Users] WHERE [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Where_key_uses_key_attribute_before_name_convention()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update(new KeyedUser { TenantUserId = 42, Id = 7, Name = "Alice" })
            .ToCommand();

        Assert.Equal("UPDATE [tenant_users] SET [Id] = @p0, [Name] = @p1 WHERE [tenant_user_id] = @p2", command.Sql);
        Assert.Equal(7, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
        Assert.Equal(42, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Where_key_rejects_missing_key_metadata()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update(new NoKeyUser { Name = "Alice" })
                .ToCommand());

        Assert.Contains("does not have a key", ex.Message);
    }

    [Fact]
    public void Where_key_rejects_composite_key_metadata()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update(new CompositeKeyUser { TenantId = 1, UserId = 2, Name = "Alice" })
                .ToCommand());

        Assert.Contains("Composite keys are not supported", ex.Message);
    }

    [Fact]
    public void Where_key_uses_type_name_id_convention()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Update(new ConventionUser { ConventionUserId = 5, Name = "Alice" })
            .ToCommand();

        Assert.Equal("UPDATE [ConventionUser] SET [Name] = @p0 WHERE [ConventionUserId] = @p1", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
        Assert.Equal(5, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Entity_command_entry_points_reject_primitive_values()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.SqlServer);

        var insertEx = Assert.Throws<ArgumentException>(() => database.Insert<string>(entity: "Users").ToCommand());
        var updateEx = Assert.Throws<ArgumentException>(() => database.Update<string>(entity: "Users").ToCommand());
        var deleteEx = Assert.Throws<ArgumentException>(() => database.Delete<string>(entity: "Users").ToCommand());
        var insertManyEx = Assert.Throws<ArgumentException>(() => database.InsertMany(new[] { "Users" }).ToCommands());
        var updateManyEx = Assert.Throws<ArgumentException>(() => database.UpdateMany(new[] { "Users" }).ToCommands());
        var deleteManyEx = Assert.Throws<ArgumentException>(() => database.DeleteMany(new[] { "Users" }).ToCommands());

        Assert.Contains("does not have any mapped columns", insertEx.Message);
        Assert.Contains("does not have any mapped columns", updateEx.Message);
        Assert.Contains("does not have any mapped columns", deleteEx.Message);
        Assert.Contains("does not have any mapped columns", insertManyEx.Message);
        Assert.Contains("does not have any mapped columns", updateManyEx.Message);
        Assert.Contains("does not have any mapped columns", deleteManyEx.Message);
    }

    [Fact]
    public void Single_entity_command_entry_points_reject_sequence_values()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.SqlServer);
        var users = new List<User> { new() { Id = 1, Name = "Alice" } };

        AssertSequenceRejected(() => database.Insert(users).ToCommand(), "InsertMany");
        AssertSequenceRejected(() => database.Insert(users, table: "users_staging").ToCommand(), "InsertMany");
        AssertSequenceRejected(() => database.InsertOrIgnore(users).ToCommand(), "InsertOrIgnoreMany");
        AssertSequenceRejected(() => database.InsertOrIgnore(users, table: "users_staging").ToCommand(), "InsertOrIgnoreMany");
        AssertSequenceRejected(() => database.InsertOrUpdate(users).ToCommand(), "InsertOrUpdateMany");
        AssertSequenceRejected(() => database.InsertOrUpdate(users, table: "users_staging").ToCommand(), "InsertOrUpdateMany");
        AssertSequenceRejected(() => database.Update(users).ToCommand(), "UpdateMany");
        AssertSequenceRejected(() => database.Update(users, table: "users_2026").ToCommand(), "UpdateMany");
        AssertSequenceRejected(() => database.Delete(users).ToCommand(), "DeleteMany");
        AssertSequenceRejected(() => database.Delete(users, table: "users_2026").ToCommand(), "DeleteMany");
    }

    [Fact]
    public void Typed_model_entry_points_reject_sequence_model_types()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.SqlServer);

        var selectEx = Assert.Throws<ArgumentException>(() => database.SelectFrom<List<User>>().ToCommand());
        var insertEx = Assert.Throws<ArgumentException>(() => database.InsertInto<List<User>>().Value("Capacity", 1).ToCommand());
        var updateEx = Assert.Throws<ArgumentException>(() => database.Update<List<User>>().Set("Capacity", 1).AllowAllRows().ToCommand());
        var deleteEx = Assert.Throws<ArgumentException>(() => database.DeleteFrom<List<User>>().AllowAllRows().ToCommand());

        Assert.Contains("is a sequence type", selectEx.Message);
        Assert.Contains("is a sequence type", insertEx.Message);
        Assert.Contains("is a sequence type", updateEx.Message);
        Assert.Contains("is a sequence type", deleteEx.Message);
    }

    [Fact]
    public void Where_key_rejects_default_key_value()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .Update(new User { Id = 0, Name = "Alice" })
                .ToCommand());

        Assert.Contains("default key value", ex.Message);
    }

    [Fact]
    public void Delete_where_key_rejects_default_key_value()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .DeleteFrom<User>()
                .WhereKey(new User { Id = 0, Name = "Alice" })
                .ToCommand());

        Assert.Contains("default key value", ex.Message);
    }

    [Fact]
    public void Entity_value_methods_reject_null_entity()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.SqlServer);

        Assert.Throws<ArgumentNullException>(() => database.InsertInto<User>().Values(null!));
        Assert.Throws<ArgumentNullException>(() => database.Update<User>().WhereKey(null!));
        Assert.Throws<ArgumentNullException>(() => database.DeleteFrom<User>().WhereKey(null!));
        Assert.Throws<ArgumentNullException>(() => database.Insert<User>(null!));
        Assert.Throws<ArgumentNullException>(() => database.Insert<User>(null!, table: "Users"));
        Assert.Throws<ArgumentNullException>(() => database.Update<User>(entity: null!));
        Assert.Throws<ArgumentNullException>(() => database.Update<User>(entity: null!, table: "Users"));
        Assert.Throws<ArgumentNullException>(() => database.Delete<User>(null!));
        Assert.Throws<ArgumentNullException>(() => database.Delete<User>(null!, table: "Users"));
        Assert.Throws<ArgumentNullException>(() => database.InsertMany<User>(null!).ToCommands());
        Assert.Throws<ArgumentNullException>(() => database.UpdateMany<User>(null!).ToCommands());
        Assert.Throws<ArgumentNullException>(() => database.DeleteMany<User>(null!).ToCommands());
        Assert.Throws<ArgumentNullException>(() => database.InsertMany([new User { Id = 1, Name = "Alice" }, null!]).ToCommands());
        Assert.Throws<ArgumentNullException>(() => database.UpdateMany([new User { Id = 1, Name = "Alice" }, null!]).ToCommands());
        Assert.Throws<ArgumentNullException>(() => database.DeleteMany([new User { Id = 1, Name = "Alice" }, null!]).ToCommands());
    }

    [Fact]
    public void Many_entity_command_entry_points_return_no_commands_for_empty_sequences()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.SqlServer);

        Assert.Empty(database.InsertMany(Array.Empty<User>()).ToCommands());
        Assert.Empty(database.UpdateMany(Array.Empty<User>()).ToCommands());
        Assert.Empty(database.DeleteMany(Array.Empty<User>()).ToCommands());
    }

    [Fact]
    public void Many_entity_command_entry_points_reject_invalid_table_identifiers_when_rendered()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.SqlServer);

        var insertEx = Assert.Throws<ArgumentException>(() =>
            database.InsertMany([new User { Id = 1, Name = "Alice" }], table: "users;drop table Users").ToCommands());
        var updateEx = Assert.Throws<ArgumentException>(() =>
            database.UpdateMany([new User { Id = 1, Name = "Alice" }], table: "users;drop table Users").ToCommands());
        var deleteEx = Assert.Throws<ArgumentException>(() =>
            database.DeleteMany([new User { Id = 1, Name = "Alice" }], table: "users;drop table Users").ToCommands());

        Assert.Contains("Invalid SQL identifier", insertEx.Message);
        Assert.Contains("Invalid SQL identifier", updateEx.Message);
        Assert.Contains("Invalid SQL identifier", deleteEx.Message);
    }

    [Fact]
    public void Update_many_rejects_missing_composite_and_default_keys()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.SqlServer);

        var missingKeyEx = Assert.Throws<InvalidOperationException>(() =>
            database.UpdateMany([new NoKeyUser { Name = "Alice" }]).ToCommands());
        var compositeKeyEx = Assert.Throws<InvalidOperationException>(() =>
            database.UpdateMany([new CompositeKeyUser { TenantId = 1, UserId = 2, Name = "Alice" }]).ToCommands());
        var defaultKeyEx = Assert.Throws<InvalidOperationException>(() =>
            database.UpdateMany([new User { Id = 0, Name = "Alice" }]).ToCommands());

        Assert.Contains("does not have a key", missingKeyEx.Message);
        Assert.Contains("Composite keys are not supported", compositeKeyEx.Message);
        Assert.Contains("default key value", defaultKeyEx.Message);
    }

    [Fact]
    public void Delete_many_rejects_missing_composite_and_default_keys()
    {
        var database = Db4NetDatabase.Create(Db4NetOptions.SqlServer);

        var missingKeyEx = Assert.Throws<InvalidOperationException>(() =>
            database.DeleteMany([new NoKeyUser { Name = "Alice" }]).ToCommands());
        var compositeKeyEx = Assert.Throws<InvalidOperationException>(() =>
            database.DeleteMany([new CompositeKeyUser { TenantId = 1, UserId = 2, Name = "Alice" }]).ToCommands());
        var defaultKeyEx = Assert.Throws<InvalidOperationException>(() =>
            database.DeleteMany([new User { Id = 0, Name = "Alice" }]).ToCommands());

        Assert.Contains("does not have a key", missingKeyEx.Message);
        Assert.Contains("Composite keys are not supported", compositeKeyEx.Message);
        Assert.Contains("default key value", defaultKeyEx.Message);
    }

    [Fact]
    public void Select_count_from_type_renders_count_with_typed_filters()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectCountFrom<User>()
            .Where(u => u.Id, Op.Gte, 1)
            .WhereGroup(group => group
                .Where(u => u.Name, Op.Like, "A%")
                .OrWhere(u => u.Id, Op.Eq, 2))
            .OrWhereGroup(group => group
                .Where(u => u.Name, Op.Eq, "Bob")
                .OrWhere(u => u.Id, Op.Eq, 3))
            .ToCommand();

        Assert.Equal("""SELECT COUNT(*) FROM "Users" WHERE "Id" >= @p0 AND ("Name" LIKE @p1 OR "Id" = @p2) OR ("Name" = @p3 OR "Id" = @p4)""", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("A%", command.Parameters.Get<string>("p1"));
        Assert.Equal(2, command.Parameters.Get<int>("p2"));
        Assert.Equal("Bob", command.Parameters.Get<string>("p3"));
        Assert.Equal(3, command.Parameters.Get<int>("p4"));
    }

    [Fact]
    public void Select_count_from_type_can_override_table_with_model_mapping()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectCountFrom<MappedUser>("app_users_staging")
            .Where(u => u.DisplayName, Op.Eq, "Alice")
            .ToCommand();

        Assert.Equal("""SELECT COUNT(*) FROM "app_users_staging" WHERE "display_name" = @p0""", command.Sql);
        Assert.Equal("Alice", command.Parameters.Get<string>("p0"));
    }

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    private static void AssertSequenceRejected(Action action, string expectedAlternative)
    {
        var ex = Assert.Throws<ArgumentException>(action);

        Assert.Contains("is a sequence type", ex.Message);
        Assert.Contains(expectedAlternative, ex.Message);
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

    [Table("tenant_users")]
    private sealed class KeyedUser
    {
        [Key]
        [Column("tenant_user_id")]
        public int TenantUserId { get; set; }

        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    private sealed class NoKeyUser
    {
        public string Name { get; set; } = "";
    }

    [Table("generated_users")]
    private sealed class GeneratedKeyUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    [Table("generated_audit_users")]
    private sealed class GeneratedAuditUser
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class CompositeKeyUser
    {
        [Key]
        public int TenantId { get; set; }

        [Key]
        public int UserId { get; set; }

        public string Name { get; set; } = "";
    }

    private sealed class ConventionUser
    {
        public int ConventionUserId { get; set; }

        public string Name { get; set; } = "";
    }
}
