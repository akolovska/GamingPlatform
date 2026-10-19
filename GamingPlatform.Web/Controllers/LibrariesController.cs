using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    // Owned games. Buying happens in OrdersController.
    [Authorize]
    public class LibrariesController : AppController
    {
        private readonly ILibraryService _libraryService;

        public LibrariesController(ILibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        public IActionResult Index() => View(_libraryService.GetLibrary(UserId!, UserId!));

        public IActionResult Details(Guid id)
        {
            var game = _libraryService.GetOwnedGame(UserId!, id);
            return game == null ? NotFound() : View(game);
        }
    }
}
