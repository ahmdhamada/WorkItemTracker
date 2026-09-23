# Work Items

A small work item tracker with an ASP.NET Core 8 API, Angular 18 standalone client, and SQL Server persistence.

## Run it

1. Install .NET 8 SDK, Node.js, npm, and SQL Server (LocalDB works on Windows).
2. Set `ConnectionStrings__WorkItems` to a SQL Server connection string as needed. The default is `Server=(localdb)\MSSQLLocalDB;Database=WorkItems;Trusted_Connection=True;TrustServerCertificate=True`.
3. From the repository root run `dotnet run --project WorkItems.Api --urls http://localhost:5000`.
4. In another terminal run `cd client`, `npm install`, then `npm start`. Open `http://localhost:4200`.
5. Run automated tests with `dotnet test`.

The API creates the database schema at startup. For production schema evolution, replace `EnsureCreated` with reviewed EF Core migrations. Set `FrontendOrigin` to the deployed client origin if it differs from `http://localhost:4200`.

## API

- `POST /api/work-items` — JSON `{ "title": "...", "description": "..." }`; returns 201 and starts in `Todo`.
- `GET /api/work-items?title=...&status=Todo&page=1&pageSize=20` — paged response with `items`, `page`, `pageSize`, and `totalCount`.
- `GET /api/work-items/{id}` — retrieve one item.
- `PATCH /api/work-items/{id}/status` — JSON `{ "status": "InProgress" }`.

## Assumptions

- Titles are trimmed and must contain a non-whitespace character; descriptions are optional and limited to 4,000 characters by the database model.
- Title search is a case-insensitive substring search under typical SQL Server collations; results sort newest first. Pagination is one-based, defaults to 20 rows, and caps at 100.
- Status strings accept case-insensitive enum spelling. Only the next sequential status is accepted; same-status requests and backward or skipped changes return 409.
- Created timestamps are UTC. IDs are database-generated integers. Invalid query enum values and request validation errors return 400; missing IDs return 404.
- SQL Server is the durable application store. SQLite is used only for isolated automated tests.
