# Insert Return Key Design

## Context

Db4Net already has SQL-shaped insert builders and entity convenience methods:

```csharp
db.Insert(user).Execute();

db.InsertInto<User>()
    .Values(user)
    .Execute();
```

It also tracks mapped key columns through `[Key]` and `Id` / `{TypeName}Id` conventions, and skips database-generated columns from entity inserts. The missing capability is the common Dapper-adjacent workflow: insert one row and read the generated primary key without forcing users to hand-write provider-specific SQL.

The feature should stay narrower than a full `INSERT ... RETURNING` DSL. Db4Net is not trying to model every inserted row shape or multi-row return result.

## Decision

Add single-row, single-key insert return support only to `InsertCommandBuilder<T>`.

Primary convenience API:

```csharp
var id = db.Insert(user).ExecuteReturnKey<long>();
var id = db.Insert(user).ExecuteReturnKey<long>(u => u.Id);
var id = db.Insert(user, table: "users_staging").ExecuteReturnKey<long>();
```

SQL-shaped builder API:

```csharp
var id = db.InsertInto<User>()
    .Values(user)
    .ReturnKey(u => u.Id)
    .Execute<long>();
```

Async and execution options are supported on the same terminal surface:

```csharp
var id = await db.Insert(user).ExecuteReturnKeyAsync<long>();
var id = await db.Insert(user).ExecuteReturnKeyAsync<long>(u => u.Id);
var id = db.Insert(user).ExecuteReturnKey<long>(new Db4NetExecutionOptions { CommandTimeout = 30 });
```

## Key Selection Rules

- `ExecuteReturnKey<TResult>()` uses the model's default key.
- If no key exists, `ExecuteReturnKey<TResult>()` throws and tells the caller to add `[Key]` or an `Id` / `{TypeName}Id` property.
- If multiple keys exist, `ExecuteReturnKey<TResult>()` throws and tells the caller to specify a key selector.
- `ExecuteReturnKey<TResult>(...)` and `ReturnKey(...)` require the selector to target a mapped key column.
- Selecting a non-key mapped column throws.
- The builder returned by `ReturnKey(...)` does not expose another `ReturnKey(...)` method, so multi-column return rows are not representable.

Composite-key entities are therefore still valid for conflict inserts, but insert-return-key requires the caller to select one key explicitly.

## Rendering Rules

The renderer emits provider-specific scalar-return SQL from the same insert values that ordinary `INSERT` already uses.

SQL Server:

```sql
INSERT INTO [generated_users] ([Name]) OUTPUT INSERTED.[Id] VALUES (@p0)
```

PostgreSQL:

```sql
INSERT INTO "generated_users" ("Name") VALUES (@p0) RETURNING "Id"
```

SQLite:

```sql
INSERT INTO "generated_users" ("Name") VALUES (@p0) RETURNING "Id"
```

MySQL:

```sql
INSERT INTO `generated_users` (`Name`) VALUES (@p0); SELECT LAST_INSERT_ID()
```

For MySQL, if the selected key was explicitly included in the insert values, Db4Net can return the inserted parameter instead:

```sql
INSERT INTO `Users` (`Id`, `Name`) VALUES (@p0, @p1); SELECT @p0
```

This makes manually assigned keys deterministic while still supporting the common auto-increment identity case.

Provider caveats are explicit:

- MySQL `LAST_INSERT_ID()` is only used for `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` keys; non-inserted non-identity keys are not supported.
- SQLite `RETURNING` requires SQLite 3.35 or newer at runtime.
- SQL Server direct `OUTPUT INSERTED...` is for ordinary target tables; trigger-enabled tables can require `OUTPUT ... INTO`, which is out of scope for this feature.

## Execution Rules

- `Execute()` remains the affected-row-count terminal method inherited from `CommandBuilderBase`.
- `ExecuteReturnKey<TResult>()` uses scalar Dapper execution directly from `InsertCommandBuilder<T>`.
- `.ReturnKey(...)` returns a dedicated `InsertReturnKeyCommandBuilder<T>` that exposes only `ToCommand()`, `Execute<TResult>()`, and `ExecuteAsync<TResult>()`.
- `InsertCommandBuilder<T>.ToCommand()` always renders ordinary insert SQL; `InsertReturnKeyCommandBuilder<T>.ToCommand()` renders the return-key insert SQL.
- `ExecuteReturnKey<TResult>(...)` should not require callers to mutate the builder with `.ReturnKey(...)`.
- Existing transaction, timeout, command type, and cancellation token behavior must flow through `Db4NetExecutionOptions`.

## Explicit Non-Goals

This design does not add:

- `InsertMany(...).ExecuteReturnKeys(...)`
- return-key support for `InsertOrIgnore(...)` or `InsertOrUpdate(...)`
- multiple `.ReturnKey(...)` calls
- returning arbitrary non-key columns
- returning full inserted entities
- a general `RETURNING` projection DSL
- provider-specific escape hatches for raw returning SQL

Those features require broader result-shape and conflict semantics and should be designed separately.

## Documentation Requirements

The public docs should explain:

- `ExecuteReturnKey<TResult>()` is for single-row inserts.
- `[Key]` identifies which column is returned by default.
- `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` controls whether entity inserts omit a generated key value.
- Composite-key entities require an explicit key selector.
- MySQL support is identity-oriented when the selected key is generated by the database.
