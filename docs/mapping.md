# Mapping

Db4Net uses standard .NET mapping attributes.

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("app_users")]
public sealed class User
{
    [Key]
    public int Id { get; set; }

    [Column("display_name")]
    public string Name { get; set; } = "";

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

Key metadata identifies mapped columns for equality predicates or conflict targets. It does not imply entity tracking, generated value readback, relationship identity maps, or automatic concurrency behavior.

Conflict-aware inserts use all key columns by default, including composite `[Key]` metadata. Entity update/delete conveniences and many update/delete conveniences require a single key column.

## Generated and Ignored Members

`[NotMapped]` members are excluded from `SelectFrom<T>()` and rejected in typed selectors such as `Select`, `Where`, `OrderBy`, `Value`, and `Set`.

`[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` and `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` mapped properties are omitted by `Values(entity)`, `Insert(entity)`, `InsertMany(entities)`, and conflict-aware insert values. Database-generated non-key properties are also omitted from entity-driven update assignments in `Update(entity)` and `UpdateMany(entities)`. Explicit `.Value(...)` and `.Set(...)` calls remain caller-controlled.
