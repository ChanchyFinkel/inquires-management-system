# Architecture Plan — Inquiries Management System (Inquires)

Full architecture plan for the Full Stack exam project, covering: DB First approach, separate `Status`/`Priority` tables (not enums) with foreign keys, a 4-project server layering with explicit dependency direction, a Cache challenge implementation, a global error-handling middleware, efficient filtering/sorting design, and PascalCase naming for all server-side methods.

---

## 1. DB First Approach — Database Schema

The idea: design the database structure first (SQL script), then generate the EF Core models from it (`Scaffold-DbContext`). The database is the source of truth, not the C# code.

### 1.1 `Statuses` table (instead of an enum)

```sql
CREATE TABLE Statuses (
    StatusId    INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(50) NOT NULL UNIQUE  -- New, InProgress, Waiting, Completed
);

INSERT INTO Statuses (Name) VALUES
('New'), ('InProgress'), ('Waiting'), ('Completed');
```

### 1.2 `Priorities` table (instead of an enum)

```sql
CREATE TABLE Priorities (
    PriorityId  INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(50) NOT NULL UNIQUE  -- Low, Medium, High
);

INSERT INTO Priorities (Name) VALUES
('Low'), ('Medium'), ('High');
```

### 1.3 `Inquiries` table (foreign keys to both lookup tables)

```sql
CREATE TABLE Inquiries (
    InquiryId         INT IDENTITY(1,1) PRIMARY KEY,
    Title             NVARCHAR(200)   NOT NULL,
    OrganizationName  NVARCHAR(200)   NOT NULL,
    StatusId          INT             NOT NULL,
    PriorityId        INT             NOT NULL,
    CreatedAt         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVersion        ROWVERSION      NOT NULL,  -- for a future Concurrency challenge, if chosen

    CONSTRAINT FK_Inquiries_Status   FOREIGN KEY (StatusId)   REFERENCES Statuses(StatusId),
    CONSTRAINT FK_Inquiries_Priority FOREIGN KEY (PriorityId) REFERENCES Priorities(PriorityId)
);

-- Indexes for filtering/sorting performance (see section 4)
CREATE INDEX IX_Inquiries_StatusId   ON Inquiries(StatusId);
CREATE INDEX IX_Inquiries_PriorityId ON Inquiries(PriorityId);
CREATE INDEX IX_Inquiries_CreatedAt  ON Inquiries(CreatedAt DESC);
CREATE INDEX IX_Inquiries_Org        ON Inquiries(OrganizationName);
```

**Note for README:** using separate lookup tables instead of enums enables future extensibility (adding a status/priority without a code deploy), FK-level referential integrity, and the option to attach future metadata (e.g. color, display order) to each value.

### 1.4 Seeding 10,000 test records

A separate script (`Seed.sql`, or a small .NET seeding console) generates random records with random `StatusId`/`PriorityId` from the existing lookup tables, and `CreatedAt` timestamps spread over time — so filtering/sorting/pagination are tested against realistic data volume, not a handful of rows.

---

## 2. Server Project Structure & Dependencies

```
                ┌───────────────────┐
                │   Inquires.Api      │  Controllers, Middleware, Program.cs
                └─────────┬──────────┘
                          │ references
                ┌─────────▼──────────┐
                │ Inquires.Services   │  Business Logic, Interfaces
                └─────────┬──────────┘
                          │ references
                ┌─────────▼──────────┐
                │   Inquires.Data     │  DbContext, Entities, Repositories
                └─────────▲──────────┘
                          │ references
                ┌─────────┴──────────┐
                │   Inquires.DTO      │  Request/Response Models, Mapping
                └────────────────────┘
```

| Project | Responsibility | References |
|---|---|---|
| **Inquires.Data** | `DbContext` (scaffolded from the DB), Entities (`Inquiry`, `Status`, `Priority`), Repository interfaces + implementations, filter/query specification objects | — (base layer, no references to other projects) |
| **Inquires.DTO** | `InquiryDto`, `CreateInquiryRequest`, `UpdateStatusRequest`, `PagedResult<T>`, `InquiryQueryParameters`, and mapping extensions (`static class InquiryMappingExtensions`) between Entity and DTO | `Inquires.Data` (needed to map Entity → DTO) |
| **Inquires.Services** | Business logic: `IInquiryService`/`InquiryService`, validation, calls the repository, drives caching | `Inquires.Data`, `Inquires.DTO` |
| **Inquires.Api** | Controllers (`InquiriesController`), `Program.cs`, `ExceptionHandlingMiddleware`, DI registration, Swagger | `Inquires.Services` (and transitively DTO) |

**Key point for the interview:** the Api project never touches Data directly — every DB access goes through Services. This is a clean separation of concerns that lets the Data layer be swapped or mocked in tests without touching Api.

### Suggested folder layout per project

