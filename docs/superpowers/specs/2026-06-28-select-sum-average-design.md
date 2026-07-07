# Select Sum and Average Design

## Goal

Extend `SelectAggregateFrom<T>()` with SQL `SUM(...)` and `AVG(...)` while keeping aggregate result types explicit enough to avoid misleading cross-provider behavior.

## Public API

Add `Sum` with two forms:

```csharp
db.SelectAggregateFrom<Order>()
  .Sum(o => o.Amount)
  .Execute<decimal>();

db.SelectAggregateFrom<Order>()
  .Sum(o => o.Quantity)
  .Execute<long>();
```

`Sum(...)` builds the SQL projection. The terminal `Execute<TResult>()` or `ExecuteAsync<TResult>()` call chooses the scalar result type used by Dapper. Use a nullable result type, such as `decimal?`, when callers need to preserve SQL `NULL` for empty result sets.

Add `Average` with the same terminal result typing rule:

```csharp
db.SelectAggregateFrom<Order>()
  .Average(o => o.Quantity)
  .Execute<decimal>();
```

Do not infer an average result type from the selected member. For integer columns, an inferred `int?` average is mathematically misleading and provider behavior varies.

## Internal Design

Extend `ScalarProjectionKind` with `Sum` and `Average`.

Extend `ScalarSqlRenderer` to render:

```sql
SELECT SUM("Amount") FROM "Orders"
SELECT AVG("Quantity") FROM "Orders"
```

Use only `SelectAggregateScalarQueryBuilder<T>` for aggregate scalar builders. The aggregate selector chooses the SQL projection, and the terminal method chooses the scalar result type.

## Type Rules

`Max(...)`, `Min(...)`, `CountDistinct(...)`, `Sum(...)`, and `Average(...)` return `SelectAggregateScalarQueryBuilder<T>`.

`Sum` and `Average` themselves have no public generic method parameter.

`SelectAggregateScalarQueryBuilder<T>` exposes `Execute<TResult>()` and `ExecuteAsync<TResult>()`, forcing callers to choose an appropriate result type such as `int?`, `decimal`, `decimal?`, `double`, or `long`.

## Testing

Cover SQL rendering, API contract shape, SQLite execution, empty-set null results, explicit result type reads, and docs/vitepress/package metadata updates.
