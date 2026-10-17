# CLAUDE.md

## Project overview

ASP.NET Core 8 MVC gaming store (games, libraries, wishlists, friends, achievements, reviews, dev studios). SQL Server via EF Core, ASP.NET Identity with roles, Stripe Checkout for purchases.

Layers (dependencies point down only):

| Project | Responsibility |
|---|---|
| `GamingPlatform.Web` | Controllers, Razor views, Identity pages, `Program.cs` (DI setup) |
| `GamingPlatform.Service` | Business logic: `Interfaces/I*Service.cs` + `Implementations/*Service.cs` |
| `GamingPlatform.Repository` | `ApplicationDbContext`, generic `IRepository<T>`, migrations |
| `GamingPlatform.Domain` | Entities (`Models/`, all inherit `BaseEntity` with a `Guid Id`), DTOs, enums, `Identity/` (user + `Roles` constants) |

## Conventions to follow

- Controllers talk to **services**, never to `ApplicationDbContext` directly. (Devs, GameDevs, Reviews, GamerAchievements and Achievements controllers are old scaffolding that still break this rule.)
- New services: interface in `Service/Interfaces`, implementation in `Service/Implementations`, registered in `Program.cs`.
- Use `Roles.Admin`, `Roles.Dev`, `Roles.DevOrAdmin` for `[Authorize(Roles = ...)]`, never string literals.
- Bind forms to DTOs (`Domain/DTO`), not entities. Take the current user's id from claims, never from form fields.
- POST actions use `[ValidateAntiForgeryToken]` and re-check permissions (see `GamesController.CanManage`).
- Secrets go in user-secrets or environment variables, never in `appsettings.json` or code.

## Commands

```bash
dotnet build GamingPlatform2.sln
dotnet run --project GamingPlatform.Web
# Migrations (I run these myself):
dotnet ef migrations add <DescriptiveName> --project GamingPlatform.Repository --startup-project GamingPlatform.Web
dotnet ef database update --project GamingPlatform.Repository --startup-project GamingPlatform.Web
```

## Known issues backlog (from the architecture review, 2026-10-08)

1. ~~Leaked secrets in git history (Stripe key, Gmail app password, DB password).~~ **Resolved 2026-10-16:** Stripe keys and Gmail app password rotated; the hosted DB (SmarterASP.net account `akolovska-001`) has expired. Secrets now live in `GamingPlatform.Web/.env` (git-ignored, loaded by `DotNetEnv`). The old values are still in git history but no longer work.
2. ~~`LibrariesController.PaymentSuccess` grants games without verifying the Stripe session.~~ **Resolved 2026-10-16:** `PaymentSuccess` fetches the session by `session_id` and grants only when `PaymentStatus == "paid"`; game and user come from the session `Metadata`; adding to the library is safe to repeat. Free games skip Stripe. Possible follow-up: a `checkout.session.completed` webhook, for buyers who close the tab before returning to the site.
3. Scaffolded controllers have no `[Authorize]`, and `AdminController`'s is commented out.
4. `IRepository.GetAll` returns `IEnumerable`, so services filter whole tables in memory.
5. No unit of work: every repository call runs `SaveChanges`.
6. Missing FKs/unique indexes on `GamerId` + `GameId` (Library, Wishlist, Review).
7. N+1 queries and missing `Include`s in `LibrariesController` / `LibraryService.GetUserGames`.
8. No test project.
