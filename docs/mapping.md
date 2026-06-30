# Mapping

Db4Net uses standard .NET mapping attributes.

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("app_users")]
public sealed class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("display_name")]
    public string Name { get; set; } = "";

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    public string DisplayOnly { get; set; } = "";
}
```

Typed projections alias mapped columns so Dapper can map results back to property names:

```sql
SELECT [Id], [display_name] AS [Name] FROM [app_users]
```

## Keys

`[Key]` and the `Id` / `<TypeName>Id` convention are used by:

- `Update(entity)` and `UpdateMany(entities)`
- `Delete(entity)` and `DeleteMany(entities)`
- `WhereKey(entity)`
- default conflict targets for conflict-aware inserts

Key metadata identifies mapped columns for equality predicates, conflict targets, and explicit single-row insert key terminals. It does not imply entity tracking, automatic entity mutation, relationship identity maps, or automatic concurrency behavior.

Conflict-aware inserts use all key columns by default, including composite `[Key]` metadata. Entity update/delete conveniences and many update/delete conveniences require a single key column and a non-default key value.

## Generated and Ignored Members

`[NotMapped]` members are excluded from `SelectFrom<T>()` and rejected in typed selectors such as `Select`, `Where`, `OrderBy`, `Value`, and `Set`.

`[Key]` and `[DatabaseGenerated(...)]` are independent. A `[Key]` column can also be `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`: Db4Net still uses it in `WHERE` predicates for entity updates and deletes, and it can be returned by explicit single-row insert key terminals, but Db4Net omits it from automatic insert values because the database generates it.

`[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` and `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` mapped properties are omitted by `Values(entity)`, `Insert(entity)`, `InsertMany(entities)`, and conflict-aware insert values. Database-generated non-key properties are also omitted from entity-driven update assignments in `Update(entity)` and `UpdateMany(entities)`. Explicit `.Value(...)` and `.Set(...)` calls remain caller-controlled.

Database-generated members cannot be used as default or explicit conflict targets, and cannot be selected through `InsertOrUpdate.Update(...)`. Conflict target columns also cannot be selected through `InsertOrUpdate.Update(...)`.

Generated key readback is explicit and limited to regular single-row insert terminals such as `Insert(entity).ExecuteReturnKey<TResult>()` and `InsertInto<T>().Values(entity).ReturnKey(...).Execute<TResult>()`. Db4Net does not mutate entity instances after insert or refresh all computed values.
