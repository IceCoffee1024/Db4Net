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
    public void Sql_server_paging_requires_order_by()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select("Id")
            .From<User>("Users")
            .Limit(10)
            .ToCommand());

        Assert.Equal("SQL Server SELECT paging requires ORDER BY when Limit or Offset is used.", exception.Message);
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("Sqlite")]
    [InlineData("PostgreSql")]
    [InlineData("MySql")]
    public void Offset_without_limit_throws_instead_of_ignoring_offset(string dialectName)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Db4NetDatabase
            .Create(GetDialectOptions(dialectName))
            .Select("Id")
            .From<User>("Users")
            .OrderBy("Id")
            .Offset(20)
            .ToCommand());

        Assert.Equal("Offset requires Limit before rendering SELECT SQL.", exception.Message);
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

    [Fact]
    public void Sql_server_renders_insert_or_ignore_as_merge()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertOrIgnore(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("MERGE INTO [Users] WITH (HOLDLOCK) AS target USING (VALUES (@p0, @p1)) AS source ([Id], [Name]) ON target.[Id] = source.[Id] WHEN NOT MATCHED THEN INSERT ([Id], [Name]) VALUES (source.[Id], source.[Name]);", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Sql_server_renders_insert_or_update_as_merge()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .InsertOrUpdate(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("MERGE INTO [Users] WITH (HOLDLOCK) AS target USING (VALUES (@p0, @p1)) AS source ([Id], [Name]) ON target.[Id] = source.[Id] WHEN MATCHED THEN UPDATE SET [Name] = source.[Name] WHEN NOT MATCHED THEN INSERT ([Id], [Name]) VALUES (source.[Id], source.[Name]);", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p1"));
    }

    [Fact]
    public void Sqlite_renders_insert_or_ignore_and_insert_or_update()
    {
        var ignore = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .InsertOrIgnore(new User { Id = 1, Name = "Alice" })
            .ToCommand();
        var update = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .InsertOrUpdate(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("""INSERT INTO "Users" ("Id", "Name") VALUES (@p0, @p1) ON CONFLICT ("Id") DO NOTHING""", ignore.Sql);
        Assert.Equal("INSERT INTO \"Users\" (\"Id\", \"Name\") VALUES (@p0, @p1) ON CONFLICT (\"Id\") DO UPDATE SET \"Name\" = excluded.\"Name\"", update.Sql);
    }

    [Fact]
    public void Postgre_sql_renders_insert_or_ignore_and_insert_or_update()
    {
        var ignore = Db4NetDatabase
            .Create(Db4NetOptions.PostgreSql)
            .InsertOrIgnore(new User { Id = 1, Name = "Alice" })
            .ToCommand();
        var update = Db4NetDatabase
            .Create(Db4NetOptions.PostgreSql)
            .InsertOrUpdate(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("""INSERT INTO "Users" ("Id", "Name") VALUES (@p0, @p1) ON CONFLICT ("Id") DO NOTHING""", ignore.Sql);
        Assert.Equal("INSERT INTO \"Users\" (\"Id\", \"Name\") VALUES (@p0, @p1) ON CONFLICT (\"Id\") DO UPDATE SET \"Name\" = excluded.\"Name\"", update.Sql);
    }

    [Fact]
    public void My_sql_renders_insert_or_ignore_and_insert_or_update()
    {
        var ignore = Db4NetDatabase
            .Create(Db4NetOptions.MySql)
            .InsertOrIgnore(new User { Id = 1, Name = "Alice" })
            .ToCommand();
        var update = Db4NetDatabase
            .Create(Db4NetOptions.MySql)
            .InsertOrUpdate(new User { Id = 1, Name = "Alice" })
            .ToCommand();

        Assert.Equal("INSERT INTO `Users` (`Id`, `Name`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `Id` = `Id`", ignore.Sql);
        Assert.Equal("INSERT INTO `Users` (`Id`, `Name`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`)", update.Sql);
    }

    [Fact]
    public void My_sql_conflict_target_is_declared_but_not_rendered()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.MySql)
            .InsertOrUpdate(new UniqueUser { Id = 1, Email = "alice@example.com", Name = "Alice" })
            .OnConflict(u => u.Email)
            .Update(u => u.Name)
            .ToCommand();

        Assert.Equal("INSERT INTO `Users` (`Id`, `Email`, `Name`) VALUES (@p0, @p1, @p2) ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`)", command.Sql);
        Assert.DoesNotContain("Email) ON DUPLICATE", command.Sql);
    }

    [Fact]
    public void Insert_or_update_can_override_conflict_target_and_update_columns()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .InsertOrUpdate(new UniqueUser { Id = 1, Email = "alice@example.com", Name = "Alice" })
            .OnConflict(u => u.Email)
            .Update(u => u.Name)
            .ToCommand();

        Assert.Equal("INSERT INTO \"Users\" (\"Id\", \"Email\", \"Name\") VALUES (@p0, @p1, @p2) ON CONFLICT (\"Email\") DO UPDATE SET \"Name\" = excluded.\"Name\"", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal("alice@example.com", command.Parameters.Get<string>("p1"));
        Assert.Equal("Alice", command.Parameters.Get<string>("p2"));
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

    [Table("Users")]
    private sealed class UniqueUser
    {
        public int Id { get; set; }

        public string Email { get; set; } = "";

        public string Name { get; set; } = "";
    }

    private static Db4NetOptions GetDialectOptions(string dialectName)
    {
        return dialectName switch
        {
            "SqlServer" => Db4NetOptions.SqlServer,
            "Sqlite" => Db4NetOptions.Sqlite,
            "PostgreSql" => Db4NetOptions.PostgreSql,
            "MySql" => Db4NetOptions.MySql,
            _ => throw new ArgumentOutOfRangeException(nameof(dialectName), dialectName, "Unknown dialect name.")
        };
    }
}