```
Inquires.Data/
  Entities/          -> Inquiry.cs, Status.cs, Priority.cs
  Context/            -> InquiresDbContext.cs
  Repositories/       -> IInquiryRepository.cs, InquiryRepository.cs
  Specifications/     -> InquiryFilterSpecification.cs

Inquires.DTO/
  Requests/            -> CreateInquiryRequest.cs, UpdateInquiryStatusRequest.cs, InquiryQueryParameters.cs
  Responses/           -> InquiryDto.cs, PagedResult.cs
  Mapping/             -> InquiryMappingExtensions.cs

Inquires.Services/
  Interfaces/          -> IInquiryService.cs
  Implementations/     -> InquiryService.cs
  Caching/              -> ICacheService.cs, MemoryCacheService.cs
  Exceptions/           -> NotFoundException.cs, ValidationException.cs

Inquires.Api/
  Controllers/          -> InquiriesController.cs
  Middleware/            -> ExceptionHandlingMiddleware.cs
  Program.cs
```

---

## 3. Data & DTO Layer — Sample Code

```csharp
// Inquires.Data/Entities/Inquiry.cs
public class Inquiry
{
    public int InquiryId { get; set; }
    public string Title { get; set; } = null!;
    public string OrganizationName { get; set; } = null!;
    public int StatusId { get; set; }
    public int PriorityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Status Status { get; set; } = null!;
    public Priority Priority { get; set; } = null!;
}
```

```csharp
// Inquires.DTO/Requests/InquiryQueryParameters.cs
public class InquiryQueryParameters
{
    public string? SearchTerm { get; set; }
    public int? StatusId { get; set; }
    public int? PriorityId { get; set; }
    public string? OrganizationName { get; set; }

    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

```csharp
// Inquires.DTO/Responses/PagedResult.cs
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

---

## 4. Smart Filtering & Sorting Design

**Core principle:** everything happens in the database, not in memory. Never call `ToList()` and then filter in C# — always build an `IQueryable` and only call `ToListAsync()` at the very end.

### 4.1 Building the query step by step (Repository)

```csharp
// Inquires.Data/Repositories/InquiryRepository.cs
public async Task<(List<Inquiry> Items, int TotalCount)> GetFilteredAsync(
    InquiryQueryParameters query, CancellationToken ct)
{
    var q = _context.Inquiries
        .AsNoTracking()
        .Include(i => i.Status)
        .Include(i => i.Priority)
        .AsQueryable();

    // Filtering
    if (query.StatusId.HasValue)
        q = q.Where(i => i.StatusId == query.StatusId.Value);

    if (query.PriorityId.HasValue)
        q = q.Where(i => i.PriorityId == query.PriorityId.Value);

    if (!string.IsNullOrWhiteSpace(query.OrganizationName))
        q = q.Where(i => i.OrganizationName.Contains(query.OrganizationName));

    if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        q = q.Where(i => i.Title.Contains(query.SearchTerm)
                       || i.OrganizationName.Contains(query.SearchTerm));

    // Count before paging (executed in the DB, not in memory)
    var totalCount = await q.CountAsync(ct);

    // Dynamic sorting — see 4.2
    q = ApplySort(q, query.SortBy, query.SortDescending);

    // Paging
    var items = await q
        .Skip((query.PageNumber - 1) * query.PageSize)
        .Take(query.PageSize)
        .ToListAsync(ct);

    return (items, totalCount);
}
```

### 4.2 Safe dynamic sorting (no SQL injection)

A fixed whitelist of allowed sort fields, not free-form string concatenation:

```csharp
private static IQueryable<Inquiry> ApplySort(
    IQueryable<Inquiry> q, string sortBy, bool desc)
{
    Expression<Func<Inquiry, object>> keySelector = sortBy switch
    {
        "Title"            => i => i.Title,
        "OrganizationName" => i => i.OrganizationName,
        "Status"           => i => i.Status.Name,
        "Priority"         => i => i.Priority.Name,
        "UpdatedAt"        => i => i.UpdatedAt,
        _                  => i => i.CreatedAt   // default
    };

    return desc ? q.OrderByDescending(keySelector) : q.OrderBy(keySelector);
}
```

This avoids using `System.Linq.Dynamic.Core` with free-form input from the client (a security and maintainability risk), while still implementing flexible sorting across several fields. New fields are easy to add to the `switch`.

### 4.3 Aggregations

A separate endpoint (`GET /api/inquiries/summary`) returns counts by status/priority — computed in the DB with `GroupBy`, and cached (see section 5) since it's queried frequently and changes less often than the raw list.

```csharp
var summary = await _context.Inquiries
    .GroupBy(i => i.Status.Name)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToListAsync(ct);
```

---

## 5. Chosen Challenge: Cache

### 5.1 What to cache and why

| What | Why | Suggested TTL |
|---|---|---|
| `Statuses`, `Priorities` (lookup tables) | Rarely change, loaded on every request for filter dropdowns | Sliding, 30 min |
| Aggregation results (`/summary`) | Relatively expensive GroupBy, viewed often on a dashboard | Absolute, 1–2 min |
| First page of the unfiltered list | The most common query | Absolute, 30–60 sec |

**What NOT to cache:** results for arbitrary filter combinations (cache-key blow-up) — only cache the common/cheap-to-identify cases.

### 5.2 Implementation (IMemoryCache, easily swappable for Redis)

```csharp
// Inquires.Services/Caching/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}
```

