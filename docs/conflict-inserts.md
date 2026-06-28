# Conflict Inserts

Use conflict-aware insert conveniences when inserts should ignore or update rows that already match a conflict target.

```csharp
db.InsertOrIgnore(user, table: "users_staging")
    .OnConflict(u => u.Email)
    .Execute();

db.InsertOrUpdate(user)
    .OnConflict(u => u.Email)
    .Update(u => u.Name, u => u.UpdatedAt)
    .Execute();
```

Many variants use the same API shape:

```csharp
db.InsertOrUpdateMany(users, table: "users_2026")
    .OnConflict(u => u.Email)
    .Update(u => u.Name, u => u.UpdatedAt)
    .Execute();
```

Available methods:

- `InsertOrIgnore(entity)`
- `InsertOrIgnore(entity, table)`
- `InsertOrIgnoreMany(entities)`
- `InsertOrIgnoreMany(entities, table)`
- `InsertOrUpdate(entity)`
- `InsertOrUpdate(entity, table)`
- `InsertOrUpdateMany(entities)`
- `InsertOrUpdateMany(entities, table)`

`OnConflict(...)` uses mapped CLR member selectors for the conflict target. `InsertOrUpdate` and `InsertOrUpdateMany` use `Update(...)` to choose mapped columns updated on conflict.

When `OnConflict(...)` is omitted, Db4Net uses key metadata as the default conflict target.

::: warning
Database-generated mapped members cannot be used as default or explicit conflict targets, and cannot be selected through `InsertOrUpdate.Update(...)`.
:::
