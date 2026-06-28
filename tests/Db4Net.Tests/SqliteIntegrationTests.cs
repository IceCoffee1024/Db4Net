using Db4Net;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace Db4Net.Tests;

public sealed class SqliteIntegrationTests
{
    [Fact]
    public void Query_single_or_default_executes_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 2)
            .QuerySingleOrDefault();

        Assert.NotNull(user);
        Assert.Equal(2, user.Id);
        Assert.Equal("Bob", user.Name);
    }

    [Fact]
    public async Task Query_single_or_default_async_executes_parameterized_sql_with_dapper()
    {
        await using var connection = CreateOpenConnection();

        var user = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 2)
            .QuerySingleOrDefaultAsync();

        Assert.NotNull(user);
        Assert.Equal(2, user.Id);
        Assert.Equal("Bob", user.Name);
    }

    [Fact]
    public void Query_executes_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var users = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Gt, 0)
            .OrderBy(u => u.Id)
            .Query()
            .ToList();

        Assert.Collection(
            users,
            user => Assert.Equal("Alice", user.Name),
            user => Assert.Equal("Bob", user.Name));
    }

    [Fact]
    public async Task Query_async_executes_parameterized_sql_with_dapper()
    {
        await using var connection = CreateOpenConnection();

        var users = (await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Gt, 0)
            .OrderBy(u => u.Id)
            .QueryAsync())
            .ToList();

        Assert.Collection(
            users,
            user => Assert.Equal("Alice", user.Name),
            user => Assert.Equal("Bob", user.Name));
    }

    [Fact]
    public void Select_count_execute_returns_filtered_count()
    {
        using var connection = CreateOpenConnection();

        var count = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectCountFrom<User>()
            .Where(u => u.Id, Op.Gt, 1)
            .Execute();

        Assert.Equal(1L, count);
    }

    [Fact]
    public async Task Select_count_execute_async_returns_filtered_count_from_explicit_table()
    {
        await using var connection = CreateOpenShardedConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        db.InsertMany(
            [
                new MappedUser { Id = 1, DisplayName = "Alice" },
                new MappedUser { Id = 2, DisplayName = "Bob" },
            ],
            table: "app_users_staging")
            .Execute();

        var count = await db
            .SelectCountFrom<MappedUser>("app_users_staging")
            .Where(u => u.DisplayName, Op.Like, "A%")
            .ExecuteAsync();

        Assert.Equal(1L, count);
    }

    [Fact]
    public void Select_count_uses_transaction_from_execution_options()
    {
        using var connection = CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "insert into Users (Id, Name) values (3, 'Charlie');";
        insert.ExecuteNonQuery();

        var countInTransaction = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectCountFrom<User>()
            .Where(u => u.Id, Op.Gt, 2)
            .Execute(new Db4NetExecutionOptions { Transaction = transaction });

        transaction.Rollback();

        var countAfterRollback = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectCountFrom<User>()
            .Where(u => u.Id, Op.Gt, 2)
            .Execute();

        Assert.Equal(1L, countInTransaction);
        Assert.Equal(0L, countAfterRollback);
    }

    [Fact]
    public void Select_count_transaction_extension_uses_transaction_scope()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
        using var transaction = db.BeginTransaction();

        transaction
            .Insert(new User { Id = 3, Name = "Charlie" })
            .Execute();

        var countInTransaction = transaction
            .SelectCountFrom<User>()
            .Where(u => u.Id, Op.Gt, 2)
            .Execute(new Db4NetExecutionOptions { CommandTimeout = 30 });

        transaction.Rollback();

        var countAfterRollback = db
            .SelectCountFrom<User>()
            .Where(u => u.Id, Op.Gt, 2)
            .Execute();

        Assert.Equal(1L, countInTransaction);
        Assert.Equal(0L, countAfterRollback);
    }

    [Fact]
    public async Task Select_count_execute_async_uses_cancellation_token()
    {
        await using var connection = CreateOpenConnection();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connection
                .UseDb4Net(Db4NetOptions.Sqlite)
                .SelectCountFrom<User>()
                .ExecuteAsync(cancellationToken: cancellation.Token));
    }

    [Fact]
    public void Query_uses_transaction_from_command_options()
    {
        using var connection = CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "insert into Users (Id, Name) values (3, 'Charlie');";
        insert.ExecuteNonQuery();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault(new Db4NetExecutionOptions { Transaction = transaction });

        Assert.NotNull(user);
        Assert.Equal("Charlie", user.Name);

        transaction.Rollback();

        var afterRollback = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.Null(afterRollback);
    }

    [Fact]
    public async Task Query_async_uses_cancellation_token()
    {
        await using var connection = CreateOpenConnection();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connection
                .UseDb4Net(Db4NetOptions.Sqlite)
                .SelectFrom<User>()
                .QueryAsync(cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task Query_async_accepts_command_options_and_cancellation_token()
    {
        await using var connection = CreateOpenConnection();
        await using var transaction = await connection.BeginTransactionAsync();

        var user = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .QuerySingleOrDefaultAsync(
                new Db4NetExecutionOptions { Transaction = transaction },
                CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void Query_first_or_default_returns_default_when_no_row_exists()
    {
        using var connection = CreateOpenConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 99)
            .QueryFirstOrDefault();

        Assert.Null(user);
    }

    [Fact]
    public async Task Query_first_or_default_async_returns_default_when_no_row_exists()
    {
        await using var connection = CreateOpenConnection();

        var user = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 99)
            .QueryFirstOrDefaultAsync();

        Assert.Null(user);
    }

    [Fact]
    public void Insert_command_executes_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var affected = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .InsertInto<User>()
            .Value(u => u.Id, 3)
            .Value(u => u.Name, "Charlie")
            .Execute();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.Equal(1, affected);
        Assert.NotNull(user);
        Assert.Equal("Charlie", user.Name);
    }

    [Fact]
    public void Update_command_executes_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();

        var affected = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .Update<User>()
            .Set(u => u.Name, "Alicia")
            .Where(u => u.Id, Op.Eq, 1)
            .Execute();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .QuerySingleOrDefault();

        Assert.Equal(1, affected);
        Assert.NotNull(user);
        Assert.Equal("Alicia", user.Name);
    }

    [Fact]
    public async Task Delete_command_async_executes_parameterized_sql_with_dapper()
    {
        await using var connection = CreateOpenConnection();

        var affected = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .DeleteFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .ExecuteAsync();

        var user = await connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .QuerySingleOrDefaultAsync();

        Assert.Equal(1, affected);
        Assert.Null(user);
    }

    [Fact]
    public void Entity_command_conveniences_execute_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var inserted = db
            .Insert(new User { Id = 3, Name = "Charlie" })
            .Execute();

        var updated = db
            .Update(new User { Id = 3, Name = "Charles" })
            .Execute();

        var user = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        var deleted = db
            .Delete(new User { Id = 3, Name = "Charles" })
            .Execute();

        var afterDelete = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.Equal(1, inserted);
        Assert.Equal(1, updated);
        Assert.NotNull(user);
        Assert.Equal("Charles", user.Name);
        Assert.Equal(1, deleted);
        Assert.Null(afterDelete);
    }

    [Fact]
    public void Entity_commands_use_transaction_from_execution_options()
    {
        using var connection = CreateOpenConnection();
        using var transaction = connection.BeginTransaction();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
        var options = new Db4NetExecutionOptions { Transaction = transaction };

        var inserted = db
            .Insert(new User { Id = 3, Name = "Charlie" })
            .Execute(options);

        var updated = db
            .Update(new User { Id = 3, Name = "Charles" })
            .Execute(options);

        var userInTransaction = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault(options);

        var deleted = db
            .Delete(new User { Id = 3, Name = "Charles" })
            .Execute(options);

        var afterDeleteInTransaction = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault(options);

        transaction.Rollback();

        var afterRollback = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.Equal(1, inserted);
        Assert.Equal(1, updated);
        Assert.NotNull(userInTransaction);
        Assert.Equal("Charles", userInTransaction.Name);
        Assert.Equal(1, deleted);
        Assert.Null(afterDeleteInTransaction);
        Assert.Null(afterRollback);
    }

    [Fact]
    public void With_transaction_applies_transaction_to_terminal_methods()
    {
        using var connection = CreateOpenConnection();
        using var transaction = connection.BeginTransaction();
        var db = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .WithTransaction(transaction);

        var inserted = db
            .Insert(new User { Id = 3, Name = "Charlie" })
            .Execute();

        var userInTransaction = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        transaction.Rollback();

        var afterRollback = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.Equal(1, inserted);
        Assert.NotNull(userInTransaction);
        Assert.Equal("Charlie", userInTransaction.Name);
        Assert.Null(afterRollback);
    }

    [Fact]
    public void With_transaction_preserves_transaction_when_call_options_override_timeout()
    {
        using var connection = CreateOpenConnection();
        using var transaction = connection.BeginTransaction();
        var db = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .WithTransaction(transaction);

        db.Insert(new User { Id = 3, Name = "Charlie" })
            .Execute(new Db4NetExecutionOptions { CommandTimeout = 30 });

        var userInTransaction = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault(new Db4NetExecutionOptions { CommandTimeout = 30 });

        transaction.Rollback();

        var afterRollback = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.NotNull(userInTransaction);
        Assert.Equal("Charlie", userInTransaction.Name);
        Assert.Null(afterRollback);
    }

    [Fact]
    public void Begin_transaction_commits_when_commit_is_called()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        using (var transaction = db.BeginTransaction())
        {
            transaction
                .Insert(new User { Id = 3, Name = "Charlie" })
                .Execute();

            transaction.Commit();
        }

        var user = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.NotNull(user);
        Assert.Equal("Charlie", user.Name);
    }

    [Fact]
    public void Begin_transaction_rolls_back_when_disposed_without_commit()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        using (var transaction = db.BeginTransaction())
        {
            transaction
                .Insert(new User { Id = 3, Name = "Charlie" })
                .Execute();
        }

        var user = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.Null(user);
    }

    [Fact]
    public void Begin_transaction_rolls_back_when_rollback_is_called()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        using (var transaction = db.BeginTransaction())
        {
            transaction
                .Insert(new User { Id = 3, Name = "Charlie" })
                .Execute();

            transaction.Rollback();
        }

        var user = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.Null(user);
    }

    [Fact]
    public void Begin_transaction_rejects_completion_after_already_completed()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
        using var transaction = db.BeginTransaction();

        transaction.Rollback();

        var ex = Assert.Throws<InvalidOperationException>(() => transaction.Commit());

        Assert.Contains("already been completed", ex.Message);
    }

    [Fact]
    public void Transaction_rejects_command_execution_after_commit()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
        using var transaction = db.BeginTransaction();
        var transactionDatabase = transaction.Database;

        transaction.Commit();

        var extensionEx = Assert.Throws<InvalidOperationException>(() =>
            transaction
                .Insert(new User { Id = 3, Name = "Charlie" })
                .Execute());
        var capturedFacadeEx = Assert.Throws<InvalidOperationException>(() =>
            transactionDatabase
                .Insert(new User { Id = 4, Name = "Dana" })
                .Execute());

        Assert.Contains("already been completed", extensionEx.Message);
        Assert.Contains("already been completed", capturedFacadeEx.Message);
    }

    [Fact]
    public void Transaction_rejects_database_access_after_dispose()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
        var transaction = db.BeginTransaction();

        transaction.Dispose();

        Assert.Throws<ObjectDisposedException>(() => transaction.Database);
    }

    [Fact]
    public void Execute_in_transaction_commits_when_delegate_succeeds()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        db.ExecuteInTransaction(transaction =>
        {
            transaction
                .Insert(new User { Id = 3, Name = "Charlie" })
                .Execute();

            transaction
                .Update(new User { Id = 3, Name = "Charles" })
                .Execute();
        });

        var user = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.NotNull(user);
        Assert.Equal("Charles", user.Name);
    }

    [Fact]
    public void Execute_in_transaction_rolls_back_when_delegate_throws()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        Assert.Throws<InvalidOperationException>(() =>
            db.ExecuteInTransaction(transaction =>
            {
                transaction
                    .Insert(new User { Id = 3, Name = "Charlie" })
                    .Execute();

                throw new InvalidOperationException("stop");
            }));

        var user = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefault();

        Assert.Null(user);
    }

    [Fact]
    public async Task Execute_in_transaction_async_commits_when_delegate_succeeds()
    {
        await using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        await db.ExecuteInTransactionAsync(async transaction =>
        {
            await transaction
                .Insert(new User { Id = 3, Name = "Charlie" })
                .ExecuteAsync();

            await transaction
                .Update(new User { Id = 3, Name = "Charles" })
                .ExecuteAsync();
        });

        var user = await db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefaultAsync();

        Assert.NotNull(user);
        Assert.Equal("Charles", user.Name);
    }

    [Fact]
    public async Task Execute_in_transaction_async_rolls_back_when_delegate_throws()
    {
        await using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.ExecuteInTransactionAsync(async transaction =>
            {
                await transaction
                    .Insert(new User { Id = 3, Name = "Charlie" })
                    .ExecuteAsync();

                throw new InvalidOperationException("stop");
            }));

        var user = await db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 3)
            .QuerySingleOrDefaultAsync();

        Assert.Null(user);
    }

    [Fact]
    public void Transaction_scope_requires_bound_connection()
    {
        var db = Db4NetDatabase.Create(Db4NetOptions.Sqlite);

        var beginEx = Assert.Throws<InvalidOperationException>(() => db.BeginTransaction());
        var executeEx = Assert.Throws<InvalidOperationException>(() => db.ExecuteInTransaction(_ => { }));

        Assert.Contains("Dapper execution requires an IDbConnection", beginEx.Message);
        Assert.Contains("Dapper execution requires an IDbConnection", executeEx.Message);
    }

    [Fact]
    public void Transaction_scope_applies_to_many_commands()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        using (var transaction = db.BeginTransaction())
        {
            transaction
                .InsertMany(
                [
                    new User { Id = 3, Name = "Charlie" },
                    new User { Id = 4, Name = "Dana" },
                ])
                .Execute();

            transaction
                .UpdateMany(
                [
                    new User { Id = 3, Name = "Charles" },
                    new User { Id = 4, Name = "Daphne" },
                ])
                .Execute();

            transaction
                .DeleteMany(
                [
                    new User { Id = 1, Name = "Alice" },
                    new User { Id = 2, Name = "Bob" },
                ])
                .Execute();

            transaction.Rollback();
        }

        var users = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.In, new[] { 1, 2, 3, 4 })
            .OrderBy(u => u.Id)
            .Query()
            .ToList();

        Assert.Collection(
            users,
            user => Assert.Equal("Alice", user.Name),
            user => Assert.Equal("Bob", user.Name));
    }

    [Fact]
    public void Transaction_scope_applies_to_conflict_many_commands()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        using (var transaction = db.BeginTransaction())
        {
            transaction
                .InsertOrIgnoreMany(
                [
                    new User { Id = 1, Name = "Ignored" },
                    new User { Id = 3, Name = "Charlie" },
                ])
                .Execute();

            transaction
                .InsertOrUpdateMany(
                [
                    new User { Id = 2, Name = "Bobby" },
                    new User { Id = 4, Name = "Dana" },
                ])
                .Execute();

            transaction.Rollback();
        }

        var users = db
            .SelectFrom<User>()
            .OrderBy(u => u.Id)
            .Query()
            .ToList();

        Assert.Collection(
            users,
            user => Assert.Equal("Alice", user.Name),
            user => Assert.Equal("Bob", user.Name));
    }

    [Fact]
    public void Many_entity_command_conveniences_execute_parameterized_sql_with_dapper()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var inserted = db
            .InsertMany(
            [
                new User { Id = 3, Name = "Charlie" },
                new User { Id = 4, Name = "Dana" },
            ])
            .Execute();

        var updated = db
            .UpdateMany(
            [
                new User { Id = 3, Name = "Charles" },
                new User { Id = 4, Name = "Daphne" },
            ])
            .Execute();

        var users = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.In, new[] { 3, 4 })
            .OrderBy(u => u.Id)
            .Query()
            .ToList();

        var deleted = db
            .DeleteMany(
            [
                new User { Id = 3, Name = "Charles" },
                new User { Id = 4, Name = "Daphne" },
            ])
            .Execute();

        var remaining = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.In, new[] { 3, 4 })
            .Query()
            .ToList();

        Assert.Equal(2, inserted);
        Assert.Equal(2, updated);
        Assert.Collection(
            users,
            user => Assert.Equal("Charles", user.Name),
            user => Assert.Equal("Daphne", user.Name));
        Assert.Equal(2, deleted);
        Assert.Empty(remaining);
    }

    [Fact]
    public void Many_entity_command_table_overrides_execute_against_explicit_tables_with_model_mapping()
    {
        using var connection = CreateOpenShardedConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var inserted = db
            .InsertMany(
            [
                new MappedUser { Id = 1, DisplayName = "Alice" },
                new MappedUser { Id = 2, DisplayName = "Bob" },
            ],
            table: "app_users_staging")
            .Execute();

        var updated = db
            .UpdateMany(
            [
                new MappedUser { Id = 1, DisplayName = "Alicia" },
                new MappedUser { Id = 2, DisplayName = "Bobby" },
            ],
            table: "app_users_staging")
            .Execute();

        var users = db
            .SelectFrom<MappedUser>("app_users_staging")
            .OrderBy(u => u.Id)
            .Query()
            .ToList();

        var deleted = db
            .DeleteMany(
            [
                new MappedUser { Id = 1, DisplayName = "Alicia" },
                new MappedUser { Id = 2, DisplayName = "Bobby" },
            ],
            table: "app_users_staging")
            .Execute();

        var afterDelete = db
            .SelectFrom<MappedUser>("app_users_staging")
            .Query()
            .ToList();

        Assert.Equal(2, inserted);
        Assert.Equal(2, updated);
        Assert.Collection(
            users,
            user => Assert.Equal("Alicia", user.DisplayName),
            user => Assert.Equal("Bobby", user.DisplayName));
        Assert.Equal(2, deleted);
        Assert.Empty(afterDelete);
    }

    [Fact]
    public void Empty_many_entity_command_conveniences_return_zero_without_executing()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        Assert.Equal(0, db.InsertMany(Array.Empty<User>()).Execute());
        Assert.Equal(0, db.UpdateMany(Array.Empty<User>()).Execute());
        Assert.Equal(0, db.DeleteMany(Array.Empty<User>()).Execute());
    }

    [Fact]
    public void Many_commands_use_transaction_from_execution_options()
    {
        using var connection = CreateOpenConnection();
        using var transaction = connection.BeginTransaction();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var inserted = db
            .InsertMany(
            [
                new User { Id = 3, Name = "Charlie" },
                new User { Id = 4, Name = "Dana" },
            ])
            .Execute(new Db4NetExecutionOptions { Transaction = transaction });

        var usersInTransaction = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.In, new[] { 3, 4 })
            .Query(new Db4NetExecutionOptions { Transaction = transaction })
            .ToList();

        transaction.Rollback();

        var usersAfterRollback = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.In, new[] { 3, 4 })
            .Query()
            .ToList();

        Assert.Equal(2, inserted);
        Assert.Equal(2, usersInTransaction.Count);
        Assert.Empty(usersAfterRollback);
    }

    [Fact]
    public void Update_many_validates_all_entities_before_executing()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            db.UpdateMany(
            [
                new User { Id = 1, Name = "Alicia" },
                new User { Id = 0, Name = "Invalid" },
            ])
            .Execute());

        var user = db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .QuerySingleOrDefault();

        Assert.Contains("default key value", ex.Message);
        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public async Task Delete_many_async_executes_parameterized_sql_with_dapper()
    {
        await using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var deleted = await db
            .DeleteMany(
            [
                new User { Id = 1, Name = "Alice" },
                new User { Id = 2, Name = "Bob" },
            ])
            .ExecuteAsync();

        var remaining = (await db
            .SelectFrom<User>()
            .QueryAsync())
            .ToList();

        Assert.Equal(2, deleted);
        Assert.Empty(remaining);
    }

    [Fact]
    public void Conflict_insert_conveniences_execute_with_sqlite()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var ignored = db
            .InsertOrIgnore(new User { Id = 1, Name = "Ignored" })
            .Execute();

        var upsertUpdated = db
            .InsertOrUpdate(new User { Id = 2, Name = "Bobby" })
            .Execute();

        var upsertInserted = db
            .InsertOrUpdate(new User { Id = 3, Name = "Charlie" })
            .Execute();

        var users = db
            .SelectFrom<User>()
            .OrderBy(u => u.Id)
            .Query()
            .ToList();

        Assert.Equal(0, ignored);
        Assert.Equal(1, upsertUpdated);
        Assert.Equal(1, upsertInserted);
        Assert.Collection(
            users,
            user => Assert.Equal("Alice", user.Name),
            user => Assert.Equal("Bobby", user.Name),
            user => Assert.Equal("Charlie", user.Name));
    }

    [Fact]
    public void Conflict_insert_many_conveniences_execute_with_sqlite()
    {
        using var connection = CreateOpenConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var ignoredAndInserted = db
            .InsertOrIgnoreMany(
            [
                new User { Id = 1, Name = "Ignored" },
                new User { Id = 3, Name = "Charlie" },
            ])
            .Execute();

        var updatedAndInserted = db
            .InsertOrUpdateMany(
            [
                new User { Id = 2, Name = "Bobby" },
                new User { Id = 4, Name = "Dana" },
            ])
            .Execute();

        var users = db
            .SelectFrom<User>()
            .OrderBy(u => u.Id)
            .Query()
            .ToList();

        Assert.Equal(1, ignoredAndInserted);
        Assert.Equal(2, updatedAndInserted);
        Assert.Collection(
            users,
            user => Assert.Equal("Alice", user.Name),
            user => Assert.Equal("Bobby", user.Name),
            user => Assert.Equal("Charlie", user.Name),
            user => Assert.Equal("Dana", user.Name));
    }

    [Fact]
    public void Conflict_insert_can_override_table_and_conflict_target_with_sqlite()
    {
        using var connection = CreateOpenUniqueConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var inserted = db
            .InsertOrUpdate(new UniqueUser { Id = 2, Email = "alice@example.com", Name = "Alicia" }, table: "unique_users_staging")
            .OnConflict(u => u.Email)
            .Update(u => u.Name)
            .Execute();

        var user = db
            .SelectFrom<UniqueUser>("unique_users_staging")
            .Where(u => u.Email, Op.Eq, "alice@example.com")
            .QuerySingleOrDefault();

        Assert.Equal(1, inserted);
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alicia", user.Name);
    }

    [Fact]
    public void Command_table_overrides_execute_against_explicit_tables_with_model_mapping()
    {
        using var connection = CreateOpenShardedConnection();
        var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

        var inserted = db
            .InsertInto<MappedUser>("app_users_staging")
            .Value(u => u.Id, 1)
            .Value(u => u.DisplayName, "Alice")
            .Execute();

        var updated = db
            .Update<MappedUser>("app_users_staging")
            .Set(u => u.DisplayName, "Alicia")
            .Where(u => u.Id, Op.Eq, 1)
            .Execute();

        var user = db
            .SelectFrom<MappedUser>("app_users_staging")
            .QuerySingleOrDefault();

        var deleted = db
            .DeleteFrom<MappedUser>("app_users_staging")
            .Where(u => u.Id, Op.Eq, 1)
            .Execute();

        var afterDelete = db
            .SelectFrom<MappedUser>("app_users_staging")
            .QuerySingleOrDefault();

        Assert.Equal(1, inserted);
        Assert.Equal(1, updated);
        Assert.NotNull(user);
        Assert.Equal("Alicia", user.DisplayName);
        Assert.Equal(1, deleted);
        Assert.Null(afterDelete);
    }

    [Fact]
    public void Typed_select_with_column_attribute_maps_result_to_property()
    {
        using var connection = CreateOpenMappedConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .Select<MappedUser>(u => u.DisplayName)
            .QuerySingleOrDefault();

        Assert.NotNull(user);
        Assert.Equal("Alice", user.DisplayName);
    }

    [Fact]
    public void Select_from_type_with_column_attribute_maps_result_to_property()
    {
        using var connection = CreateOpenMappedConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<MappedUser>()
            .QuerySingleOrDefault();

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.DisplayName);
        Assert.Equal("", user.Ignored);
    }

    [Fact]
    public void Select_from_type_excludes_not_mapped_property_even_when_table_has_matching_column()
    {
        using var connection = CreateOpenMappedConnection();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<MappedUser>()
            .QuerySingleOrDefault();

        Assert.NotNull(user);
        Assert.Equal("", user.Ignored);
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = """
            create table Users (Id integer primary key, Name text not null);
            insert into Users (Id, Name) values (1, 'Alice');
            insert into Users (Id, Name) values (2, 'Bob');
            """;
        setup.ExecuteNonQuery();
        return connection;
    }

    private static SqliteConnection CreateOpenMappedConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = """
            create table app_users (Id integer primary key, display_name text not null, Ignored text not null);
            insert into app_users (Id, display_name, Ignored) values (1, 'Alice', 'should-not-map');
            """;
        setup.ExecuteNonQuery();
        return connection;
    }

    private static SqliteConnection CreateOpenShardedConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = """
            create table app_users_staging (Id integer primary key, display_name text not null, Ignored text not null default '');
            """;
        setup.ExecuteNonQuery();
        return connection;
    }

    private static SqliteConnection CreateOpenUniqueConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = """
            create table unique_users_staging (Id integer primary key, Email text not null unique, Name text not null);
            insert into unique_users_staging (Id, Email, Name) values (1, 'alice@example.com', 'Alice');
            """;
        setup.ExecuteNonQuery();
        return connection;
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

    [Table("unique_users")]
    private sealed class UniqueUser
    {
        public int Id { get; set; }

        public string Email { get; set; } = "";

        public string Name { get; set; } = "";
    }
}
