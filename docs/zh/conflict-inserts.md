# 冲突插入

当插入遇到已有行时，可以使用冲突感知插入方法选择忽略或更新。

```csharp
db.InsertOrIgnore(user, table: "users_staging")
    .OnConflict(u => u.Email)
    .Execute();

db.InsertOrUpdateMany(users, table: "users_2026")
    .OnConflict(u => u.Email)
    .Update(u => u.Name, u => u.UpdatedAt)
    .Execute();
```

可用方法包括：

- `InsertOrIgnore(user)`
- `InsertOrIgnoreMany(users)`
- `InsertOrUpdate(user)`
- `InsertOrUpdateMany(users)`

这些方法也支持 `table` 重载。显式表名只改变 SQL 目标表，CLR 成员到列的映射仍来自 `T`。

## 冲突目标与更新列

`OnConflict(...)` 接收映射 CLR 成员选择器作为冲突目标。`InsertOrUpdate` 和 `InsertOrUpdateMany` 还支持 `Update(...)` 选择冲突时更新的映射列。

如果省略 `OnConflict(...)`，Db4Net 会使用键元数据作为默认冲突目标。冲突插入可以把复合 `[Key]` 元数据作为默认冲突目标。默认冲突目标要求键列不是数据库生成列；像 `[Key]` 加 `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` 这样的 identity key 仍然可以用于实体 update/delete 谓词，但不能作为冲突目标。

冲突插入终结方法返回影响行数。生成键回读只适用于常规单行 `InsertInto<T>()` / `Insert(entity)` 命令。

::: warning
数据库生成的映射成员不能作为默认或显式冲突目标，也不能通过 `InsertOrUpdate.Update(...)` 选择为冲突更新列。`OnConflict(...)` 里选择的冲突目标列也不能再通过 `InsertOrUpdate.Update(...)` 选择为更新列。
:::

::: warning 方言差异
SQLite 和 PostgreSQL 渲染原生 `ON CONFLICT`。MySQL 对 `InsertOrIgnore(...)` 渲染 `INSERT IGNORE`，对 `InsertOrUpdate(...)` 渲染 `ON DUPLICATE KEY UPDATE`；duplicate-key 处理会应用到任意主键或唯一键冲突，且 `INSERT IGNORE` 也可能按 MySQL 规则把部分数据错误降级为 warning。SQL Server 渲染 `MERGE ... WITH (HOLDLOCK)`。更多细节见[冲突插入语法](./dialects.md#冲突插入语法)。
:::
