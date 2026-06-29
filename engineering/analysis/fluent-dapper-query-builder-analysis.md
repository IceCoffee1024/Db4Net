# Fluent Dapper Query Builder：v1 方向与当前状态

最后更新：2026-06-27

## 目标

开发一个轻量级 NuGet 包，让 .NET 开发者可以用 Fluent API 构造安全的参数化 SQL，并通过 Dapper 执行。

这个项目定位为 Dapper 的 SQL Builder，而不是 ORM，也不是 LINQ Provider。当前项目引用 `Dapper` `2.1.79`；正式发布前仍建议再次确认依赖版本。

## 设计取向

v1 聚焦安全、轻量、可预测的单表查询和命令构造。

API 设计刻意贴近 SQL 语句顺序：`SelectFrom<T>()`、`InsertInto<T>()`、`Update<T>()`、`DeleteFrom<T>()` 分别对应 `SELECT ... FROM`、`INSERT INTO`、`UPDATE`、`DELETE FROM`。这样可以保持 Dapper 用户熟悉的 SQL 心智模型，同时通过类型映射、标识符校验和参数化值来保证安全边界。

Dapper 已经很好地解决了执行和对象映射问题。这个包真正有价值的地方，是补上安全、清晰的 SQL 组合能力：

- 值始终转换成 Dapper 参数。
- 类型化 API 可以从 CLR Model 解析表名和列名。
- 字符串字段 API 用于动态列集合场景，例如动态表格、动态表单、导出模板、字段级权限；字符串必须是 CLR 属性名，不是数据库列名或 SQL 片段。
- 执行前可以通过 `ToCommand()` 查看生成的 SQL 和参数。
- `UPDATE`、`DELETE` 默认要求 `WHERE`，只有显式调用 `AllowAllRows()` 才允许影响整表。

v1 不实现完整表达式翻译。支持 `u => u.Id` 这种成员选择器，但不翻译 `u => u.Id == 1 && u.Name.StartsWith("A")` 这类完整谓词。

生成 SQL 的关键词统一大写，例如：

```sql
SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0
```

## 推荐 API

主 API 使用接近 Dapper 的终止方法名称：

```csharp
db.SelectFrom<User>(u => u.Id, u => u.Name)
  .Where(u => u.Id, Op.Eq, 1)
  .QuerySingleOrDefault();

db.SelectFrom<User>()
  .Where(u => u.Id, Op.Eq, 1)
  .QueryAsync();

db.Select("Id", "Name")
  .From<User>()
  .Where("Id", Op.Eq, 1)
  .QuerySingleOrDefault();

db.SelectFrom<User>("user_report_view")
  .Where("Id", Op.Eq, 1)
  .QueryAsync();

db.InsertInto<User>()
  .Value(u => u.Id, 3)
  .Value(u => u.Name, "Charlie")
  .ExecuteAsync();

db.InsertInto<User>("users_staging")
  .Value(u => u.Id, 3)
  .Value(u => u.Name, "Charlie")
  .ExecuteAsync();

db.Update<User>()
  .Set(u => u.Name, "Alice")
  .Where(u => u.Id, Op.Eq, 1)
  .Execute();

db.Update<User>("users_tenant_001")
  .Set(u => u.Name, "Alice")
  .Where(u => u.Id, Op.Eq, 1)
  .Execute();

db.DeleteFrom<User>()
  .Where(u => u.Id, Op.Eq, 1)
  .ExecuteAsync();

db.DeleteFrom<User>("users_2026_06")
  .Where(u => u.Id, Op.Eq, 1)
  .ExecuteAsync();

db.InsertOrIgnore(user, table: "users_staging")
  .OnConflict(u => u.Email)
  .Execute();

db.InsertOrUpdateMany(users, table: "users_2026")
  .OnConflict(u => u.Email)
  .Update(u => u.Name, u => u.UpdatedAt)
  .Execute();
```

同时支持现代 C# 的集合表达式：

```csharp
db.Select(["Id", "Name"])
  .From<User>()
  .Where(u => u.Id, Op.Eq, 1)
  .QuerySingleOrDefault();
```

