# 映射

Db4Net 支持标准映射特性：

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

类型化投影会为映射列添加别名，便于 Dapper 映射回属性名：

```sql
SELECT [Id], [display_name] AS [Name] FROM [app_users]
```

`[NotMapped]` 成员会从 `SelectFrom<T>()` 中排除，并在类型化 `Select`、`Where`、`OrderBy`、`Value` 和 `Set` 成员选择器中被拒绝。

## 键与数据库生成列

`[Key]` 以及 `Id` / `<TypeName>Id` 约定会被实体命令便捷方法、`WhereKey(user)` 和冲突插入默认目标使用。

冲突插入默认使用所有键列，包括复合 `[Key]` 元数据。实体 update/delete 便捷方法以及 many update/delete 便捷方法仍要求只有一个键列。

带有 `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` 或 `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` 的映射属性，会被 `Values(entity)`、`Insert(entity)`、`InsertMany(users)` 和冲突插入值跳过。数据库生成的非键属性也会从 `Update(entity)` 和 `UpdateMany(users)` 的实体驱动更新赋值中跳过。

数据库生成成员不能作为默认或显式冲突目标，也不能通过 `InsertOrUpdate.Update(...)` 选择为冲突更新列。显式 `.Value(...)` 和 `.Set(...)` 仍由调用方完全控制。
