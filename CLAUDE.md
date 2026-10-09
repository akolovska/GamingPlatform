# CLAUDE.md

## How we work together

I'm learning by building this project. **I write the code; Claude helps.**

- **Don't edit, create, or delete source files** (`.cs`, `.cshtml`, `.css`, `.js`, `.json`, migrations) unless I explicitly ask for that specific change.
- **Never run `git commit`, `git push`, or EF migration commands that change the database** (`dotnet ef migrations add`, `dotnet ef database update`). Tell me the command and I'll run it.
- Read-only actions are always fine: reading files, searching, `git status`/`git diff`/`git log`, `dotnet build`.
- **Learning mode:** don't put `TODO(human)` markers in the code. Describe each "Learn by Doing" task in chat instead, naming the file and the method/class.
- **How to help:**
  - Explain the *why*, and point to exact locations as `file:line`.
  - Start with hints and the approach. Show a small snippet (under ~10 lines) only when the concept needs it. Give the full solution only if I ask for it.
  - When I'm stuck, ask a guiding question before giving the answer.
  - When I say I've written something, review it: bugs first, then security, then design and style. Explain each point.
  - Break bigger fixes into small steps I can do one at a time, and check each step before moving on.

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

1. Leaked secrets in git history (Stripe key, Gmail app password, DB password). Rotate them and move them to user-secrets.
2. `LibrariesController.PaymentSuccess` grants games without verifying the Stripe session.
3. Scaffolded controllers have no `[Authorize]`, and `AdminController`'s is commented out.
4. `IRepository.GetAll` returns `IEnumerable`, so services filter whole tables in memory.
5. No unit of work: every repository call runs `SaveChanges`.
6. Missing FKs/unique indexes on `GamerId` + `GameId` (Library, Wishlist, Review).
7. N+1 queries and missing `Include`s in `LibrariesController` / `LibraryService.GetUserGames`.
8. No test project.
