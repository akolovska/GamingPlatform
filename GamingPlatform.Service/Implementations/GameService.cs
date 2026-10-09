using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GamingPlatform.Service.Implementations
{
    public class GameService : IGameService
    {
        private readonly IRepository<Game> _gameRepository;
        private readonly IRepository<Library> _libraryRepository;

        public GameService(IRepository<Game> gameRepository, IRepository<Library> libraryRepository)
        {
            _gameRepository = gameRepository;
            _libraryRepository = libraryRepository;
        }

        // Everything the views and DTO mapping touch
        private static IQueryable<Game> WithDetails(IQueryable<Game> q) => q;

        public void AddGame(Game game) => _gameRepository.Insert(game);

        public void UpdateGame(Game game) => _gameRepository.Update(game);

        public void DeleteGame(Guid id)
        {
            var game = GetGameById(id);
            if (game != null)
                _gameRepository.Delete(game);
        }

        public List<Game> GetAllGames(bool includeDelisted = false) =>
            _gameRepository.GetAll(
                selector: x => x,
                predicate: x => includeDelisted || !x.IsDelisted,
                include: q => q
                    .Include(g => g.GameGenres).ThenInclude(x => x.Genre)
                    .Include(g => g.GameDevelopers).ThenInclude(d => d.Developer)
            ).ToList();
        public Game? GetGameById(Guid id) =>
            _gameRepository.Get(
                selector: x => x,
                predicate: x => x.Id == id,
                include: q => q
                    .Include(g => g.GameGenres).ThenInclude(x => x.Genre)
                    .Include(g => g.GameDevelopers).ThenInclude(d => d.Developer)
                    .Include(g => g.Achievements)
                    .Include(g => g.Reviews)
            );

        public List<Game> GetGamesByIds(List<Guid> ids) =>
            _gameRepository.GetAll(
                selector: x => x,
                predicate: x => ids.Contains(x.Id),
                include: q => q
                    .Include(g => g.GameGenres).ThenInclude(x => x.Genre)
                    .Include(g => g.GameDevelopers).ThenInclude(d => d.Developer)
            ).ToList();

        // Games added to the most libraries in the last N days
        public List<Game> GetTrending(int take = 8, int days = 7)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var topIds = _libraryRepository.GetAll(
                    selector: l => l,
                    predicate: l => l.DateAdded >= since)
                .GroupBy(l => l.GameId)
                .Select(g => new { GameId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(take)
                .Select(x => x.GameId)
                .ToList();

            if (!topIds.Any()) return new List<Game>();

            return GetGamesByIds(topIds.Select(i => (Guid)i).ToList())
                .Where(g => !g.IsDelisted)
                .OrderBy(g => topIds.IndexOf(g.Id))
                .ToList();
        }
    }
}