这样设计的原因：

- `SelectFrom<T>(...)` 是推荐的类型化指定字段入口，会自动使用 `T` 的表映射，并把数据库列 alias 成属性名以适配 Dapper 默认映射。
- `SelectFrom<T>()` 是推荐的类型化整实体入口，会展开 `T` 的可映射属性并按需 alias。
- `Select("Id", "Name").From<User>()` 兼容动态字段集合场景；字符串字段名解释为 `User` 的 CLR 属性名，不接受数据库列名或 SQL 片段。
- `SelectFrom<T>("view_or_table")` 可以覆盖报表视图、历史表、分表等场景，但字段仍然按 `T` 的属性映射。
- 绑定 `T` 后，`Query()`、`QueryFirstOrDefault()`、`QuerySingleOrDefault()` 会直接物化当前模型；非泛型 builder 仍保留 `Query<T>()` 等显式结果类型重载，与 Dapper 命名习惯一致。
- `Execute()`、`ExecuteAsync()` 只用于 `InsertInto<T>()`、`Update<T>()`、`DeleteFrom<T>()` 这类命令 builder，不挂在 `SELECT` builder 上。
- `InsertInto<T>("table")`、`Update<T>("table")`、`DeleteFrom<T>("table")` 支持多租户分表、按年月分表、staging 表、archive 表等场景；显式表名只覆盖 SQL 目标表，字段映射仍然来自 `T`。
- entity command convenience 只作为单表命令 builder 的便利层：这里的 entity 是作为值来源的已映射 CLR 对象，不是被跟踪的 ORM 实体；`Insert(entity)`、`Update(entity)`、`Delete(entity)` 是常用单实体入口，`InsertMany(entities)`、`UpdateMany(entities)`、`DeleteMany(entities)` 是多实体便利入口，`Values(entity)` 和 `WhereKey(entity)` 是底层 builder convenience，最终仍通过可检查的 SQL 命令和 Dapper 执行。
- conflict-aware insert 的公共入口固定为 `InsertOrIgnore(entity)`、`InsertOrIgnoreMany(entities)`、`InsertOrUpdate(entity)`、`InsertOrUpdateMany(entities)`，并分别支持 `table` 覆盖。不要增加 `Save`、`Merge`、`Upsert`、`Bulk` 作为公共 API 命名或别名。
- `OnConflict(...)` 使用 CLR member selectors 指定冲突目标；`InsertOrUpdate` / `InsertOrUpdateMany` 的 `Update(...)` 也使用 CLR member selectors 指定冲突时更新的列。它们不是数据库列名字符串入口，也不是 SQL 片段入口。
- `Many` 版本只是逐实体 Dapper multi-execute convenience：对实体序列执行同一类参数化命令，空集合返回 0，不自动开启事务，不代表 provider-native copy/import，也不代表 set-based synchronization。
- `GetList()` 和 `GetSingleOrDefault()` 可以作为可选别名后续再加。

当前设计让 `SelectFrom<T>()` 表示整实体查询，`SelectFrom<T>(...)` 表示从同一模型表中选择指定映射属性，避免 facade 上再出现容易被误解为结果类型入口的泛型 `Select`。

操作符示例：

```csharp
.Where(u => u.Id, Op.Eq, 1)
.Where(u => u.Name, Op.Like, "A%")
.Where(u => u.Id, Op.In, [1, 2, 3])
.Where(u => u.DeletedAt, Op.IsNull)
.Where(u => u.Name, Op.IsNotNull)
```

`Eq null` 和 `NotEq null` 仍然兼容，分别渲染为 `IS NULL` 和 `IS NOT NULL`。但推荐业务代码优先使用 `Op.IsNull`、`Op.IsNotNull` 的无值重载，可读性更明确，也避免传入第三个 `null` 参数。

混合 `AND` / `OR` 时，普通 `Where(...)` / `OrWhere(...)` 链按照 SQL 原生优先级渲染，不自动添加括号。需要明确分组时使用 `WhereGroup(...)` / `OrWhereGroup(...)`：

