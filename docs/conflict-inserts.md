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

When `OnConflict(...)` is omitted, Db4Net uses key metadata as the default conflict target. Conflict-aware inserts can use composite `[Key]` metadata as the default target. This requires non-database-generated key columns; an identity key such as `[Key]` plus `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` is still valid for entity update/delete predicates, but it is not a valid conflict target.

Conflict-aware insert terminals return affected row counts. Generated-key readback is intentionally limited to regular single-row `InsertInto<T>()` / `Insert(entity)` commands.

::: warning
Database-generated mapped members cannot be used as default or explicit conflict targets, and cannot be selected through `InsertOrUpdate.Update(...)`. Columns selected in `OnConflict(...)` also cannot be selected through `InsertOrUpdate.Update(...)`.
:::

::: warning Dialect differences
SQLite and PostgreSQL render native `ON CONFLICT`. MySQL renders `INSERT IGNORE` for `InsertOrIgnore(...)`. MySQL `InsertOrUpdate(...)` follows the MySQL 8.0.19+ row-alias form, for example `INSERT ... VALUES (...) AS _new ON DUPLICATE KEY UPDATE col = _new.col`; this generated SQL is not compatible with MySQL 5.7, MySQL 8.0.0-8.0.18, or MariaDB. Duplicate-key handling applies to any primary or unique key violation, and `INSERT IGNORE` can also turn some data errors into warnings according to MySQL rules. SQL Server renders `MERGE ... WITH (HOLDLOCK)`. See [Conflict Inserts](./dialects.md#conflict-inserts) in the dialect guide.
:::
