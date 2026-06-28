# Getting Started

Db4Net is a lightweight fluent SQL builder for safe, parameterized Dapper queries and commands. It is not an ORM and does not try to become a LINQ provider.

## Install

```bash
dotnet add package Db4Net --prerelease
```

Package assets target `net8.0` and `netstandard2.0`.

## First Query

```csharp
using Db4Net;

var user = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .QuerySingleOrDefaultAsync();
```

`UseDb4Net(...)` creates a Db4Net facade bound to an `IDbConnection`, so terminal methods execute through Dapper.

## Inspect SQL

Use `Db4NetDatabase.Create(...)` when you only want to build SQL:

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.SqlServer)
    .Select<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.Eq, 1)
    .ToCommand();

Console.WriteLine(command.Sql);
```

```sql
SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0
```

::: tip
Db4Net validates identifiers and parameterizes values, but Dapper still handles execution and materialization.
:::
