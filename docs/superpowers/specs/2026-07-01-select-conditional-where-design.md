# Select Conditional Where Design

## Context

Complex search and paging repository methods often need several optional filters. The existing API already supports these queries with ordinary C# `if` statements:

```csharp
var query = db.SelectFrom<ChatMessage>();

if (!string.IsNullOrEmpty(keyword))
{
    query.WhereGroup(group => group
        .Where(m => m.PlayerId, Op.Like, keyword)
        .OrWhere(m => m.Message, Op.Like, keyword));
}
```

This is correct and efficient, but it becomes noisy when a query has many optional filters. The feature is an API ergonomics improvement, not a SQL performance optimization.

## Decision

Add conditional composition to read-only SELECT builders:

```csharp
query
    .When(hasKeyword, q => q.WhereGroup(group => group
        .Where(m => m.PlayerId, Op.Like, keyword)
        .OrWhere(m => m.Message, Op.Like, keyword)))
    .WhereIf(hasPlayerId, m => m.PlayerId, Op.Eq, playerId)
    .OrWhereIf(hasFallback, m => m.SenderName, Op.Eq, fallbackName)
    .WhereGroupIf(hasRange, group => group
        .WhereIf(start.HasValue, m => m.CreatedAt, Op.Gte, start)
        .WhereIf(end.HasValue, m => m.CreatedAt, Op.Lte, end));
```

Supported APIs:

- `SelectQueryBuilder.When(bool, Action<SelectQueryBuilder>)`
- `SelectQueryBuilder<T>.When(bool, Action<SelectQueryBuilder<T>>)`
- `SelectCountQueryBuilder<T>.When(bool, Action<SelectCountQueryBuilder<T>>)`
- `SelectExistsQueryBuilder<T>.When(bool, Action<SelectExistsQueryBuilder<T>>)`
- `SelectAggregateScalarQueryBuilder<T>.When(bool, Action<SelectAggregateScalarQueryBuilder<T>>)`
- `WhereIf(...)`
- `OrWhereIf(...)`
- `WhereGroupIf(...)`

`WhereIf` and `OrWhereIf` support the existing string/property-name and typed member-selector shapes. `WhereGroupIf` supports nested `FilterGroupBuilder` and `FilterGroupBuilder<T>`. The read-only scope includes row queries, count queries, exists queries, and aggregate scalar queries.

## Non-Goals

Do not add conditional APIs to `UpdateCommandBuilder<T>` or `DeleteCommandBuilder<T>` in this iteration. Conditional CUD filters can accidentally skip predicates and widen an update/delete operation. If that is added later, it needs a separate design with explicit safety rules.

Do not add conditional `WhereIn` / `WhereNotIn` variants yet. `When(...)` already handles those cases without multiplying overloads.

## Behavior

- False conditions leave the builder unchanged.
- True conditions delegate to the existing `Where`, `OrWhere`, or `WhereGroup` methods.
- `When(...)` always validates the delegate and only invokes it when the condition is true.
- Empty conditional groups do not render SQL.
- This feature does not change SQL rendering, parameter naming, paging, or execution.

## Documentation

Docs should describe conditional filters as a readability helper for optional search criteria, pagination queries, and matching scalar count/exists/aggregate queries. Examples should show both `WhereIf` for simple filters and `When` for grouped keyword search.
