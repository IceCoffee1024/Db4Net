# Fluent Dapper Query Builder：v1 方向与当前状态

日期：2026-06-26

## 目标

开发一个轻量级 NuGet 包，让 .NET 开发者可以用 Fluent API 构造安全的参数化 SQL，并通过 Dapper 执行。

这个项目定位为 Dapper 的 SQL Builder，而不是 ORM，也不是 LINQ Provider。当前项目引用 `Dapper` `2.1.79`；正式发布前仍建议再次确认依赖版本。

## 设计取向

v1 聚焦 `SELECT` 查询。

Dapper 已经很好地解决了执行和对象映射问题。这个包真正有价值的地方，是补上安全、清晰的 SQL 组合能力：

- 值始终转换成 Dapper 参数。
- 类型化 API 可以从 CLR Model 解析表名和列名。
- 字符串字段 API 用于动态列集合场景，但在绑定 `T` 后必须按 CLR 属性名校验和映射。
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

db.Select("Id", "Name")
  .From<User>()
  .Where("Id", Op.Eq, 1)
  .QuerySingleOrDefault<User>();

db.SelectFrom<User>("user_report_view")
  .Where("Id", Op.Eq, 1)
  .QueryAsync<User>();
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
- `SelectFrom<T>()` 是推荐的类型化整实体入口，会展开 `T` 的可映射属性并按需 alias。
- `Select("Id", "Name").From<User>()` 兼容动态字段集合场景；字符串字段名解释为 `User` 的 CLR 属性名，不接受数据库列名。
- `SelectFrom<T>("view_or_table")` 可以覆盖报表视图、历史表、分表等场景，但字段仍然按 `T` 的属性映射。
- `Query<T>()`、`QueryFirstOrDefault<T>()`、`QuerySingleOrDefault<T>()` 与 Dapper 的命名习惯一致，并提供对应 async 终止方法。
- `GetList()` 和 `GetSingleOrDefault()` 可以作为可选别名后续再加。

v1 先固定上述三条主线，不增加 `SelectFrom<T>(...)` 指定字段重载，避免 `SelectFrom` 同时承担整实体和指定字段两种含义。

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

- `Select<T>(...)`、`Select(...)`、`SelectFrom<T>()`、`SelectFrom<T>(string)`
- `From<T>()`、`From<T>(string)`
- `Where(...)`、`OrWhere(...)`
- `OrderBy(...)`、`OrderByDescending(...)`
- `Limit(...)`、`Offset(...)`、`Page(pageNumber, pageSize)`
- `ToCommand()`
- `Query<T>()`、`QueryFirstOrDefault<T>()`、`QuerySingleOrDefault<T>()`、`Execute()`
- `QueryAsync<T>()`、`QueryFirstOrDefaultAsync<T>()`、`QuerySingleOrDefaultAsync<T>()`、`ExecuteAsync()`
- `Db4NetCommandOptions`：执行时传入 transaction、command timeout、command type；async 终止方法额外支持 `CancellationToken`
- SQL Server、SQLite、PostgreSQL、MySQL 方言
- 集中方言渲染测试覆盖 SQL Server / SQLite / PostgreSQL / MySQL 的标识符引用、分页语法和分页参数顺序
- `[Table]`、`[Column]` 映射
- 字符串标识符校验
- XML 文档生成
- NuGet README 打包
- PostgreSQL、MySQL、SQL Server 可选真实集成测试
- 测试项目支持本地 `local.runsettings`，存在该文件时 `dotnet test` 会通过 `RunSettingsFilePath` 自动使用它

验证状态：

- `dotnet test`：当前本机配置下 99 个测试全部通过；外部数据库测试是否执行取决于 `local.runsettings` 或环境变量。
- `dotnet build -c Release`：最近验证为 0 warning，0 error。
- `dotnet pack src/Db4Net/Db4Net.csproj -c Release`：可生成 `Db4Net.0.1.0-alpha.1.nupkg`。

测试策略：

- SQLite 集成测试默认使用内存数据库执行。
- PostgreSQL、MySQL、SQL Server 集成测试通过以下环境变量启用：
  - `DB4NET_POSTGRESQL_CONNECTION_STRING`
  - `DB4NET_MYSQL_CONNECTION_STRING`
  - `DB4NET_SQLSERVER_CONNECTION_STRING`
- 本地可在 `tests/Db4Net.Tests/local.runsettings` 填入连接字符串；该文件被 `.gitignore` 忽略，避免提交凭据。
- 测试项目配置了 `RunSettingsFilePath`，当 `local.runsettings` 存在时，直接执行 `dotnet test` 也会使用该文件。
- CI 或不读取项目级 runsettings 的 runner 仍可显式使用 `dotnet test --settings tests\Db4Net.Tests\local.runsettings`。

## 核心设计

SQL 渲染和 Dapper 执行保持分离。

当前内部组件：

