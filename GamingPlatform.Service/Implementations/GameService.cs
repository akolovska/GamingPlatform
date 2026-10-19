using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace GamingPlatform.Service.Implementations
{
    public class GameService : IGameService
    {
        public const int StorePageSize = 24;
        public const int MaxScreenshots = 10;

        private readonly IRepository<Game> _gameRepository;
        private readonly IRepository<Genre> _genreRepository;
        private readonly IRepository<Library> _libraryRepository;
        private readonly IRepository<Wishlist> _wishlistRepository;
        private readonly IRepository<CartItem> _cartRepository;
        private readonly IRepository<ReviewVote> _voteRepository;
        private readonly IDevService _devService;
        private readonly IFriendService _friendService;
        private readonly ISaleAnnouncer _saleAnnouncer;
        private readonly IImageService _images;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public GameService(
            IRepository<Game> gameRepository,
            IRepository<Genre> genreRepository,
            IRepository<Library> libraryRepository,
            IRepository<Wishlist> wishlistRepository,
            IRepository<CartItem> cartRepository,
            IRepository<ReviewVote> voteRepository,
            IDevService devService,
            IFriendService friendService,
            ISaleAnnouncer saleAnnouncer,
            IImageService images,
            UserManager<GamingPlatformUser> userManager)
        {
            _gameRepository = gameRepository;
            _genreRepository = genreRepository;
            _libraryRepository = libraryRepository;
            _wishlistRepository = wishlistRepository;
            _cartRepository = cartRepository;
            _voteRepository = voteRepository;
            _devService = devService;
            _friendService = friendService;
            _saleAnnouncer = saleAnnouncer;
            _images = images;
            _userManager = userManager;
        }

        // What a game card needs (genres, studio, reviews for the summary)
        private static IIncludableQueryable<Game, object> WithCardData(IQueryable<Game> q) =>
            q.Include(g => g.GameGenres).ThenInclude(x => x.Genre)
             .Include(g => g.GameDevelopers).ThenInclude(d => d.Developer)
             .Include(g => g.Reviews);

        // Everything the store page of one game and CanManage read
        private static IIncludableQueryable<Game, object> WithDetails(IQueryable<Game> q) =>
            q.Include(g => g.GameGenres).ThenInclude(x => x.Genre)
             .Include(g => g.GameDevelopers).ThenInclude(d => d.Developer)
             .Include(g => g.Reviews)
             .Include(g => g.Achievements);

        // ---------- Store ----------

        public PagedResult<GameDto> Search(StoreQueryDto query, string? viewerId)
        {
            var now = DateTime.UtcNow;
            var text = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim();
            var genreId = query.GenreId;
            bool free = query.Free, onSale = query.OnSale, comingSoon = query.ComingSoon;

            // Everything below runs in SQL: only one page of games is ever loaded
            System.Linq.Expressions.Expression<Func<Game, bool>> filter = g =>
                !g.IsDelisted &&
                (text == null ||
                    g.Title!.Contains(text) ||
                    g.GameGenres.Any(x => x.Genre.Name.Contains(text)) ||
                    g.GameDevelopers.Any(d => d.Developer.StudioName!.Contains(text))) &&
                (genreId == null || g.GameGenres.Any(x => x.GenreId == genreId)) &&
                (!free || (g.Price ?? 0) <= 0) &&
                // Same rule as Pricing.OnSaleInSql (EF can't call it inside this expression)
                (!onSale || ((g.Price ?? 0) > 0 && g.DiscountPercent > 0 &&
                             (g.DiscountStartsAt == null || g.DiscountStartsAt <= now) &&
                             (g.DiscountEndsAt == null || g.DiscountEndsAt > now))) &&
                (!comingSoon || g.ReleaseDate > now);

            var price = Pricing.CurrentPriceInSql(now);
            Func<IQueryable<Game>, IOrderedQueryable<Game>> order = query.Sort switch
            {
                StoreSort.PriceLow => q => q.OrderBy(price).ThenBy(g => g.Title).ThenBy(g => g.Id),
                StoreSort.PriceHigh => q => q.OrderByDescending(price).ThenBy(g => g.Title).ThenBy(g => g.Id),
                StoreSort.Newest => q => q.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Title).ThenBy(g => g.Id),
                StoreSort.TopRated => q => q
                    .OrderByDescending(g => g.Reviews.Any() ? g.Reviews.Average(r => (double)r.Score) : 0)
                    .ThenByDescending(g => g.Reviews.Count()).ThenBy(g => g.Id),
                StoreSort.Popular => q => q.OrderByDescending(g => g.Libraries.Count()).ThenBy(g => g.Title).ThenBy(g => g.Id),
                _ => q => q.OrderBy(g => g.Title).ThenBy(g => g.Id)
            };

            int total = _gameRepository.Count(filter);
            int page = Math.Clamp(query.Page, 1, Math.Max(1, (total + StorePageSize - 1) / StorePageSize));
            var games = _gameRepository.GetAll(g => g, filter, order, WithCardData,
                skip: (page - 1) * StorePageSize, take: StorePageSize);

            return new PagedResult<GameDto>
            {
                Items = ToDtos(games, viewerId),
                Page = page,
                PageSize = StorePageSize,
                TotalCount = total
            };
        }

        // Games added to the most libraries in the last N days
        public List<GameDto> GetTrending(string? viewerId, int take = 8, int days = 7)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var topIds = _libraryRepository.GetAll(l => l.GameId, l => l.DateAdded >= since)
                .GroupBy(id => id)
                .OrderByDescending(g => g.Count())
                .Take(take)
                .Select(g => g.Key)
                .ToList();

            return GetByIds(topIds, viewerId, includeDelisted: false);
        }

        // "Because you played X": for your most recently added games, the store games most like them.
        // Similar = shares genres (one point per shared genre) or the studio (two points),
        // then the better reviewed first. You never see a game you own, or the same game twice.
        public List<RecommendationDto> GetRecommendations(string userId, int rows = 3, int perRow = 6)
        {
            var ownedIds = _libraryRepository.GetAll(
                selector: l => l.GameId,
                predicate: l => l.GamerId == userId,
                orderBy: q => q.OrderByDescending(l => l.DateAdded));
            if (ownedIds.Count == 0) return new List<RecommendationDto>();

            // A few more candidates than rows, because some owned games have nothing similar left
            var basisIds = ownedIds.Take(rows * 3).ToList();
            var basis = _gameRepository.GetAll(
                selector: g => new
                {
                    g.Id,
                    g.Title,
                    GenreIds = g.GameGenres.Select(x => x.GenreId).ToList(),
                    StudioIds = g.GameDevelopers.Select(d => d.DevId).ToList()
                },
                predicate: g => basisIds.Contains(g.Id))
                .OrderBy(b => basisIds.IndexOf(b.Id))
                .ToList();

            var excluded = new List<Guid>(ownedIds);
            var result = new List<RecommendationDto>();
            foreach (var game in basis)
            {
                var genreIds = game.GenreIds;
                var studioIds = game.StudioIds;
                var shown = excluded.ToList(); // a copy, so EF sends this round's list

                var similar = _gameRepository.GetAll(
                    selector: g => g.Id,
                    predicate: g => !g.IsDelisted && !shown.Contains(g.Id) &&
                        (g.GameGenres.Any(x => genreIds.Contains(x.GenreId)) ||
                         g.GameDevelopers.Any(d => studioIds.Contains(d.DevId))),
                    orderBy: q => q
                        .OrderByDescending(g => g.GameGenres.Count(x => genreIds.Contains(x.GenreId))
                                              + (g.GameDevelopers.Any(d => studioIds.Contains(d.DevId)) ? 2 : 0))
                        .ThenByDescending(g => g.Reviews.Count(r => r.Score >= 4))
                        .ThenBy(g => g.Title),
                    take: perRow);
                if (similar.Count == 0) continue;

                excluded.AddRange(similar);
                result.Add(new RecommendationDto
                {
                    BasedOnId = game.Id,
                    BasedOnTitle = game.Title ?? "a game you own",
                    Games = GetByIds(similar, userId, includeDelisted: false)
                });
                if (result.Count == rows) break;
            }
            return result;
        }

        public List<GameDto> GetByIds(IList<Guid> ids, string? viewerId, bool includeDelisted)
        {
            if (ids.Count == 0) return new List<GameDto>();

            var idList = ids.ToList(); // EF translates List.Contains to SQL IN
            var games = _gameRepository.GetAll(
                selector: g => g,
                predicate: g => idList.Contains(g.Id) && (includeDelisted || !g.IsDelisted),
                include: WithDetails);
            return ToDtos(games.OrderBy(g => idList.IndexOf(g.Id)), viewerId);
        }

        public GameDetailsDto? GetDetails(Guid id, string? viewerId, bool isAdmin, string? reviewSort = null)
        {
            var game = GetGameById(id);
            if (game == null) return null;

            bool owns = viewerId != null && _libraryRepository.Any(l => l.GamerId == viewerId && l.GameId == id);
            bool canManage = CanManage(game, viewerId, isAdmin);

            // Delisted games are visible only to owners and to people who manage them
            if (game.IsDelisted && !owns && !canManage) return null;

            var dto = Map<GameDetailsDto>(game, DateTime.UtcNow, owns,
                wished: viewerId != null && _wishlistRepository.Any(w => w.GamerId == viewerId && w.GameId == id),
                inCart: viewerId != null && _cartRepository.Any(c => c.UserId == viewerId && c.GameId == id));

            dto.CanManage = canManage;
            dto.CanReview = owns && !dto.IsComingSoon && !game.Reviews.Any(r => r.GamerId == viewerId);
            dto.TrailerEmbedUrl = YouTube.EmbedUrl(game.TrailerUrl);
            dto.Reviews = MapReviews(game.Reviews, viewerId, isAdmin, reviewSort);

            // Friends who don't own it yet can be sent it as a gift
            if (viewerId != null && !game.IsDelisted)
            {
                var friends = _friendService.GetFriends(viewerId);
                var friendIds = friends.Select(f => f.Id).ToList();
                var owners = _libraryRepository.GetAll(l => l.GamerId, l => l.GameId == id && friendIds.Contains(l.GamerId)).ToHashSet();
                dto.GiftableFriends = friends.Where(f => !owners.Contains(f.Id)).ToList();
            }
            return dto;
        }

        private List<ReviewDto> MapReviews(ICollection<Review> reviews, string? viewerId, bool isAdmin, string? sort)
        {
            // One query each for authors and votes, however many reviews there are
            var reviewIds = reviews.Select(r => r.Id).ToList();
            var votes = _voteRepository.GetAll(v => new { v.ReviewId, v.UserId, v.IsHelpful }, v => reviewIds.Contains(v.ReviewId));
            var authorIds = reviews.Where(r => r.GamerId != null).Select(r => r.GamerId!).Distinct().ToList();
            var names = _userManager.Users
                .Where(u => authorIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.UserName ?? "Unknown");

            var dtos = reviews.Select(r =>
            {
                bool mine = viewerId != null && r.GamerId == viewerId;
                var reviewVotes = votes.Where(v => v.ReviewId == r.Id).ToList();
                return new ReviewDto
                {
                    Id = r.Id,
                    Comment = r.Comment,
                    Score = r.Score,
                    DateRated = r.DateRated,
                    GamerName = r.GamerId != null && names.TryGetValue(r.GamerId, out var name) ? name : "Deleted user",
                    IsMine = mine,
                    CanDelete = mine || isAdmin,
                    Upvotes = reviewVotes.Count(v => v.IsHelpful),
                    Downvotes = reviewVotes.Count(v => !v.IsHelpful),
                    MyVote = reviewVotes.FirstOrDefault(v => v.UserId == viewerId)?.IsHelpful,
                    CanVote = viewerId != null && !mine
                };
            });

            return sort == "newest"
                ? dtos.OrderByDescending(r => r.DateRated).ToList()
                : dtos.OrderByDescending(r => r.Upvotes - r.Downvotes).ThenByDescending(r => r.DateRated).ToList();
        }

        public Game? GetGameById(Guid id) =>
            _gameRepository.Get(selector: g => g, predicate: g => g.Id == id, include: WithDetails);

        public bool CanManage(Game game, string? userId, bool isAdmin)
        {
            if (isAdmin) return true;
            if (userId == null) return false;
            return game.GameDevelopers?.Any(gd => gd.Developer?.UserId == userId) == true;
        }

        // ---------- Create / edit (Dev or Admin) ----------

        public ServiceResult<GameEditDto> GetForEdit(Guid id, string userId, bool isAdmin)
        {
            var game = GetGameById(id);
            if (game == null) return ServiceResult<GameEditDto>.NotFound();
            if (!CanManage(game, userId, isAdmin)) return ServiceResult<GameEditDto>.Forbidden();

            // A running or scheduled sale shows on the form; one that has ended doesn't
            bool sale = Pricing.HasCurrentOrUpcomingSale(game, DateTime.UtcNow);
            return ServiceResult<GameEditDto>.Success(new GameEditDto
            {
                Form = new GameFormDto
                {
                    Id = game.Id,
                    Title = game.Title ?? string.Empty,
                    Description = game.Description,
                    ReleaseDate = game.ReleaseDate,
                    Price = game.Price,
                    Platform = game.Platform,
                    SystemRequirements = game.SystemRequirements,
                    TrailerUrl = game.TrailerUrl,
                    DiscountPercent = sale ? game.DiscountPercent : 0,
                    DiscountStartsAt = sale ? game.DiscountStartsAt : null,
                    // Stored as the moment the sale ends; the form shows the last day of the sale
                    DiscountEndsAt = sale ? game.DiscountEndsAt?.AddDays(-1) : null,
                    GenreIds = game.GameGenres.Select(x => x.GenreId).ToList()
                },
                CoverImageUrl = game.CoverImageUrl,
                Achievements = MapAchievements(game),
                Screenshots = game.Screenshots.ToList()
            });
        }

        public async Task<ServiceResult<Guid>> CreateAsync(GameFormDto form, IFormFile? cover, string userId, bool isAdmin)
        {
            var result = new ServiceResult<Guid>();

            // Admins pick the studio; a Dev always publishes under their own
            Dev? studio = isAdmin
                ? (form.DevId.HasValue ? _devService.GetById(form.DevId.Value) : null)
                : _devService.GetByUserId(userId);
            if (studio == null)
                result.AddError(nameof(GameFormDto.DevId), isAdmin
                    ? "Please choose a studio."
                    : "Your account is not linked to a studio. Contact an admin.");

            var genreIds = ValidGenreIds(form.GenreIds);
            if (genreIds.Count == 0)
                result.AddError(nameof(GameFormDto.GenreIds), "Choose at least one genre.");

            ValidateTrailerAndSale(form, result);

            var coverError = _images.Validate(cover);
            if (coverError != null)
                result.AddError("CoverFile", coverError);

            if (!result.Succeeded) return result;

            // Save the file only once everything else is valid
            var coverUrl = await _images.SaveAsync(cover!, ImageFolders.Covers);
            var game = new Game
            {
                Id = Guid.NewGuid(),
                CoverImageUrl = coverUrl,
                GameDevelopers = new List<GameDev> { new GameDev { DevId = studio!.Id } },
                GameGenres = genreIds.Select(id => new GameGenre { GenreId = id }).ToList()
            };
            ApplyForm(game, form);

            try
            {
                _gameRepository.Insert(game);
            }
            catch
            {
                _images.Delete(coverUrl, ImageFolders.Covers); // don't leave an unused file behind
                throw;
            }
            return ServiceResult<Guid>.Success(game.Id, "Game created.");
        }

        public async Task<ServiceResult> UpdateAsync(Guid id, GameFormDto form, IFormFile? cover, string userId, bool isAdmin)
        {
            var game = GetGameById(id);
            if (game == null) return ServiceResult.NotFound();
            if (!CanManage(game, userId, isAdmin)) return ServiceResult.Forbidden();

            var result = new ServiceResult();
            var genreIds = ValidGenreIds(form.GenreIds);
            if (genreIds.Count == 0)
                result.AddError(nameof(GameFormDto.GenreIds), "Choose at least one genre.");

            ValidateTrailerAndSale(form, result);

            // A new cover is optional on edit; no file keeps the current one
            var coverError = cover == null ? null : _images.Validate(cover);
            if (coverError != null)
                result.AddError("CoverFile", coverError);

            if (!result.Succeeded) return result;

            var oldSale = (game.DiscountPercent, game.DiscountStartsAt, game.DiscountEndsAt);
            var oldCover = game.CoverImageUrl;
            var newCover = cover == null ? null : await _images.SaveAsync(cover, ImageFolders.Covers);

            ApplyForm(game, form);
            if (newCover != null) game.CoverImageUrl = newCover;

            // A new or changed sale gets announced to wishlisters again (by SaleAnnouncer, when it starts)
            if ((game.DiscountPercent, game.DiscountStartsAt, game.DiscountEndsAt) != oldSale)
                game.SaleAnnouncedAt = null;

            // Remove unticked genres, add newly ticked ones
            foreach (var link in game.GameGenres.Where(x => !genreIds.Contains(x.GenreId)).ToList())
                game.GameGenres.Remove(link);
            foreach (var genreId in genreIds.Where(gid => game.GameGenres.All(x => x.GenreId != gid)))
                game.GameGenres.Add(new GameGenre { GameId = game.Id, GenreId = genreId });

            try
            {
                _gameRepository.Update(game);
            }
            catch
            {
                if (newCover != null) _images.Delete(newCover, ImageFolders.Covers);
                throw;
            }

            // The old file goes only after the database points at the new one
            if (newCover != null) _images.Delete(oldCover, ImageFolders.Covers);

            // A sale that starts now is announced right away, without waiting for the hourly job
            if (game.SaleAnnouncedAt == null && Pricing.IsOnSale(game, DateTime.UtcNow))
                _saleAnnouncer.Announce(game.Id);

            return ServiceResult.Ok("Game saved.");
        }

        public ServiceResult SetListed(Guid id, bool listed, string userId, bool isAdmin)
        {
            var game = GetGameById(id);
            if (game == null) return ServiceResult.NotFound();
            if (!CanManage(game, userId, isAdmin)) return ServiceResult.Forbidden();

            game.IsDelisted = !listed;
            _gameRepository.Update(game);
            return ServiceResult.Ok(listed
                ? "Game is listed in the store again."
                : "Game removed from the store. Owners keep it.");
        }

        // ---------- Screenshots ----------

        public async Task<ServiceResult> AddScreenshotsAsync(Guid gameId, IList<IFormFile> files, string userId, bool isAdmin)
        {
            var game = GetGameById(gameId);
            if (game == null) return ServiceResult.NotFound();
            if (!CanManage(game, userId, isAdmin)) return ServiceResult.Forbidden();

            if (files.Count == 0) return ServiceResult.Invalid("Please choose at least one image.");
            if (game.Screenshots.Count + files.Count > MaxScreenshots)
                return ServiceResult.Invalid($"A game can have at most {MaxScreenshots} screenshots.");

            // Check every file before saving any, so a bad one doesn't leave half the upload behind
            foreach (var file in files)
                if (_images.Validate(file) is { } error)
                    return ServiceResult.Invalid($"{file.FileName}: {error}");

            var saved = new List<string>();
            foreach (var file in files)
                saved.Add(await _images.SaveAsync(file, ImageFolders.Screenshots));

            // A new list (not Add on the old one) so EF sees the column changed
            game.Screenshots = game.Screenshots.Concat(saved).ToList();
            try
            {
                _gameRepository.Update(game);
            }
            catch
            {
                foreach (var url in saved) _images.Delete(url, ImageFolders.Screenshots);
                throw;
            }
            return ServiceResult.Ok(files.Count == 1 ? "Screenshot added." : $"{files.Count} screenshots added.");
        }

        public ServiceResult DeleteScreenshot(Guid gameId, string url, string userId, bool isAdmin)
        {
            var game = GetGameById(gameId);
            if (game == null) return ServiceResult.NotFound();
            if (!CanManage(game, userId, isAdmin)) return ServiceResult.Forbidden();

            // Only a URL that's really in this game's list: the posted value can't point at other files
            if (!game.Screenshots.Contains(url)) return ServiceResult.NotFound();

            game.Screenshots = game.Screenshots.Where(s => s != url).ToList();
            _gameRepository.Update(game);
            _images.Delete(url, ImageFolders.Screenshots);
            return ServiceResult.Ok("Screenshot removed.");
        }

        // ---------- Helpers ----------

        private static void ApplyForm(Game game, GameFormDto form)
        {
            game.Title = form.Title.Trim();
            game.Description = form.Description;
            game.ReleaseDate = form.ReleaseDate;
            game.Price = form.Price;
            game.Platform = form.Platform;
            game.SystemRequirements = form.SystemRequirements;
            game.TrailerUrl = YouTube.VideoId(form.TrailerUrl) is { } videoId ? YouTube.WatchUrl(videoId) : null;

            // Dates are whole days (UTC): the sale starts when its first day begins
            // and ends when its last day is over
            game.DiscountPercent = form.DiscountPercent;
            game.DiscountStartsAt = form.DiscountPercent > 0 ? form.DiscountStartsAt?.Date : null;
            game.DiscountEndsAt = form.DiscountPercent > 0 ? form.DiscountEndsAt?.Date.AddDays(1) : null;
        }

        private static void ValidateTrailerAndSale(GameFormDto form, ServiceResult result)
        {
            if (!string.IsNullOrWhiteSpace(form.TrailerUrl) && YouTube.VideoId(form.TrailerUrl) == null)
                result.AddError(nameof(GameFormDto.TrailerUrl), "Use a YouTube link, e.g. https://www.youtube.com/watch?v=...");

            if (form.DiscountPercent > 0 && (form.Price ?? 0) <= 0)
                result.AddError(nameof(GameFormDto.DiscountPercent), "Free games can't be on sale.");

            if (form.DiscountPercent > 0 && form.DiscountEndsAt != null && form.DiscountEndsAt.Value.Date < DateTime.UtcNow.Date)
                result.AddError(nameof(GameFormDto.DiscountEndsAt), "The sale's last day can't be in the past.");

            if (form.DiscountPercent > 0 && form.DiscountStartsAt != null && form.DiscountEndsAt != null &&
                form.DiscountStartsAt.Value.Date > form.DiscountEndsAt.Value.Date)
                result.AddError(nameof(GameFormDto.DiscountStartsAt), "The sale can't start after its last day.");
        }

        // Drops ids of genres that don't exist and duplicates
        private List<Guid> ValidGenreIds(List<Guid> ids)
        {
            var distinct = ids.Distinct().ToList();
            return _genreRepository.GetAll(g => g.Id, g => distinct.Contains(g.Id));
        }

        private List<GameDto> ToDtos(IEnumerable<Game> games, string? viewerId)
        {
            // Three queries for the viewer's flags, instead of three per game
            var owned = new HashSet<Guid>();
            var wished = new HashSet<Guid>();
            var inCart = new HashSet<Guid>();
            if (viewerId != null)
            {
                owned = _libraryRepository.GetAll(l => l.GameId, l => l.GamerId == viewerId).ToHashSet();
                wished = _wishlistRepository.GetAll(w => w.GameId!.Value, w => w.GamerId == viewerId && w.GameId != null).ToHashSet();
                inCart = _cartRepository.GetAll(c => c.GameId, c => c.UserId == viewerId).ToHashSet();
            }

            var now = DateTime.UtcNow;
            return games.Select(g => Map<GameDto>(g, now, owned.Contains(g.Id), wished.Contains(g.Id), inCart.Contains(g.Id))).ToList();
        }

        private static T Map<T>(Game g, DateTime now, bool owned, bool wished, bool inCart) where T : GameDto, new()
        {
            var reviews = g.Reviews ?? new List<Review>();
            int positive = reviews.Count(r => ReviewSummary.IsPositive(r.Score));
            bool onSale = Pricing.IsOnSale(g, now) && (g.Price ?? 0) > 0;

            return new T
            {
                Id = g.Id,
                Title = g.Title ?? "Untitled",
                Description = g.Description,
                Genres = g.GameGenres.Where(x => x.Genre != null).Select(x => x.Genre.Name).OrderBy(n => n).ToList(),
                Developers = g.GameDevelopers?.Where(d => d.Developer != null)
                    .Select(d => new StudioRefDto { Id = d.Developer.Id, Name = d.Developer.StudioName ?? "Unknown" })
                    .ToList() ?? new List<StudioRefDto>(),
                ReleaseDate = g.ReleaseDate,
                Platform = g.Platform,
                CoverImageUrl = g.CoverImageUrl,
                SystemRequirements = g.SystemRequirements,
                Achievements = MapAchievements(g),
                Screenshots = g.Screenshots?.ToList() ?? new List<string>(),
                IsDelisted = g.IsDelisted,

                Price = g.Price,
                CurrentPrice = Pricing.CurrentPrice(g, now),
                DiscountPercent = onSale ? g.DiscountPercent : 0,
                DiscountEndsAt = onSale ? g.DiscountEndsAt : null,
                IsComingSoon = g.ReleaseDate > now,

                Rating = reviews.Count > 0 ? reviews.Average(r => r.Score) : null,
                ReviewCount = reviews.Count,
                ReviewSummary = ReviewSummary.Label(positive, reviews.Count),
                PositivePercent = reviews.Count > 0 ? ReviewSummary.PositivePercent(positive, reviews.Count) : null,

                InLibrary = owned,
                InWishlist = wished,
                InCart = inCart
            };
        }

        private static List<AchievementDto> MapAchievements(Game g) =>
            g.Achievements?
                .OrderBy(a => a.Name)
                .Select(a => new AchievementDto { Id = a.Id, Name = a.Name, Description = a.Description })
                .ToList() ?? new List<AchievementDto>();
    }
}
