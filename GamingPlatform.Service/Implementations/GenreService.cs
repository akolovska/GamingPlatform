using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class GenreService : IGenreService
    {
        private readonly IRepository<Genre> _genreRepository;
        private readonly IRepository<GameGenre> _gameGenreRepository;

        public GenreService(IRepository<Genre> genreRepository, IRepository<GameGenre> gameGenreRepository)
        {
            _genreRepository = genreRepository;
            _gameGenreRepository = gameGenreRepository;
        }

        public List<GenreDto> GetAll() =>
            _genreRepository.GetAll(
                selector: g => new GenreDto { Id = g.Id, Name = g.Name, GameCount = g.GameGenres.Count },
                orderBy: q => q.OrderBy(g => g.Name));

        public ServiceResult Add(string name)
        {
            name = name?.Trim() ?? string.Empty;
            if (name.Length is 0 or > 50) return ServiceResult.Invalid("Enter a genre name (max 50 characters).");

            // The unique index on Name rejects duplicates (case-insensitively, with the default collation)
            if (!_genreRepository.TryInsert(new Genre { Name = name }))
                return ServiceResult.Invalid("That genre already exists.");

            return ServiceResult.Ok($"Genre \"{name}\" added.");
        }

        public ServiceResult Delete(Guid id)
        {
            var genre = _genreRepository.Get(selector: g => g, predicate: g => g.Id == id);
            if (genre == null) return ServiceResult.NotFound();

            if (_gameGenreRepository.Any(x => x.GenreId == id))
                return ServiceResult.Invalid("Games still use this genre, so it can't be deleted.");

            _genreRepository.Delete(genre);
            return ServiceResult.Ok($"Genre \"{genre.Name}\" deleted.");
        }
    }
}
