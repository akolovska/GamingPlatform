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

        public List<Genre> GetAll() =>
            _genreRepository.GetAll(g => g)
                .OrderBy(g => g.Name)
                .ToList();

        public Genre? GetById(Guid id) =>
            _genreRepository.Get(selector: g => g, predicate: g => g.Id == id);

        public bool NameExists(string name, Guid? exceptId = null)
        {
            var normalized = name.Trim().ToLower();

            return _genreRepository.GetAll(g => g)
                .Any(g => g.Name.ToLower() == normalized &&
                          (exceptId == null || g.Id != exceptId));
        }

        public bool IsInUse(Guid id) =>
            _gameGenreRepository.GetAll(x => x)
                .Any(x => x.GenreId == id);

        public void Add(Genre genre)
        {
            genre.Name = genre.Name.Trim();
            _genreRepository.Insert(genre);
        }

        public void Update(Genre genre)
        {
            genre.Name = genre.Name.Trim();
            _genreRepository.Update(genre);
        }

        public void Delete(Guid id)
        {
            // Safety net: never delete a genre that games still use
            if (IsInUse(id)) return;

            var genre = GetById(id);
            if (genre != null)
                _genreRepository.Delete(genre);
        }
    }
}