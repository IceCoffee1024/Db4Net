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

如果省略 `OnConflict(...)`，Db4Net 会使用键元数据作为默认冲突目标。

::: warning 方言差异
SQLite 和 PostgreSQL 渲染原生 `ON CONFLICT`。MySQL 渲染 `ON DUPLICATE KEY UPDATE`，数据库本身会对任意主键或唯一键冲突触发处理。SQL Server 渲染方言特定命令，但这不是原生导入、copy 或集合式同步 API。
:::
