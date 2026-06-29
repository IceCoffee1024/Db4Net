# Select IN Subquery Design

## Context

Db4Net is intentionally Dapper-friendly and SQL-shaped, but it is not a full SQL DSL or LINQ provider. The project already supports typed `SELECT`, scalar queries, CRUD builders, conflict-aware inserts, filter grouping, and Dapper execution options.

Subquery support was considered in two forms:

- filter subqueries such as `WHERE Id IN (SELECT UserId FROM Orders ...)`
- source subqueries such as `FROM (SELECT ...) AS alias`

The first form fits Db4Net's current single-source query model. The second form requires a broader query-source and alias system, and should be deferred until joins, qualified columns, grouping, and derived table design are addressed together.

## Decision

Implement only single-column, non-correlated `IN` subquery filters for `SelectQueryBuilder`:

```csharp
db.SelectFrom<User>()
    .WhereIn(
        u => u.Id,
        db.SelectFrom<Order>(o => o.UserId)
            .Where(o => o.Amount, Op.Gt, 100m))
    .Query();
```

Supported public methods:

- `WhereIn(...)`
- `OrWhereIn(...)`
- `WhereNotIn(...)`
- `OrWhereNotIn(...)`

The methods are available on:

- `SelectQueryBuilder`
- `SelectQueryBuilder<T>`
- `FilterGroupBuilder`
- `FilterGroupBuilder<T>`

This keeps grouped filters and top-level filters consistent.

## Rendering Rules

The rendered SQL shape is:

```sql
WHERE "Id" IN (
  SELECT "UserId"
  FROM "Orders"
  WHERE "Amount" > @p0
)
```

When the outer query also has parameters, outer and nested filters share one parameter sequence:

```sql
WHERE "Name" LIKE @p0
  AND "Id" IN (
    SELECT "UserId"
    FROM "Orders"
    WHERE "Amount" > @p1
  )
```

The subquery must select exactly one column. `SelectFrom<T>()`, which expands to all mapped columns, is rejected for subquery filters unless the caller narrows it with `SelectFrom<T>(...)` or `.Select(...)`.

## Internal Design

The implementation uses an internal filter node rather than encoding subqueries as `object?` values for `Op.In`.

Key internal pieces:

- `FilterSubqueryClause` represents `IN` / `NOT IN` subquery filters.
- `SelectQueryModel.Clone()` snapshots a subquery when it is attached to an outer query, so later mutations of the subquery builder do not change the outer query.
- `SqlRenderContext` carries the dialect and a shared `SqlParameterWriter`.
- `SelectSqlRenderer.RenderSql(...)` can render nested `SELECT` SQL into an existing render context.
- `FilterSqlRenderer` renders `FilterSubqueryClause` by recursively rendering the nested `SelectQueryModel` with the same context.

This avoids merging separate `DynamicParameters` instances and prevents parameter name collisions.

## Explicit Non-Goals

This design does not add:

- `WhereExists(...)` or `WhereNotExists(...)`
- correlated subqueries that reference outer columns
- `From(subQuery, alias)`
- derived table sources
- joins
- qualified column or table alias APIs
- column-to-column predicates such as `WhereColumn(...)`
- raw SQL fragments

Those features require a broader alias and query-source model and should be designed together instead of being added one method at a time.

## Follow-Up Direction

If Db4Net later adds report-oriented query features, the next design step should be a query-source model that can represent:

- table/view sources
- derived table sources
- aliases
- qualified columns
- joins
- correlated predicates

Until that exists, complex report SQL should remain a Dapper raw SQL use case or be exposed through database views.
