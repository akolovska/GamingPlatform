using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    public interface IGenreService
    {
        List<GenreDto> GetAll();
        ServiceResult Add(string name);
        ServiceResult Delete(Guid id); // only genres no game uses
    }
}