```csharp
// Inquires.Services/Implementations/InquiryService.cs (relevant excerpt)
public async Task<InquiryDto> UpdateStatusAsync(int id, int newStatusId, CancellationToken ct)
{
    var inquiry = await _repository.GetByIdAsync(id, ct)
        ?? throw new NotFoundException($"Inquiry {id} not found");

    if (!await _repository.StatusExistsAsync(newStatusId, ct))
        throw new ValidationException("Invalid status value");

    inquiry.StatusId = newStatusId;
    inquiry.UpdatedAt = DateTime.UtcNow;
    await _repository.SaveChangesAsync(ct);

    // Invalidate relevant cache — whenever the underlying data changes
    await _cache.RemoveByPrefixAsync("inquiries:summary");
    await _cache.RemoveByPrefixAsync("inquiries:list:page1");

    return inquiry.ToDto();
}
```

**Invalidation strategy:** on every `Create` / `UpdateStatus` / `Delete`, invalidate the aggregation cache keys and the cached "first page." The lookup tables (Status/Priority) are only invalidated if there's an admin endpoint that changes them (not expected within this task's scope — worth mentioning in the README as a future extension point).

**Note for README:** explain briefly that `IMemoryCache` was chosen for simplicity given the task's scope, but the design (`ICacheService`) allows switching to `IDistributedCache`/Redis in a multi-instance production environment without changing any calling code.

---

## 6. Global Error-Handling Middleware

```csharp
// Inquires.Api/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            var (statusCode, title) = ex switch
            {
                NotFoundException   => (StatusCodes.Status404NotFound, "Resource not found"),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
                _                    => (StatusCodes.Status500InternalServerError, "Internal server error")
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
```

Registration in `Program.cs` (at the very start of the pipeline, before everything else):

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

This directly covers what the exam document requires: a non-existent inquiry (`NotFoundException` → 404), an invalid status value (`ValidationException` → 400), an invalid request (model validation → automatic 400 via `[ApiController]`), and an error during the update process (fallback to 500 with logging).

---

## 7. Naming Convention — PascalCase

This is already the official .NET convention (Microsoft C# Conventions), so it comes "for free" as long as it's applied consistently:

```csharp
// Correct
public async Task<PagedResult<InquiryDto>> GetInquiriesAsync(InquiryQueryParameters query) { ... }
public async Task<InquiryDto> UpdateInquiryStatusAsync(int id, int statusId) { ... }

// Incorrect
public async Task<PagedResult<InquiryDto>> getInquiries(...) { ... }
```

Applies to: method names in Controllers/Services/Repositories, and Property names in DTOs/Entities. Parameters and local variables stay camelCase as usual — it's worth explaining in the interview that this follows official .NET conventions rather than being an arbitrary "capitalize the first letter" rule.

---

## 8. Client Layer (Angular)

```
src/app/inquiries/
  inquiry-list/           -> table + filters + sorting + pagination
  inquiry-filter-bar/      -> separate filtering component (Status/Priority/Search)
  inquiry-status-badge/    -> status display
  inquiry-summary/          -> aggregation display (summary cards/chart)
  services/
    inquiry.service.ts      -> HTTP calls only, no business logic
  models/
    inquiry.model.ts, query-params.model.ts
```

**Principles:**
- **Clean service:** `InquiryService` only sends HTTP requests and returns `Observable`s; no UI-state manipulation inside it.
- **State in a component/light store:** a single `BehaviorSubject` in a Facade service to manage filter/paging state — avoids prop drilling.
- **Debounced search:** `distinctUntilChanged` + `debounceTime(300)` on the search box, to avoid flooding the server with requests.
- **Explicit UI states:** `loading | error | empty | data` handled explicitly in the template (not just "if there's data, show the table").

```typescript
this.searchControl.valueChanges.pipe(
  debounceTime(300),
  distinctUntilChanged(),
  switchMap(term => this.inquiryService.getInquiries({ ...this.currentQuery, searchTerm: term }))
).subscribe(...)
```

---

## 9. Recommended Build Order (Submission Checklist)

1. SQL script: `Statuses`, `Priorities`, `Inquiries` + seed of 10,000 records.
2. `Scaffold-DbContext` → `Inquires.Data`.
3. `Inquires.DTO` — models + mapping.
4. `Inquires.Services` — `IInquiryService`, validation, `ICacheService`.
5. `Inquires.Api` — Controller, `ExceptionHandlingMiddleware`, Swagger, DI registration.
6. One meaningful unit test (e.g. on `ApplySort`/filtering, or on `UpdateInquiryStatusAsync` with a mocked repository).
7. Angular: `InquiryService` → `InquiryListComponent` → filtering/sorting/paging → loading/error/empty handling.
8. `README.md`: run instructions, technologies used, brief solution structure, one key technology decision and its rationale (e.g. "separate Status/Priority tables instead of enums"), and one thing you'd improve with more time (e.g. moving to Redis, adding concurrency handling with `RowVersion`).

---

*This document is an architecture plan — not final code. It's meant to serve as the basis for the actual implementation and for answers in the follow-up interview.*
