---
layout: home

hero:
  name: "Db4Net"
  text: "Safe, SQL-shaped fluent SQL for Dapper"
  tagline: Build parameterized single-table queries and commands without turning Dapper into an ORM.
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started
    - theme: alt
      text: Select Queries
      link: /select

features:
  - title: SQL-shaped API
    details: Use SelectFrom<T>(), InsertInto<T>(), Update<T>(), and DeleteFrom<T>() in statement order.
  - title: Safe by default
    details: Identifiers are validated and quoted by the configured dialect; values are passed as Dapper parameters.
  - title: Dapper stays in charge
    details: Db4Net builds and executes commands, but does not add tracking, relationships, migrations, or SaveChanges().
  - title: Typed mapping
    details: Standard attributes such as Table, Column, Key, and NotMapped drive table and column metadata.
  - title: Entity conveniences
    details: Insert, update, delete, many-entity, and conflict-aware shortcuts reuse the same validated builders.
  - title: Multi-dialect rendering
    details: SQL Server, SQLite, PostgreSQL, and MySQL are supported for quoting, paging, and conflict SQL.
---
