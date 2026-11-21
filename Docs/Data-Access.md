# Data Access

[Home](README.md) > Data Access

`Collector.Data` — the DAL: Dapper repositories over PostgreSQL.

## Layout

```
Collector.Data/
├── Entities/         POCOs matching table shapes (+ Auth/, journal, chat…)
├── Interfaces/       every repository has an interface
├── Repositories/     Dapper implementations (+ Auth/)
├── Models/           filter/result shapes (FilteredUserResult…)
└── Services/
    └── DapperStartupService.cs   registers all repos as transient
```

## Conventions

- All repositories require an interface — update the interface when method
  signatures change.
- Enums live in `Collector.Data/Enums` (FeedDocType, GrammarType,
  QueueStatus, …).
- SQL calls hit stored functions (`SELECT public."Feed_Add"(…)`) — business
  logic lives in `Collector.SQL/Functions` + `Stored Procedures`, not inline
  SQL.
- `CollectorRepository` is the shared base.
- Npgsql `IDbConnection` injected per repo.