```csharp
db.SelectFrom<User>()
  .WhereGroup(group => group
    .Where(u => u.Id, Op.Eq, 1)
    .OrWhere(u => u.Name, Op.Eq, "Alice"))
  .Where(u => u.Id, Op.Gt, 0)
  .Query();
```

该设计保留 SQL-shaped 心智模型，同时让括号分组显式、可读、可测试。

## 当前实现状态

当前 alpha 阶段实现已完成，计划中的首个公开包版本为 `0.1.0-alpha.1`。当前 alpha 已经不只是最初的 SELECT builder，而是安全、SQL-shaped、单表 query/command builder 的早期完整形态。

已实现：

- `Select(...)`、`SelectFrom<T>()`、`SelectFrom<T>(...)`、`SelectFrom<T>(string)`
- `From<T>()`、`From<T>(string)`
- `Where(...)`、`OrWhere(...)`、`WhereGroup(...)`、`OrWhereGroup(...)`
- `OrderBy(...)`、`OrderByDescending(...)`
- `Limit(...)`、`Offset(...)`、`Page(pageNumber, pageSize)`
- `ToCommand()`
- typed builder 终止方法：`Query()`、`QueryFirstOrDefault()`、`QuerySingleOrDefault()`
- typed builder async 终止方法：`QueryAsync()`、`QueryFirstOrDefaultAsync()`、`QuerySingleOrDefaultAsync()`
- 非泛型 builder 显式结果类型终止方法：`Query<T>()`、`QueryFirstOrDefault<T>()`、`QuerySingleOrDefault<T>()` 及对应 async 方法
- `SelectExistsFrom<T>()`、`SelectCountFrom<T>()`、`SelectAggregateFrom<T>()` 标量查询入口；`SelectAggregateFrom<T>()` 支持 `Max`、`Min`、`Sum`、`Average`、`CountDistinct`，其中 `Max` / `Min` / `CountDistinct` 保留默认终止方法，`Sum` / `Average` 使用终端 `Execute<TResult>()` / `ExecuteAsync<TResult>()` 指定标量读取类型
- `InsertInto<T>()`、`Update<T>()`、`DeleteFrom<T>()`
- `InsertInto<T>(string)`、`Update<T>(string)`、`DeleteFrom<T>(string)`
- `Value(...)`、`Values(entity)`、`Set(...)`、`WhereKey(entity)`、命令 builder 上的 `Execute()`、`ExecuteAsync()`
- `Insert(entity)`、`Insert(entity, table)`、`Update(entity)`、`Update(entity, table)`、`Delete(entity)`、`Delete(entity, table)` 短入口，常用整实体命令不再通过公开整实体 `Set` 重载表达
- `InsertMany(entities)`、`InsertMany(entities, table)`、`UpdateMany(entities)`、`UpdateMany(entities, table)`、`DeleteMany(entities)`、`DeleteMany(entities, table)` 多实体便利入口；这些是逐实体参数化命令 convenience，不是 provider-native copy/import API，不自动开启事务，空集合返回 0
- `InsertOrIgnore(entity)`、`InsertOrIgnore(entity, table)`、`InsertOrUpdate(entity)`、`InsertOrUpdate(entity, table)` conflict-aware insert 短入口
- `InsertOrIgnoreMany(entities)`、`InsertOrIgnoreMany(entities, table)`、`InsertOrUpdateMany(entities)`、`InsertOrUpdateMany(entities, table)` 多实体 conflict-aware insert 便利入口；这些仍然是逐实体参数化命令 convenience
- conflict-aware builders 支持 `OnConflict(...)`；update variants 支持 `Update(...)`
- `Update<T>()`、`DeleteFrom<T>()` 默认禁止无 `WHERE`，可通过 `AllowAllRows()` 显式放行
- `Db4NetExecutionOptions`：执行时传入 transaction、command timeout、command type；async 终止方法额外支持 `CancellationToken`
- SQL Server、SQLite、PostgreSQL、MySQL 方言
- 集中方言渲染测试覆盖 SQL Server / SQLite / PostgreSQL / MySQL 的标识符引用、分页语法、分页参数顺序和分页校验规则
- `[Table]`、`[Column]` 映射
- 字符串标识符校验
- XML 文档生成
- NuGet README 打包
- PostgreSQL、MySQL、SQL Server 可选真实集成测试
- 测试项目支持本地 `local.runsettings`，存在该文件时 `dotnet test` 会通过 `RunSettingsFilePath` 自动使用它

