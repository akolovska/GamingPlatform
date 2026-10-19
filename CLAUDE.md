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

- Controllers talk to **services**, never to `ApplicationDbContext` directly. Business rules (permissions, validation, what may happen) live in services; controllers only map the result to a response.
- Services return `ServiceResult` / `ServiceResult<T>` (`NotFound`, `Forbidden`, `Invalid` with form errors, `Ok` with a toast message). Controllers inherit `AppController`, which has `UserId`, `IsAdmin`, `Failure(result)`, `AddErrors(result)` and `Flash(result)`.
- New services: interface in `Service/Interfaces`, implementation in `Service/Implementations`, registered in `Program.cs`.
- Uploaded images go through `IImageService` only (validation, saving, and path-checked deletes).
- Actions that need a confirmed email use `[RequireVerifiedEmail]` (`Web/Filters`).
- The built-in Identity "Manage" pages are switched off in `Program.cs`; account settings (profile, password, two-factor, connected Google account, delete) live in `AccountController`. `Areas/Identity/Pages/Account/ExternalLogin` replaces Identity's Google sign-up so new accounts still give a username and an 18+ date of birth.
- Prices: `Service/Pricing.cs` is the only place that decides what a game costs (sales included). Review labels ("Very Positive"...) come from `Service/ReviewSummary.cs`.
- Buying always goes through `IOrderService` (buy now, gift, cart). Orders are records: never edit them except their `Status`; they keep no foreign key to users so they survive account deletion.
- Notify users through `INotificationService` (it skips deleted accounts).
- Sales can be scheduled (`DiscountStartsAt`/`DiscountEndsAt`). `ISaleAnnouncer` tells wishlisters once per sale (notification, plus email unless they turned `WishlistSaleEmails` off); `Web/Jobs/SaleAnnouncementJob` runs it hourly (`Jobs:SaleAnnouncementMinutes`). Changing a sale resets `SaleAnnouncedAt`.
- Studio pages and the Dev dashboard are `IStudioService` (`StudiosController`); `IDevService` is admin studio management.
- Config keys (in `.env`): `ConnectionStrings__DefaultConnection`, `Stripe__SecretKey`, `Stripe__WebhookSecret`, `EmailSettings__*`, `Authentication__Google__ClientId`/`ClientSecret` (Google login is off without them), `AdminUserName`, `SiteUrl` (links in emails sent by background jobs).
- Use `Roles.Admin`, `Roles.Dev`, `Roles.DevOrAdmin` for `[Authorize(Roles = ...)]`, never string literals.
- Bind forms to DTOs (`Domain/DTO`), not entities. Take the current user's id from claims, never from form fields.
- POST actions use `[ValidateAntiForgeryToken]` and re-check permissions (see `GamesController.CanManage`).
- Secrets go in user-secrets or environment variables, never in `appsettings.json` or code.

## Commands

```bash
dotnet build GamingPlatform2.sln
dotnet run --project GamingPlatform.Web
dotnet test GamingPlatform.Tests   # needs SQL Server LocalDB; uses its own GamingPlatform_Tests database
docker build -t gamingplatform .   # app image; `--target test` builds the test image (see Dockerfile, .github/workflows/ci.yml)
# Migrations (I run these myself):
dotnet ef migrations add <DescriptiveName> --project GamingPlatform.Repository --startup-project GamingPlatform.Web
dotnet ef database update --project GamingPlatform.Repository --startup-project GamingPlatform.Web
```

## Known issues backlog (from the architecture review, 2026-10-08)

1. ~~Leaked secrets in git history (Stripe key, Gmail app password, DB password).~~ **Resolved 2026-10-16:** Stripe keys and Gmail app password rotated; the hosted DB (SmarterASP.net account `akolovska-001`) has expired. Secrets now live in `GamingPlatform.Web/.env` (git-ignored, loaded by `DotNetEnv`). The old values are still in git history but no longer work.
2. ~~`LibrariesController.PaymentSuccess` grants games without verifying the Stripe session.~~ **Resolved 2026-10-16:** `PaymentSuccess` fetches the session by `session_id` and grants only when `PaymentStatus == "paid"`; game and user come from the session `Metadata`; adding to the library is safe to repeat. Free games skip Stripe. **Follow-up done 2026-10-19:** `POST /stripe/webhook` (`StripeWebhookController` → `LibraryService.HandleStripeWebhook`) grants the game on `checkout.session.completed`, for buyers who close the tab. It checks the `Stripe-Signature` header and stays off (404) until `Stripe__WebhookSecret` is set in `.env`; register the URL in the Stripe dashboard to get that secret.
3. ~~Scaffolded controllers have no `[Authorize]`.~~ **Resolved 2026-10-17:** the scaffolded controllers were removed; `AdminController` (genres, studios) is admin-only.
4. ~~`IRepository.GetAll` filters in memory.~~ **Resolved 2026-10-17:** filtering always runs in SQL (predicates, `Any`).
5. ~~No unit of work.~~ **Resolved 2026-10-19:** `IUnitOfWork.InTransaction(...)` wraps multi-step changes in one database transaction (account deletion, accepting/blocking friends, granting a game, studio owner + Dev role). Repository calls still save immediately; inside `InTransaction` those saves commit or roll back together. Use it for any new operation that saves more than once.
6. ~~Missing FKs/unique indexes on `GamerId` + `GameId`.~~ **Resolved** in m17; inserts use `TryInsert`, so duplicates from double clicks are ignored instead of crashing.
7. ~~N+1 queries in library/wishlist/profile pages.~~ **Resolved 2026-10-17:** `GameService` loads the viewer's library/wishlist ids once per page.
8. ~~No test project.~~ **Resolved 2026-10-19:** `GamingPlatform.Tests` (xUnit) tests the services against a real SQL Server database `GamingPlatform_Tests` on LocalDB, which is created and deleted on every run (override with `TEST_CONNECTION_STRING`; the name must end in `_Tests`). Covers the 18+ rule, image path checks, reviews, wishlist/library, friends, account deletion, transactions and the Stripe webhook.
9. **Pending migration (yours to run):** one migration covers everything since m18: new tables `Orders`, `OrderItems`, `CartItems`, `Notifications`, `ReviewVotes`; new `Games` columns `TrailerUrl`, `Screenshots` (JSON list), `DiscountPercent`, `DiscountStartsAt`, `DiscountEndsAt`, `SaleAnnouncedAt`; new `AspNetUsers` column `WishlistSaleEmails` (give existing rows `1`, so current users get sale emails); and the unused `GamerAchievements` table is dropped. Run `dotnet ef migrations add StoreCommerceSocial --project GamingPlatform.Repository --startup-project GamingPlatform.Web`, then `database update`. Until then the app fails on pages that read the new tables.
