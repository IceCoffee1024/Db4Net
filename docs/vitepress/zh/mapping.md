# 映射

Db4Net 支持标准映射特性：

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

类型化投影会为映射列添加别名，便于 Dapper 映射回属性名：

```sql
SELECT [Id], [display_name] AS [Name] FROM [app_users]
```

`[NotMapped]` 成员会从 `SelectFrom<T>()` 中排除，并在类型化 `Select`、`Where`、`OrderBy`、`Value` 和 `Set` 成员选择器中被拒绝。

## 键与数据库生成列

`[Key]` 以及 `Id` / `<TypeName>Id` 约定用于定位记录，例如实体命令便捷方法、`WhereKey(user)`、冲突插入默认目标，以及显式单行插入键回读。

冲突插入默认使用所有键列，包括复合 `[Key]` 元数据。实体 update/delete 便捷方法以及 many update/delete 便捷方法仍要求只有一个键列，并且键值不能是默认值。

`[Key]` 和 `[DatabaseGenerated(...)]` 是独立概念。一个 `[Key]` 列也可以标记为 `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`：Db4Net 仍会在实体 update/delete 的 `WHERE` 谓词中使用它，也可以通过显式单行插入键终结方法返回它，但会在自动 insert 值中跳过它，因为该值由数据库生成。

带有 `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` 或 `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` 的映射属性，会被 `Values(entity)`、`Insert(entity)`、`InsertMany(users)` 和冲突插入值跳过。数据库生成的非键属性也会从 `Update(entity)` 和 `UpdateMany(users)` 的实体驱动更新赋值中跳过。

数据库生成成员不能作为默认或显式冲突目标，也不能通过 `InsertOrUpdate.Update(...)` 选择为冲突更新列。冲突目标列也不能再通过 `InsertOrUpdate.Update(...)` 选择为更新列。显式 `.Value(...)` 和 `.Set(...)` 仍由调用方完全控制。

生成键回读是显式行为，并且只限常规单行插入终结方法，例如 `Insert(entity).ExecuteReturnKey<TResult>()` 和 `InsertInto<T>().Values(entity).ReturnKey(...).Execute<TResult>()`。Db4Net 不会在插入后修改实体实例，也不会刷新所有 computed/generated 值。