验证状态：

- `dotnet test`：当前本机配置下测试全部通过；外部数据库测试是否执行取决于 `local.runsettings` 或环境变量。
- `dotnet build -c Release`：最近验证为 0 warning，0 error。
- `dotnet pack src/Db4Net/Db4Net.csproj -c Release`：可生成 `Db4Net.0.1.0-alpha.1.nupkg` 和 `Db4Net.0.1.0-alpha.1.snupkg`。

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

- `SelectQueryBuilder`：面向用户的 SELECT Fluent API，并在 query 终止方法中调用 Dapper。
- `SelectCountQueryBuilder<T>`、`SelectExistsQueryBuilder<T>`、`SelectAggregateQueryBuilder<T>`、`SelectAggregateScalarQueryBuilder<T, TResult>`：面向用户的标量查询 API，覆盖行数、存在性和列级聚合。
- `FilterNode`、`FilterClause`、`FilterGroup`：记录过滤条件树，支持普通条件和显式括号分组。
- `FilterClauseBuilder`：内部集中追加过滤条件和校验操作符值规则。
- `FilterGroupBuilder`、`FilterGroupBuilder<T>`：面向用户的显式分组 API，只暴露过滤方法，不暴露排序、分页或命令渲染。
- `InsertCommandBuilder<T>`、`UpdateCommandBuilder<T>`、`DeleteCommandBuilder<T>`：面向用户的单表命令 Fluent API，并通过 `Execute()` / `ExecuteAsync()` 调用 Dapper。
- `InsertManyCommandBuilder<T>`、`UpdateManyCommandBuilder<T>`、`DeleteManyCommandBuilder<T>`：面向用户的多实体命令 convenience；`ToCommands()` 可检查逐实体命令，`Execute()` / `ExecuteAsync()` 使用 Dapper 对实体序列执行同一条参数化命令并累加影响行数。
- `InsertOrIgnoreCommandBuilder<T>`、`InsertOrUpdateCommandBuilder<T>`、`InsertOrIgnoreManyCommandBuilder<T>`、`InsertOrUpdateManyCommandBuilder<T>`：面向用户的 conflict-aware insert API，支持可检查 SQL 和 Dapper 执行。
- `SelectQueryModel`：记录选择列、表、过滤条件、排序、分页。
- `ScalarQueryModel`：记录标量查询表、投影类型、聚合列和过滤条件，由 count、exists、aggregate 查询共享。
- `InsertCommandModel`、`UpdateCommandModel`、`DeleteCommandModel`：记录命令表、赋值、过滤条件和整表操作放行状态。
- `SelectSqlRenderer`：把查询模型渲染成 SQL 和参数。
- `ScalarSqlRenderer`：把 `COUNT(*)`、`EXISTS`、`MAX`、`MIN`、`SUM`、`AVG`、`COUNT(DISTINCT ...)` 渲染成 SQL 和参数。
- `CommandSqlRenderer`：把 Insert/Update/Delete 命令模型渲染成 SQL 和参数。
- `ManyCommandSqlRenderer`：把多实体 convenience 的逐实体命令模板和可检查命令渲染成 SQL 和参数。
- `ConflictInsertSqlRenderer`：把 `InsertOrIgnore` / `InsertOrUpdate` 渲染成各方言的 conflict-aware insert SQL。
- `FilterSqlRenderer`：递归渲染过滤条件树，并为 `WhereGroup(...)` / `OrWhereGroup(...)` 输出括号。
- `ISqlDialect`：处理标识符引用、分页语法、分页参数顺序。
- `ModelMetadataProvider`：从 CLR 类型和成员选择器构建映射元数据。
- `ModelMetadata<T>`：按实体类型缓存表名、可映射列集合，以及按属性名索引的列字典，减少类型化 API 构建查询时的重复反射和线性查找。
- `RenderedSqlCommand`：承载 `Sql` 和 Dapper `DynamicParameters`。
- `Db4NetExecutionOptions`：承载 Dapper 执行选项，终止方法内部统一转换为 Dapper `CommandDefinition`。

