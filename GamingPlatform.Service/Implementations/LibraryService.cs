using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class LibraryService : ILibraryService
    {
        private readonly IRepository<Library> _libraryRepository;
        private readonly IGameService _gameService;

        public LibraryService(IRepository<Library> libraryRepository, IGameService gameService)
        {
            _libraryRepository = libraryRepository;
            _gameService = gameService;
        }

        // Newest first. Delisted games stay in the library: owners keep what they bought.
        public List<GameDto> GetLibrary(string ownerId, string viewerId)
        {
            var ids = _libraryRepository.GetAll(
                selector: l => l.GameId,
                predicate: l => l.GamerId == ownerId,
                orderBy: q => q.OrderByDescending(l => l.DateAdded));
            return _gameService.GetByIds(ids, viewerId, includeDelisted: true);
        }

        public GameDto? GetOwnedGame(string userId, Guid gameId)
        {
            if (!_libraryRepository.Any(l => l.GamerId == userId && l.GameId == gameId)) return null;
            return _gameService.GetByIds(new[] { gameId }, userId, includeDelisted: true).FirstOrDefault();
        }
    }
}
