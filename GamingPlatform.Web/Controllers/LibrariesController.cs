using GamingPlatform.Domain.Models;
using GamingPlatform.Domain.Models.DTO;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GamingPlatform.Web.Controllers
{
    public class LibrariesController : Controller
    {
        private readonly ILibraryService _libraryService;
        private readonly IGameService _gameService;
        private readonly IGamerService _gamerService;

        public LibrariesController(ILibraryService libraryService, IGameService gameService, IGamerService gamerService)
        {
            _libraryService = libraryService;
            _gameService = gameService;
            _gamerService = gamerService;
        }

        // GET: Libraries
        public IActionResult Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var libraryEntries = _libraryService.GetAllLibrariesByUser(Guid.Parse(userId));

            var libraryDtos = libraryEntries.Select(l => new LibraryDTO()
            {
                Id = l.Id,
                GameName = l.Game.Title,
                DateAdded = l.DateAdded,
                PlayTimeHours = l.PlayTimeHours
            });

            return View(libraryDtos);
        }


        // GET: Libraries/Details/5
        public IActionResult Details(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var library = _libraryService.GetLibraryById(id);
            if (library == null)
            {
                return NotFound();
            }

            return View(library);
        }


        // GET: Libraries/Edit/5
        public IActionResult Edit(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var library = _libraryService.GetLibraryById(id);
            if (library == null)
            {
                return NotFound();
            }
            var dto = new LibraryDTO()
            {
                Id = library.Id,
                GameName = _gameService.GetGameById(library.GameId).Title,
                DateAdded = library.DateAdded,
                PlayTimeHours = library.PlayTimeHours
            };

            return View(dto);
        }

        // POST: Libraries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, [Bind("GamerId,GameName,DateAdded,PlayTimeHours,Id")] LibraryDTO libraryDTO)
        {
            if (id != libraryDTO.Id)
            {
                return NotFound();
            }

            Library library = _libraryService.GetLibraryById(libraryDTO.Id);
            


            if (ModelState.IsValid)
            {
                try
                {
                    var game = _gameService.GetAllGames()
                        .FirstOrDefault(x => x.Title.Equals(libraryDTO.GameName, StringComparison.OrdinalIgnoreCase));
                    if (game == null)
                    {
                        ModelState.AddModelError("GameName", "Game not found.");
                        return View(libraryDTO);
                    }

                    library.GameId = game.Id;
                    library.DateAdded = libraryDTO.DateAdded;
                    library.PlayTimeHours = libraryDTO.PlayTimeHours;
                    _libraryService.UpdateLibrary(library);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (library.Id == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            var dto = new LibraryDTO()
            {
                Id = library.Id,
                GameName = _gameService.GetGameById(library.GameId).Title,
                DateAdded = library.DateAdded,
                PlayTimeHours = library.PlayTimeHours
            };

            return View(dto);
        }
    }
}