当前 NuGet 元数据：

- `PackageId`: `Db4Net`
- `Version`: `0.1.0-alpha.1`
- `Authors`: `IceCoffee1024`
- `Description`: `Safe, SQL-shaped fluent query and command builder for Dapper.`
- `PackageReleaseNotes`: `0.1.0-alpha.1 adds SQL-shaped single-table SELECT/CUD builders, existence, count, scalar aggregate queries with terminal-typed sum and average, entity and many conveniences, conflict-aware inserts, generated-column safeguards, paging validation, explicit filter groups, lightweight transaction scopes, net8.0/netstandard2.0 package assets, and bilingual documentation.`
- `PackageTags`: `dapper;sql;fluent;query-builder`
- `PackageReadmeFile`: `README.md`
- `PackageLicenseExpression`: `MIT`
- `PackageProjectUrl`: `https://dotnet.db4.dev`
- `RepositoryUrl`: `https://github.com/IceCoffee1024/Db4Net.git`
- `RepositoryType`: `git`
- `GenerateDocumentationFile`: `true`
- `PublishRepositoryUrl`: `true`
- `EmbedUntrackedSources`: `true`
- `SymbolPackageFormat`: `snupkg`
- `IncludeSymbols`: `true`

`PackageProjectUrl` 指向项目网站 `https://dotnet.db4.dev`，该地址当前可访问并展示 Db4Net 文档；源码仓库地址由 `RepositoryUrl` 单独指向 GitHub。

发布包体验已补齐：

- SourceLink。
- Symbols package。

发布说明已补充到根目录 `CHANGELOG.md`，通过 `PackageReleaseNotes` 写入 NuGet 元数据，并打包进 nupkg 根目录。

分页参数顺序由方言决定：

- SQLite：`LIMIT @p1 OFFSET @p2`
- PostgreSQL：`LIMIT @p1 OFFSET @p2`
- MySQL：`LIMIT @p1 OFFSET @p2`
- SQL Server：`OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY`

这是必要的，因为不同数据库的分页语法中 limit/offset 出现顺序不同。

分页校验规则：

- `Offset(...)` 必须与 `Limit(...)` 配套使用，避免偏移量被静默忽略。
- SQL Server 分页必须带 `OrderBy(...)`，因为没有 `ORDER BY` 时 `OFFSET` / `FETCH` 是无效 SQL。

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
- 支持 `[Key]`、`Id` / `<TypeName>Id` 主键约定，用于 `Update(entity)`、`Delete(entity)` 和 `WhereKey(entity)` 生成等值谓词，也作为 conflict-aware insert 的默认 conflict target；entity update/delete 和 many update/delete 仍要求单键，conflict-aware insert 默认目标允许复合 key 元数据；没有主键时不会退化为整表操作。
- 支持 `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` 和 `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` 映射属性在 `Values(entity)` / `Insert(entity)` 中跳过；数据库生成的非键属性也会从 `Update(entity)` / `UpdateMany(entities)` 的实体驱动赋值中跳过；显式 `.Value(...)` / `.Set(...)` 仍由调用者控制，Db4Net 不读取或跟踪数据库生成值。数据库生成成员不能作为默认或显式 conflict target，也不能被 `InsertOrUpdate.Update(...)` 选为冲突更新列。
- 类型化投影会渲染列别名，例如 `[display_name] AS [DisplayName]`，让 Dapper 可以按属性名回填实体。
- 类型化成员选择器只允许选择可映射属性；`[NotMapped]` 成员用于 `Select`、`Where`、`OrderBy`、`Value`、`Set` 时会被拒绝。
- 绑定 `T` 后，字符串字段名也按 CLR 属性名解析。例如 `Select("DisplayName").From<MappedUser>()` 会渲染 `[display_name] AS [DisplayName]`；`Select("display_name").From<MappedUser>()` 会被拒绝。
- 数据库列名只由 `[Column]` 决定，不作为字段字符串输入的公共 API。
- 字符串字段 API 的推荐名称是动态属性名投影，不是自由 SQL 字符串入口。
- 不做自动复数化。

