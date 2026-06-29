# Limitations

Db4Net is intentionally focused on safe, SQL-shaped Dapper queries and commands. The query builder is still intentionally compact: it supports typed table/view sources plus single-column `IN` subquery filters, but it is not a full SQL DSL.

Included in the current alpha:

- typed `SELECT`, `INSERT`, `UPDATE`, and `DELETE` builders
- dynamic CLR property-name projection with model validation
- table and view overrides
- entity and many-entity command conveniences
- conflict-aware insert conveniences
- `Where`, `OrWhere`, single-column `WhereIn` subqueries, `WhereGroup`, `OrWhereGroup`, `OrderBy`, `Limit`, `Offset`, and `Page`
- sync and async Dapper-style terminal methods
- existing transaction pass-through, lightweight transaction scopes, command timeout, command type, and async cancellation token support

Intentionally out of scope:

- joins
- provider-native copy/import APIs
- set-based synchronization or optimized batching
- change tracking, dirty checking, `SaveChanges()`, or unit-of-work behavior
- relationship loading, cascade persistence, lazy loading, or proxy generation
- migrations or schema management
- automatic concurrency tokens
- full predicate expression translation such as `Where(u => u.Id == 1)`
- full LINQ provider behavior

For complex joins or database-specific SQL, use Dapper raw SQL directly or expose stable read models through database views.
