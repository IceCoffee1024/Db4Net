using System;
using System.ComponentModel.DataAnnotations.Schema;
using Db4Net;

namespace Db4Net.Tests;

public sealed class FilterRenderingTests
{
    // ── Op.NotLike ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer", "[Name] NOT LIKE @p0")]
    [InlineData("Sqlite", "\"Name\" NOT LIKE @p0")]
    [InlineData("PostgreSql", "\"Name\" NOT LIKE @p0")]
    [InlineData("MySql", "`Name` NOT LIKE @p0")]
    public void Not_like_operator_renders_correctly_across_dialects(string dialect, string expectedPredicate)
    {
        var options = GetOptions(dialect);
        var command = Db4NetDatabase
            .Create(options)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.NotLike, "A%")
            .ToCommand();

        Assert.Contains(expectedPredicate, command.Sql);
        Assert.Equal("A%", command.Parameters.Get<string>("p0"));
    }

    // ── Op.NotIn (collection) ─────────────────────────────────────────────────

    [Fact]
    public void Not_in_operator_renders_parameterized_list()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.NotIn, new[] { 1, 2, 3 })
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] NOT IN (@p0, @p1, @p2)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal(2, command.Parameters.Get<int>("p1"));
        Assert.Equal(3, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Not_in_operator_rejects_empty_collection()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Id, Op.NotIn, Array.Empty<int>()));

        Assert.Contains("at least one value", ex.Message);
    }

    [Fact]
    public void Not_in_operator_rejects_string_value()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Name, Op.NotIn, "Alice"));

        Assert.Contains("non-string enumerable", ex.Message);
    }

    // ── Op.In validation now at builder stage ────────────────────────────────

    [Fact]
    public void In_operator_rejects_empty_collection_at_where_call_not_at_render()
    {
        // Validation now happens at Where() time, not ToCommand() time.
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Id, Op.In, Array.Empty<int>()));

        Assert.Contains("at least one value", ex.Message);
    }

    // ── WhereBetween ──────────────────────────────────────────────────────────

    [Fact]
    public void Where_between_renders_between_predicate()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .WhereBetween(u => u.Id, 10, 20)
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] BETWEEN @p0 AND @p1", command.Sql);
        Assert.Equal(10, command.Parameters.Get<int>("p0"));
        Assert.Equal(20, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Where_between_string_overload_renders_between_predicate()
    {
        // String-based overload: SelectFrom<User>("table") still selects all mapped columns.
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>("Users")
            .WhereBetween("Id", 5, 15)
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Id] BETWEEN @p0 AND @p1", command.Sql);
        Assert.Equal(5, command.Parameters.Get<int>("p0"));
        Assert.Equal(15, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Or_where_between_renders_or_between_predicate()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.Eq, "Alice")
            .OrWhereBetween(u => u.Id, 1, 5)
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Name] = @p0 OR [Id] BETWEEN @p1 AND @p2", command.Sql);
    }

    [Fact]
    public void Where_between_sqlite_uses_double_quoted_identifiers()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .WhereBetween(u => u.Id, 1, 100)
            .ToCommand();

        Assert.Equal("""SELECT "Id", "Name" FROM "Users" WHERE "Id" BETWEEN @p0 AND @p1""", command.Sql);
    }

    // ── SqlServer paging dead-code fix ────────────────────────────────────────

    [Fact]
    public void Sql_server_limit_without_explicit_offset_generates_valid_t_sql()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .OrderBy(u => u.Id)
            .Limit(10)
            .ToCommand();

        // When no Offset() is given, RenderOffsetBeforeLimit=true causes SelectSqlRenderer
        // to inject offset=0 as a parameter so T-SQL syntax is always valid.
        Assert.Contains("OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY", command.Sql);
        Assert.Equal(0, command.Parameters.Get<int>("p0"));
        Assert.Equal(10, command.Parameters.Get<int>("p1"));
    }

    [Fact]
    public void Sql_server_limit_with_explicit_offset_renders_both()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .OrderBy(u => u.Id)
            .Offset(20)
            .Limit(5)
            .ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] ORDER BY [Id] OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY", command.Sql);
        Assert.Equal(20, command.Parameters.Get<int>("p0"));
        Assert.Equal(5, command.Parameters.Get<int>("p1"));
    }

    // ── Fork() ────────────────────────────────────────────────────────────────

    [Fact]
    public void Fork_creates_independent_builder_that_does_not_affect_original()
    {
        var base_ = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.Eq, "Alice");

        var fork1 = base_.Fork().OrderBy(u => u.Id).Limit(10);
        var fork2 = base_.Fork().OrderBy(u => u.Name).Limit(5);

        var baseCmd = base_.ToCommand();
        var fork1Cmd = fork1.ToCommand();
        var fork2Cmd = fork2.ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Name] = @p0", baseCmd.Sql);
        Assert.Contains("ORDER BY [Id]", fork1Cmd.Sql);
        Assert.Contains("ORDER BY [Name]", fork2Cmd.Sql);
        Assert.DoesNotContain("ORDER BY", baseCmd.Sql);
    }

    [Fact]
    public void Typed_fork_preserves_type_safety()
    {
        var base_ = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.Eq, "Alice");

        var fork = base_.Fork();

        // Fork should return SelectQueryBuilder<User> — verify it has typed methods
        var cmd = fork.Where(u => u.Id, Op.Gt, 0).ToCommand();
        Assert.Contains("[Id] > @p1", cmd.Sql);
    }

    // ── MySql upsert uses row alias instead of deprecated VALUES() ─────────────

    [Fact]
    public void Mysql_insert_or_update_uses_row_alias_syntax()
    {
        var entity = new User { Id = 1, Name = "Alice" };
        var command = Db4NetDatabase
            .Create(Db4NetOptions.MySql)
            .InsertOrUpdate(entity)
            .ToCommand();

        Assert.Contains("AS _new", command.Sql);
        Assert.Contains("ON DUPLICATE KEY UPDATE", command.Sql);
        Assert.Contains("= _new.", command.Sql);
        Assert.DoesNotContain("VALUES(`", command.Sql);
    }

    [Fact]
    public void Mysql_insert_or_ignore_uses_insert_ignore_syntax()
    {
        var entity = new User { Id = 1, Name = "Bob" };
        var command = Db4NetDatabase
            .Create(Db4NetOptions.MySql)
            .InsertOrIgnore(entity)
            .ToCommand();

        Assert.StartsWith("INSERT IGNORE INTO", command.Sql);
    }

    // ── ISqlDialect.RequiresOrderByForPaging ────────────────────────────────

    [Fact]
    public void Sql_server_paging_without_order_by_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Limit(10)
                .ToCommand());

        Assert.Contains("requires ORDER BY", ex.Message);
    }

    [Theory]
    [InlineData("Sqlite")]
    [InlineData("PostgreSql")]
    [InlineData("MySql")]
    public void Non_sql_server_dialects_allow_limit_without_order_by(string dialect)
    {
        var command = Db4NetDatabase
            .Create(GetOptions(dialect))
            .SelectFrom<User>()
            .Limit(10)
            .ToCommand();

        Assert.Contains("LIMIT", command.Sql);
    }

    // ── WhereBetweenIf column-mapping regression ─────────────────────────────

    [Fact]
    public void Where_between_if_on_typed_builder_uses_column_attribute_not_property_name()
    {
        // Regression: SelectQueryBuilder<T>.WhereBetweenIf must override the base class
        // method so that column-name mapping (e.g. [Column("created_at")]) is applied.
        // Without the override, base.WhereBetweenIf → base.WhereBetween → uses the raw
        // property name "CreatedAt" instead of the mapped column name "created_at".
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectFrom<Article>()
            .WhereBetweenIf(true, "CreatedAt", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31))
            .ToCommand();

        Assert.Contains("\"created_at\" BETWEEN", command.Sql);
        Assert.DoesNotContain("\"CreatedAt\" BETWEEN", command.Sql);
    }

    [Fact]
    public void Or_where_between_if_on_typed_builder_uses_column_attribute()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectFrom<Article>()
            .Where(a => a.Id, Op.Eq, 1)
            .OrWhereBetweenIf(true, "CreatedAt", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31))
            .ToCommand();

        Assert.Contains("\"created_at\" BETWEEN", command.Sql);
        Assert.DoesNotContain("\"CreatedAt\" BETWEEN", command.Sql);
    }

    [Fact]
    public void Where_between_if_false_skips_filter_on_typed_builder()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .WhereBetweenIf(false, u => u.Id, 1, 100)
            .ToCommand();

        Assert.DoesNotContain("BETWEEN", command.Sql);
        Assert.Empty(command.Parameters.ParameterNames);
    }

    // ── L-3: WhereBetween cross-dialect ──────────────────────────────────────

    [Theory]
    [InlineData("SqlServer", "[Id] BETWEEN @p0 AND @p1")]
    [InlineData("Sqlite", "\"Id\" BETWEEN @p0 AND @p1")]
    [InlineData("PostgreSql", "\"Id\" BETWEEN @p0 AND @p1")]
    [InlineData("MySql", "`Id` BETWEEN @p0 AND @p1")]
    public void Where_between_renders_correctly_across_all_dialects(string dialect, string expectedPredicate)
    {
        var command = Db4NetDatabase
            .Create(GetOptions(dialect))
            .SelectFrom<User>()
            .WhereBetween(u => u.Id, 10, 20)
            .ToCommand();

        Assert.Contains(expectedPredicate, command.Sql);
        Assert.Equal(10, command.Parameters.Get<int>("p0"));
        Assert.Equal(20, command.Parameters.Get<int>("p1"));
    }

    [Theory]
    [InlineData("SqlServer", "[Id] NOT IN (@p0, @p1)")]
    [InlineData("Sqlite", "\"Id\" NOT IN (@p0, @p1)")]
    [InlineData("PostgreSql", "\"Id\" NOT IN (@p0, @p1)")]
    [InlineData("MySql", "`Id` NOT IN (@p0, @p1)")]
    public void Not_in_operator_renders_correctly_across_all_dialects(string dialect, string expectedPredicate)
    {
        var command = Db4NetDatabase
            .Create(GetOptions(dialect))
            .SelectFrom<User>()
            .Where(u => u.Id, Op.NotIn, new[] { 1, 2 })
            .ToCommand();

        Assert.Contains(expectedPredicate, command.Sql);
    }

    // ── L-3: Null guard validation ────────────────────────────────────────────

    [Fact]
    public void Like_operator_rejects_null_value()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Name, Op.Like, null));

        Assert.Contains("does not accept a null value", ex.Message);
    }

    [Fact]
    public void Not_like_operator_rejects_null_value()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .Where(u => u.Name, Op.NotLike, null));

        Assert.Contains("does not accept a null value", ex.Message);
    }

    [Fact]
    public void Where_between_rejects_null_low_bound()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .WhereBetween(u => u.Id, null, 20));

        Assert.Equal("low", ex.ParamName);
    }

    [Fact]
    public void Where_between_rejects_null_high_bound()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Db4NetDatabase
                .Create(Db4NetOptions.SqlServer)
                .SelectFrom<User>()
                .WhereBetween(u => u.Id, 10, null));

        Assert.Equal("high", ex.ParamName);
    }

    // ── L-4: Fork isolation ───────────────────────────────────────────────────

    [Fact]
    public void Fork_isolates_column_selection_changes()
    {
        var base_ = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>();

        var fork = base_.Fork().Select(u => u.Id);

        var baseCmd = base_.ToCommand();
        var forkCmd = fork.ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users]", baseCmd.Sql);
        Assert.Equal("SELECT [Id] FROM [Users]", forkCmd.Sql);
    }

    [Fact]
    public void Fork_isolates_limit_and_offset_changes()
    {
        var base_ = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.Eq, "Alice");

        var page1 = base_.Fork().Limit(10).Offset(0);
        var page2 = base_.Fork().Limit(10).Offset(10);

        var baseCmd = base_.ToCommand();
        var page1Cmd = page1.ToCommand();
        var page2Cmd = page2.ToCommand();

        Assert.DoesNotContain("LIMIT", baseCmd.Sql);
        Assert.Contains("LIMIT @p1 OFFSET @p2", page1Cmd.Sql);
        Assert.Contains("LIMIT @p1 OFFSET @p2", page2Cmd.Sql);
        Assert.Equal(0, page1Cmd.Parameters.Get<int>("p2"));
        Assert.Equal(10, page2Cmd.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void Fork_on_typed_builder_preserves_type_safety_and_isolation()
    {
        var base_ = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.Eq, "Alice");

        // Both forks add different Where clauses; base must remain unchanged.
        var fork1 = base_.Fork().Where(u => u.Id, Op.Gt, 0);
        var fork2 = base_.Fork().Where(u => u.Id, Op.Lt, 100);

        var baseCmd = base_.ToCommand();
        var fork1Cmd = fork1.ToCommand();
        var fork2Cmd = fork2.ToCommand();

        Assert.Equal("SELECT [Id], [Name] FROM [Users] WHERE [Name] = @p0", baseCmd.Sql);
        Assert.Contains("[Id] > @p1", fork1Cmd.Sql);
        Assert.Contains("[Id] < @p1", fork2Cmd.Sql);
        // Base should have only one WHERE condition (no [Id] predicate).
        Assert.DoesNotContain("[Id] >", baseCmd.Sql);
        Assert.DoesNotContain("[Id] <", baseCmd.Sql);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Db4NetOptions GetOptions(string dialect) => dialect switch
    {
        "SqlServer" => Db4NetOptions.SqlServer,
        "Sqlite" => Db4NetOptions.Sqlite,
        "PostgreSql" => Db4NetOptions.PostgreSql,
        "MySql" => Db4NetOptions.MySql,
        _ => throw new ArgumentException($"Unknown dialect: {dialect}")
    };

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Table("Articles")]
    private sealed class Article
    {
        public int Id { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