命令 builder 的映射规则与查询 builder 保持一致。例如 `Update<MappedUser>().Set("DisplayName", "Alice")` 会渲染 `[display_name] = @p0`，而 `Set("display_name", "Alice")` 会被拒绝。

表名或视图名覆盖示例里使用 `"user_report_view"`，强调它只覆盖查询来源，不改变字段映射规则。

命令表名覆盖也遵守同一规则。例如 `Update<User>("users_tenant_001")` 只把 SQL 目标表渲染为 `users_tenant_001`；`Set(u => u.Name, ...)`、`Where(u => u.Id, ...)` 仍按 `User` 的属性和 `[Column]` 映射生成列名。这个设计服务于多租户分表、按年月分表、staging 表和 archive 表，同时避免开放自由 SQL 片段。

SQLite 和 PostgreSQL 使用原生 `ON CONFLICT` 语法。MySQL 使用 `ON DUPLICATE KEY UPDATE`；显式 `OnConflict(...)` 用于表达 Db4Net 侧的预期冲突列和更新列推导，但 MySQL 数据库本身会按任意 primary key / unique key 冲突处理。SQL Server 生成方言专属的 conflict-aware command，不承诺 provider-native copy/import、optimized batching 或 set-based synchronization。

## JOIN 策略

Db4Net 长期定位为安全、轻量、可预测的单表/视图 SQL builder，不内置 JOIN builder。

对于稳定、可复用的跨表读模型，推荐在数据库侧定义视图，并通过 `SelectFrom<T>()` 像普通表一样查询。对于高度动态、数据库特定或一次性的复杂 JOIN，推荐直接使用 Dapper 原生 SQL。

这个取舍可以让 Db4Net 避免演变成完整 ORM 或通用 SQL query builder，同时保留 Dapper 的灵活性。

## v1 后续范围

暂缓：

- Join、CTE、子查询、聚合查询
- Provider-native copy/import、set-based synchronization 和高度优化批处理
- 关系加载和 Dapper multi-mapping 辅助能力
- 显式 raw SQL 片段 API，例如 `RawSql.Fragment(...)`
- 复合主键自动持久化语义；需要时使用显式 `Where(...)`

明确不做：

- `Where(u => u.Id == 1)` 这类谓词表达式翻译
- 完整 LINQ Provider
- change tracking / dirty checking
- `SaveChanges()` / unit of work
- 关系级联持久化、lazy loading、proxy
- migrations / schema management
- 自动并发 token 处理

## 关键风险

表达式支持很容易失控。Db4Net 应继续使用 `Where(u => u.Id, Op.Eq, 1)` 这种成员选择器 + 显式操作符 + 参数值的形式，不实现 `Where(u => u.Id == 1)` 这类谓词表达式翻译，避免演变成 LINQ Provider。

标识符注入是主要安全风险。参数只能保护值，不能保护表名和列名。

即使是简单 SQL，不同数据库的语法也有差异。方言抽象必须继续保持清晰，避免把差异硬编码在 builder 中。

API 命名要贴近 Dapper 用户习惯。先使用 Dapper 风格的终止方法，后续再补便利别名。Conflict-aware insert 不提供 `Save`、`Merge`、`Upsert`、`Bulk` 公共别名，避免把 Db4Net 定位推向 ORM、同步框架或 bulk/import 工具。

## 参考

- Dapper NuGet package: https://www.nuget.org/packages/Dapper/
- Dapper GitHub repository: https://github.com/DapperLib/Dapper
- Dapper.SqlBuilder NuGet package: https://www.nuget.org/packages/Dapper.SqlBuilder
- Dapper.SimpleSqlBuilder documentation: https://mishael-o.github.io/Dapper.SimpleSqlBuilder/