- `SelectQueryBuilder`：面向用户的 Fluent API，并在终止方法中调用 Dapper。
- `QueryModel`：记录选择列、表、过滤条件、排序、分页。
- `SqlRenderer`：把查询模型渲染成 SQL 和参数。
- `ISqlDialect`：处理标识符引用、分页语法、分页参数顺序。
- `ModelMetadataProvider`：从 CLR 类型和成员选择器构建映射元数据。
- `ModelMetadata<T>`：按实体类型缓存表名、可映射列集合，以及按属性名索引的列字典，减少类型化 API 构建查询时的重复反射和线性查找。
- `SqlCommandDefinition`：承载 `Sql` 和 Dapper `DynamicParameters`。
- `Db4NetCommandOptions`：承载 Dapper 执行选项，终止方法内部统一转换为 Dapper `CommandDefinition`。

当前 NuGet 元数据：

- `PackageId`: `Db4Net`
- `Version`: `0.1.0-alpha.1`
- `Authors`: `IceCoffee`
- `Description`: `Lightweight fluent SQL builder and Dapper execution helpers.`
- `PackageTags`: `dapper;sql;fluent;query-builder`
- `PackageReadmeFile`: `README.md`
- `PackageLicenseExpression`: `MIT`
- `PackageProjectUrl`: `https://github.com/IceCoffee1024/Db4Net`
- `RepositoryUrl`: `https://github.com/IceCoffee1024/Db4Net.git`
- `RepositoryType`: `git`
- `GenerateDocumentationFile`: `true`

发布前仍可选择补充：

- SourceLink。
- Symbols package。
- Release notes。

分页参数顺序由方言决定：

- SQLite：`LIMIT @p1 OFFSET @p2`
- PostgreSQL：`LIMIT @p1 OFFSET @p2`
- MySQL：`LIMIT @p1 OFFSET @p2`
- SQL Server：`OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY`

这是必要的，因为不同数据库的分页语法中 limit/offset 出现顺序不同。

## 安全和映射

值必须始终变成参数：

```sql
WHERE [Id] = @p0
```

Dapper 不能参数化表名和列名。对于表名或视图名覆盖，当前使用保守规则校验，只允许字母、数字、下划线和点号。

当前映射规则：

- 类型名映射到表名。
- 属性名映射到列名。
- 支持 `[Table]`、`[Column]` 和 `[NotMapped]`。
- 类型化投影会渲染列别名，例如 `[display_name] AS [DisplayName]`，让 Dapper 可以按属性名回填实体。
- 类型化成员选择器只允许选择可映射属性；`[NotMapped]` 成员用于 `Select`、`Where`、`OrderBy` 时会被拒绝。
- 绑定 `T` 后，字符串字段名也按 CLR 属性名解析。例如 `Select("DisplayName").From<MappedUser>()` 会渲染 `[display_name] AS [DisplayName]`；`Select("display_name").From<MappedUser>()` 会被拒绝。
- 数据库列名只由 `[Column]` 决定，不作为字段字符串输入的公共 API。
- 不做自动复数化。

表名或视图名覆盖示例里使用 `"user_report_view"`，强调它只覆盖查询来源，不改变字段映射规则。

## JOIN 策略

Db4Net 长期定位为安全、轻量、可预测的单表/视图 SQL builder，不内置 JOIN builder。

对于稳定、可复用的跨表读模型，推荐在数据库侧定义视图，并通过 `SelectFrom<T>()` 像普通表一样查询。对于高度动态、数据库特定或一次性的复杂 JOIN，推荐直接使用 Dapper 原生 SQL。

这个取舍可以让 Db4Net 避免演变成完整 ORM 或通用 SQL query builder，同时保留 Dapper 的灵活性。

## v1 后续范围

暂缓：

- Join、CTE、子查询、聚合查询
- Insert/update/delete builder
- 关系加载和 Dapper multi-mapping 辅助能力
- 显式 raw SQL 片段 API，例如 `RawSql.Fragment(...)`

明确不做：

- `Where(u => u.Id == 1)` 这类谓词表达式翻译
- 完整 LINQ Provider

## 关键风险

表达式支持很容易失控。Db4Net 应继续使用 `Where(u => u.Id, Op.Eq, 1)` 这种成员选择器 + 显式操作符 + 参数值的形式，不实现 `Where(u => u.Id == 1)` 这类谓词表达式翻译，避免演变成 LINQ Provider。

标识符注入是主要安全风险。参数只能保护值，不能保护表名和列名。

即使是简单 SQL，不同数据库的语法也有差异。方言抽象必须继续保持清晰，避免把差异硬编码在 builder 中。

API 命名要贴近 Dapper 用户习惯。先使用 Dapper 风格的终止方法，后续再补便利别名。

## 参考

- Dapper NuGet package: https://www.nuget.org/packages/Dapper/
- Dapper GitHub repository: https://github.com/DapperLib/Dapper
- Dapper.SqlBuilder NuGet package: https://www.nuget.org/packages/Dapper.SqlBuilder
- Dapper.SimpleSqlBuilder documentation: https://mishael-o.github.io/Dapper.SimpleSqlBuilder/
