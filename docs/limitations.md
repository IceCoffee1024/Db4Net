# Limitations

Db4Net is intentionally focused on safe, SQL-shaped Dapper queries and commands. The query builder is still intentionally compact: it supports typed table/view sources plus single-column `IN` subquery filters, but it is not a full SQL DSL.

Included in the current alpha:

- typed `SELECT`, `INSERT`, `UPDATE`, and `DELETE` builders
- typed existence and count query builders through `SelectExistsFrom<T>()` and `SelectCountFrom<T>()`
- typed scalar aggregate query builders through `SelectAggregateFrom<T>()`
- paged SELECT terminal methods through `QueryPage(...)` and `QueryPageAsync(...)`
- dynamic CLR property-name projection with model validation
- table and view overrides
- entity and many-entity command conveniences
- regular single-row insert key return
- conflict-aware insert conveniences
- `Where`, `OrWhere`, single-column `WhereIn` subqueries, `WhereGroup`, `OrWhereGroup`, `OrderBy`, `OrderByDescending`, `Limit`, `Offset`, and `Page`
- sync and async Dapper-style terminal methods
- existing transaction pass-through, lightweight transaction scopes, command timeout, command type, and async cancellation token support

Intentionally out of scope:

- joins
- provider-native copy/import APIs
- set-based synchronization or optimized batching
- generated-key readback for `InsertMany(...)` or conflict-aware inserts
- automatic refresh of all database-generated or computed values
- MySQL generated-key readback for non-auto-increment generated keys such as trigger/default/expression-generated values
- SQL Server generated-key readback for trigger-enabled tables that require `OUTPUT ... INTO`
- SQLite generated-key readback on SQLite runtimes older than 3.35
- change tracking, dirty checking, `SaveChanges()`, or unit-of-work behavior
- relationship loading, cascade persistence, lazy loading, or proxy generation
- migrations or schema management
- automatic concurrency tokens
- full predicate expression translation such as `Where(u => u.Id == 1)`
- full LINQ provider behavior

For complex joins or database-specific SQL, use Dapper raw SQL directly or expose stable read models through database views.
