# Filters

Use `Where(...)` for `AND` filters and `OrWhere(...)` for `OR` filters. Typed builders accept member selectors or CLR property-name strings.

```csharp
var users = db.SelectFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .Where(u => u.Name, Op.Like, "A%")
    .OrWhere(u => u.Email, Op.IsNotNull)
    .Query();
```

Common operators include equality, comparisons, `Like`, `In`, and null checks:

```csharp
.Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
.Where(u => u.DeletedAt, Op.IsNull)
.Where(u => u.Name, Op.IsNotNull)
```

`Op.Eq` with `null` renders `IS NULL`, and `Op.NotEq` with `null` renders `IS NOT NULL`. Prefer `Op.IsNull` and `Op.IsNotNull` when no value is needed.

## `IN` Subqueries

Use `WhereIn(...)`, `OrWhereIn(...)`, `WhereNotIn(...)`, and `OrWhereNotIn(...)` when the `IN` list comes from another single-column Db4Net `SELECT` query.

```csharp
var users = db.SelectFrom<User>()
    .WhereIn(
        u => u.Id,
        db.SelectFrom<Order>(o => o.UserId)
            .Where(o => o.Amount, Op.Gt, 100m))
    .Query();
```

The subquery must select exactly one column. Db4Net renders the subquery as SQL and keeps one shared parameter sequence across the outer query and nested query.

## Grouped Filters

Use `WhereGroup(...)` and `OrWhereGroup(...)` when mixed `AND` and `OR` filters need explicit parentheses.

```csharp
var users = db.SelectFrom<User>()
    .WhereGroup(group => group
        .Where(u => u.Id, Op.Eq, 1)
        .OrWhere(u => u.Name, Op.Eq, "Alice"))
    .Where(u => u.Id, Op.Gt, 0)
    .Query();
```

This renders grouped SQL like:

```sql
WHERE ([Id] = @p0 OR [Name] = @p1) AND [Id] > @p2
```

::: warning
Plain `Where(...)` and `OrWhere(...)` chains follow SQL operator precedence. They do not add parentheses automatically.
:::
