# Select Sum and Average Design

## Goal

Extend `SelectAggregateFrom<T>()` with SQL `SUM(...)` and `AVG(...)` while keeping aggregate result types explicit enough to avoid misleading cross-provider behavior.

## Public API

Add `Sum` with two forms:

```csharp
db.SelectAggregateFrom<Order>()
  .Sum(o => o.Amount)
  .Execute();

db.SelectAggregateFrom<Order>()
  .Sum<decimal>(o => o.Amount)
  .Execute();
```

The first form infers the result type from the selected value-type member and returns `TValue?`. The second form lets callers choose the scalar read type and returns `TResult?`.

Add `Average` only with an explicit result type:

```csharp
db.SelectAggregateFrom<Order>()
  .Average<decimal>(o => o.Quantity)
  .Execute();
```

Do not add `Average(o => o.Quantity)` in this iteration. For integer columns, an inferred `int?` average is mathematically misleading and provider behavior varies.

## Internal Design

Extend `ScalarProjectionKind` with `Sum` and `Average`.

Extend `ScalarSqlRenderer` to render:

```sql
SELECT SUM("Amount") FROM "Orders"
SELECT AVG("Quantity") FROM "Orders"
```

Reuse the existing `SelectAggregateScalarQueryBuilder<T, TResult>` for filters, grouped filters, command rendering, and Dapper scalar execution.

## Type Rules

`Sum<TValue>(Expression<Func<T, TValue>> memberSelector) where TValue : struct` returns `SelectAggregateScalarQueryBuilder<T, TValue?>`.

`Sum<TResult>(Expression<Func<T, TResult>> memberSelector) where TResult : struct` also covers explicit result calls such as `Sum<decimal>(o => o.Amount)`. Expression conversion is acceptable because metadata extraction already strips simple `Convert` nodes.

`Average<TResult>(Expression<Func<T, TResult>> memberSelector) where TResult : struct` returns `SelectAggregateScalarQueryBuilder<T, TResult?>`. Callers are expected to choose an appropriate result type such as `decimal` or `double`.

## Testing

Cover SQL rendering, API contract shape, SQLite execution, empty-set null results, explicit result type reads, and docs/package metadata updates.
