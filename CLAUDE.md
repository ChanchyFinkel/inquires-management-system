# Inquiries Coding Conventions

- Follow the architecture defined in `ARCHITECTURE.md` exactly; do not add layers or speculative abstractions.
- Use PascalCase for every server-side method name, including controllers, services, repositories, and middleware.
- Use standard C# naming conventions for types and properties; use camelCase for parameters and local variables.
- Prefix private instance fields with an underscore (`_repository`, `_cache`, `_context`); constants and `static readonly` fields stay PascalCase.
- Keep the four-project dependency direction: `Inquiries.Api` -> `Inquiries.Services` -> `Inquiries.Data` and `Inquiries.DTO`; `Inquiries.DTO` references `Inquiries.Data`.
- Keep database-first development order: define SQL schema and seed data before creating EF entities.
- Keep filtering, sorting, counting, and paging in the database; do not materialize before applying query operations.
- Use `AsNoTracking` for read-only repository queries.
- Add XML documentation only to public controller actions and public service interface members; omit it for internal and private methods.
- Prefer CLI-generated boilerplate and existing project patterns; keep changes scoped to the exam requirements.
