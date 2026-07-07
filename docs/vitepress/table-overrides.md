# Table Overrides

Use table overloads when a CLR model mapping is reused for tenant, time-partitioned, staging, archive, or view-backed data.

```csharp
var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update<User>("users_tenant_001")
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();
```

The explicit table changes only the SQL target table. Property-to-column mapping still comes from `T`, and the table identifier is validated and quoted by the configured dialect.

## Select Overrides

```csharp
var users = db.SelectFrom<User>("users_archive")
    .Where(u => u.Id, Op.Gt, 0)
    .Query();

var rows = db.Select("Id", "Name")
    .From<User>("active_users_view")
    .Query();
```

## Command Overrides

```csharp
db.InsertInto<User>("users_staging");
db.Update<User>("users_2026");
db.DeleteFrom<User>("users_2026");
```

Entity and many conveniences also have `table` overloads:

```csharp
db.Insert(user, table: "users_staging").Execute();
db.UpdateMany(users, table: "users_2026").Execute();
db.InsertOrIgnoreMany(users, table: "users_staging").Execute();
```
