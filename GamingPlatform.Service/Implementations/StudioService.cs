using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class StudioService : IStudioService
    {
        private readonly IRepository<Dev> _devRepository;
        private readonly IRepository<GameDev> _gameDevRepository;
        private readonly IRepository<Library> _libraryRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<Review> _reviewRepository;
        private readonly IGameService _gameService;

        public StudioService(
            IRepository<Dev> devRepository,
            IRepository<GameDev> gameDevRepository,
            IRepository<Library> libraryRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<Review> reviewRepository,
            IGameService gameService)
        {
            _devRepository = devRepository;
            _gameDevRepository = gameDevRepository;
            _libraryRepository = libraryRepository;
            _orderItemRepository = orderItemRepository;
            _reviewRepository = reviewRepository;
            _gameService = gameService;
        }

        public StudioPageDto? GetPage(Guid id, string? viewerId, bool isAdmin)
        {
            var studio = _devRepository.Get(d => d, d => d.Id == id);
            if (studio == null) return null;

            // Only games in the store: delisted ones aren't shown publicly
            var games = _gameService.GetByIds(GameIds(id), viewerId, includeDelisted: false)
                .OrderBy(g => g.Title)
                .ToList();
            var listedIds = games.Select(g => g.Id).ToList();
            var scores = _reviewRepository.GetAll(r => r.Score, r => r.GameId != null && listedIds.Contains(r.GameId.Value));
            int positive = scores.Count(ReviewSummary.IsPositive);

            return new StudioPageDto
            {
                Id = studio.Id,
                Name = studio.StudioName ?? "Unnamed studio",
                Games = games,
                ReviewCount = scores.Count,
                ReviewSummary = ReviewSummary.Label(positive, scores.Count),
                PositivePercent = scores.Count > 0 ? ReviewSummary.PositivePercent(positive, scores.Count) : null,
                CanViewDashboard = isAdmin || (viewerId != null && studio.UserId == viewerId)
            };
        }

        public ServiceResult<StudioDashboardDto> GetDashboard(Guid? studioId, string userId, bool isAdmin)
        {
            var own = _devRepository.Get(d => d, d => d.UserId == userId);
            Dev? studio;
            if (isAdmin)
            {
                studio = studioId != null
                    ? _devRepository.Get(d => d, d => d.Id == studioId)
                    : own ?? _devRepository.Get(d => d, orderBy: q => q.OrderBy(d => d.StudioName));
                if (studio == null) return ServiceResult<StudioDashboardDto>.NotFound();
            }
            else
            {
                if (own == null) return ServiceResult<StudioDashboardDto>.Invalid("Your account isn't linked to a studio yet. Ask an admin to link it.");
                if (studioId != null && studioId != own.Id) return ServiceResult<StudioDashboardDto>.Forbidden();
                studio = own;
            }

            // Delisted games are included here: the dev still wants their numbers
            var games = _gameDevRepository.GetAll(
                selector: gd => new { gd.Game.Id, gd.Game.Title, gd.Game.IsDelisted },
                predicate: gd => gd.DevId == studio.Id && gd.GameId != null);
            var ids = games.Select(g => g.Id).ToList();

            // Three small queries for the whole studio, grouped here
            var owners = _libraryRepository.GetAll(l => l.GameId, l => ids.Contains(l.GameId))
                .GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
            var sales = _orderItemRepository.GetAll(
                    selector: i => new { i.GameId, i.Price },
                    predicate: i => ids.Contains(i.GameId) && i.Order.Status == OrderStatus.Paid) // refunds don't count
                .GroupBy(i => i.GameId).ToDictionary(g => g.Key, g => (Units: g.Count(), Revenue: g.Sum(i => i.Price)));
            var reviews = _reviewRepository.GetAll(
                selector: r => new { GameId = r.GameId!.Value, r.Score, r.DateRated },
                predicate: r => r.GameId != null && ids.Contains(r.GameId.Value));

            var stats = games.Select(g =>
            {
                var gameReviews = reviews.Where(r => r.GameId == g.Id).ToList();
                var sold = sales.GetValueOrDefault(g.Id);
                return new GameStatsDto
                {
                    GameId = g.Id,
                    Title = g.Title ?? "Untitled",
                    IsDelisted = g.IsDelisted,
                    Owners = owners.GetValueOrDefault(g.Id),
                    UnitsSold = sold.Units,
                    Revenue = sold.Revenue,
                    ReviewCount = gameReviews.Count,
                    AverageRating = gameReviews.Count > 0 ? gameReviews.Average(r => r.Score) : null,
                    ReviewSummary = ReviewSummary.Label(gameReviews.Count(r => ReviewSummary.IsPositive(r.Score)), gameReviews.Count)
                };
            })
            .OrderByDescending(s => s.Revenue).ThenByDescending(s => s.Owners).ThenBy(s => s.Title)
            .ToList();

            // Average rating per month for the last 12 months (empty months show no rating)
            var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var byMonth = Enumerable.Range(0, 12)
                .Select(i => thisMonth.AddMonths(i - 11))
                .Select(month =>
                {
                    var inMonth = reviews.Where(r => r.DateRated >= month && r.DateRated < month.AddMonths(1)).ToList();
                    return new MonthlyRatingDto
                    {
                        Month = month,
                        ReviewCount = inMonth.Count,
                        AverageRating = inMonth.Count > 0 ? inMonth.Average(r => r.Score) : null
                    };
                })
                .ToList();

            return ServiceResult<StudioDashboardDto>.Success(new StudioDashboardDto
            {
                StudioId = studio.Id,
                StudioName = studio.StudioName ?? "Unnamed studio",
                Revenue = stats.Sum(s => s.Revenue),
                UnitsSold = stats.Sum(s => s.UnitsSold),
                Owners = stats.Sum(s => s.Owners),
                ReviewCount = reviews.Count,
                AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Score) : null,
                Games = stats,
                RatingByMonth = byMonth,
                AllStudios = isAdmin
                    ? _devRepository.GetAll(d => new StudioRefDto { Id = d.Id, Name = d.StudioName ?? "Unnamed studio" }, orderBy: q => q.OrderBy(d => d.StudioName))
                    : new List<StudioRefDto>()
            });
        }

        private List<Guid> GameIds(Guid studioId) =>
            _gameDevRepository.GetAll(gd => gd.GameId!.Value, gd => gd.DevId == studioId && gd.GameId != null);
    }
}
