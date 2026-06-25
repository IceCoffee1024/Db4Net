# Fluent Dapper Query Builder：v1 方向与当前状态

日期：2026-06-25

## 目标

开发一个轻量级 NuGet 包，让 .NET 开发者可以用 Fluent API 构造安全的参数化 SQL，并通过 Dapper 执行。

这个项目定位为 Dapper 的 SQL Builder，而不是 ORM，也不是 LINQ Provider。截至 2026-06-24，NuGet 显示 `Dapper` 版本为 `2.1.79`；发布前仍需要再次确认。

## 设计取向

v1 聚焦 `SELECT` 查询。

Dapper 已经很好地解决了执行和对象映射问题。这个包真正有价值的地方，是补上安全、清晰的 SQL 组合能力：

- 值始终转换成 Dapper 参数。
- 类型化 API 可以从 CLR Model 解析表名和列名。
- 字符串 API 用于动态查询场景，但必须严格校验标识符。
- 执行前可以通过 `ToCommand()` 查看生成的 SQL 和参数。

v1 不实现完整表达式翻译。支持 `u => u.Id` 这种成员选择器，但不翻译 `u => u.Id == 1 && u.Name.StartsWith("A")` 这类完整谓词。

生成 SQL 的关键词统一大写，例如：

```sql
SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0
```

## 推荐 API

主 API 使用接近 Dapper 的终止方法名称：

```csharp
db.Select<User>(u => u.Id, u => u.Name)
  .Where(u => u.Id, Op.Eq, 1)
  .QuerySingleOrDefault<User>();

db.SelectFrom<User>()
  .Where(u => u.Id, Op.Eq, 1)
  .QueryAsync<User>();

db.SelectFrom("Users")
  .Where("Id", Op.Eq, 1)
  .QuerySingleOrDefault<dynamic>();

db.Select("Id", "Name")
  .From<User>()
  .Where(u => u.Id, Op.Eq, 1)
  .QuerySingleOrDefault<User>();
```

同时支持现代 C# 的集合表达式：

```csharp
db.Select(["Id", "Name"])
  .From<User>()
  .Where(u => u.Id, Op.Eq, 1)
  .QuerySingleOrDefault<User>();
```

这样设计的原因：

- `Select<T>(...)` 是推荐的类型化指定字段入口，会自动使用 `T` 的表映射，并把数据库列 alias 成属性名以适配 Dapper 默认映射。
- `SelectFrom<T>()` 是推荐的类型化整表入口，对应 `SELECT * FROM ...`。
- `Select("Id", "Name").From<User>()` 兼容字符串字段场景。
- `SelectFrom("Users")` 可以覆盖报表、历史库、动态表名等场景。
- `Query<T>()`、`QueryFirstOrDefault<T>()`、`QuerySingleOrDefault<T>()` 与 Dapper 的命名习惯一致，并提供对应 async 终止方法。
- `GetList()` 和 `GetSingleOrDefault()` 可以作为可选别名后续再加。

操作符示例：

```csharp
.Where(u => u.Id, Op.Eq, 1)
.Where(u => u.Name, Op.Like, "A%")
.Where(u => u.Id, Op.In, [1, 2, 3])
.Where(u => u.DeletedAt, Op.IsNull)
.Where(u => u.Name, Op.IsNotNull)
```

`Eq null` 和 `NotEq null` 仍然兼容，分别渲染为 `IS NULL` 和 `IS NOT NULL`。但推荐业务代码优先使用 `Op.IsNull`、`Op.IsNotNull` 的无值重载，可读性更明确，也避免传入第三个 `null` 参数。

## 当前实现状态

第一阶段已经完成，当前包版本为 `0.1.0-alpha.1`。

已实现：

- `Select<T>(...)`、`Select(...)`、`SelectFrom<T>()`、`SelectFrom(string)`
- `From<T>()`、`From(string)`
- `Where(...)`、`OrWhere(...)`
- `OrderBy(...)`、`OrderByDescending(...)`
- `Limit(...)`、`Offset(...)`、`Page(pageNumber, pageSize)`
- `ToCommand()`
- `Query<T>()`、`QueryFirstOrDefault<T>()`、`QuerySingleOrDefault<T>()`、`Execute()`
- `QueryAsync<T>()`、`QueryFirstOrDefaultAsync<T>()`、`QuerySingleOrDefaultAsync<T>()`、`ExecuteAsync()`
- SQL Server 和 SQLite 方言
- `[Table]`、`[Column]` 映射
- 字符串标识符校验
- XML 文档生成
- NuGet README 打包

验证状态：

- `dotnet test`：51 个测试全部通过。
- 覆盖率：行覆盖率 `94.52%`，分支覆盖率 `87.94%`。
- `dotnet build -c Release`：0 warning，0 error。
- `dotnet pack src/Db4Net/Db4Net.csproj -c Release --no-build`：可生成 `Db4Net.0.1.0-alpha.1.nupkg`。

## 核心设计

SQL 渲染和 Dapper 执行保持分离。

当前内部组件：

- `SelectQueryBuilder`：面向用户的 Fluent API，并在终止方法中调用 Dapper。
- `QueryModel`：记录选择列、表、过滤条件、排序、分页。
- `SqlRenderer`：把查询模型渲染成 SQL 和参数。
- `ISqlDialect`：处理标识符引用、分页语法、分页参数顺序。
- `ModelMetadataProvider`：映射类型和成员选择器。
- `SqlCommandDefinition`：承载 `Sql` 和 Dapper `DynamicParameters`。

分页参数顺序由方言决定：

- SQLite：`LIMIT @p1 OFFSET @p2`
- SQL Server：`OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY`

这是必要的，因为不同数据库的分页语法中 limit/offset 出现顺序不同。

## 安全和映射

值必须始终变成参数：

```sql
WHERE [Id] = @p0
```

Dapper 不能参数化表名和列名。对于字符串形式的表名、列名，当前使用保守规则校验，只允许字母、数字、下划线和点号。

当前映射规则：

- 类型名映射到表名。
- 属性名映射到列名。
- 支持 `[Table]` 和 `[Column]`。
- 类型化指定字段会渲染列别名，例如 `[display_name] AS [DisplayName]`，让 Dapper 可以按属性名回填实体。
- 不做自动复数化。

字符串示例里使用 `"Users"`，因为它更像真实表名，也避免和 `User` CLR 类型混淆。

## v1 后续范围

暂缓：

- Join、CTE、子查询、聚合查询
- Insert/update/delete builder
- 完整谓词表达式翻译
- 关系加载和 Dapper multi-mapping 辅助能力
- 显式 raw SQL 片段 API，例如 `RawSql.Fragment(...)`
- 更多数据库方言，例如 PostgreSQL、MySQL

## 关键风险

表达式支持很容易失控。v1 应继续限制在成员选择器。

标识符注入是主要安全风险。参数只能保护值，不能保护表名和列名。

即使是简单 SQL，不同数据库的语法也有差异。方言抽象必须继续保持清晰，避免把差异硬编码在 builder 中。

API 命名要贴近 Dapper 用户习惯。先使用 Dapper 风格的终止方法，后续再补便利别名。

## 参考

- Dapper NuGet package: https://www.nuget.org/packages/Dapper/
- Dapper GitHub repository: https://github.com/DapperLib/Dapper
- Dapper.SqlBuilder NuGet package: https://www.nuget.org/packages/Dapper.SqlBuilder
- Dapper.SimpleSqlBuilder documentation: https://mishael-o.github.io/Dapper.SimpleSqlBuilder/
