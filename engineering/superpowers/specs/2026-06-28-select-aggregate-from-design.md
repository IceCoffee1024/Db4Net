# Select Aggregate From Design

## Goal

Add a long-term scalar-query shape that keeps `SelectCountFrom<T>()` and `SelectExistsFrom<T>()` as high-frequency public shortcuts, while adding one bounded aggregate entry point for less common scalar projections.

## Public API

Keep:

```csharp
db.SelectCountFrom<User>().Where(u => u.Id, Op.Gt, 0).Execute();
db.SelectExistsFrom<User>().Where(u => u.Id, Op.Eq, 1).Execute();
```

Add:

```csharp
db.SelectAggregateFrom<User>()
  .Max(u => u.Id)
  .Where(u => u.Name, Op.Like, "A%")
  .Execute();

db.SelectAggregateFrom<User>()
  .Min(u => u.Id)
  .Execute();

db.SelectAggregateFrom<User>()
  .CountDistinct(u => u.Name)
  .Execute();
```

`SelectAggregateFrom<T>(string table)` supports sharded/staging table overrides while still mapping columns from `T`.

Do not add `SelectMaxFrom<T>()`, `SelectMinFrom<T>()`, or `SelectCountDistinctFrom<T>()`. Those would create an unbounded method family and make the facade noisier over time.

## Result Types

`CountDistinct` returns `long` because SQL count is non-null.

`Max` and `Min` operate on mapped value-type columns and return `TValue?` because SQL aggregate functions return `NULL` for an empty result set. This preserves the database behavior instead of silently mapping empty sets to default values.

`Sum` and `Average` are intentionally deferred. Their result type rules vary more by provider and CLR numeric type, so adding them should be a separate design decision.

## Internal Design

Replace the duplicate count/exists query models and renderers with an internal scalar model:

```csharp
internal enum ScalarProjectionKind
{
    CountAll,
    Exists,
    CountDistinct,
    Max,
    Min
}

internal sealed class ScalarQueryModel
{
    public string? Table { get; set; }
    public ScalarProjectionKind ProjectionKind { get; set; }
    public string? Column { get; set; }
    public List<FilterNode> Filters { get; } = [];
}
```

`ScalarSqlRenderer` renders all scalar SQL shapes and reuses `FilterSqlRenderer` / `SqlParameterWriter`.

`ScalarQueryBuilderState<T>` owns typed/string filter mapping and group creation for scalar builders. Public builders keep their own methods and return types, but delegate filter state changes internally.

`DapperScalarExecutor` centralizes Dapper `ExecuteScalar` command creation and execution-option merging.

## Testing

Add command-rendering tests for:

- `Max`
- `Min`
- `CountDistinct`
- explicit table override
- string-property filters mapped as CLR property names
- grouped filters

Add SQLite integration tests for:

- max returning a value
- min returning a value
- count distinct returning a `long`
- max returning null when filters match no rows
- transaction extension using the transaction scope

Add API contract tests to pin:

- `Db4NetDatabase.SelectAggregateFrom<T>()`
- `Db4NetDatabase.SelectAggregateFrom<T>(string)`
- `Db4NetTransactionExtensions.SelectAggregateFrom<T>()`
- `Db4NetTransactionExtensions.SelectAggregateFrom<T>(string)`
- aggregate selector methods `Max`, `Min`, `CountDistinct`
- scalar aggregate terminal methods `Where`, `OrWhere`, `WhereGroup`, `OrWhereGroup`, `ToCommand`, `Execute`, `ExecuteAsync`

## Documentation

Update root README, package README, VitePress select pages, changelog, and package release notes to show the aggregate entry point without implying it is a row-query builder.
