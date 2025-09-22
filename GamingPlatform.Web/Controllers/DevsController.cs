using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Web
{
    public class DevsController : Controller
    {
        private readonly IDevService _devService;

        public DevsController(IDevService devService)
        {
            _devService = devService;
        }

        // GET: Devs
        public IActionResult Index()
        {
            return View(_devService.GetAllDevs());
        }

        // GET: Devs/Details/5
        public IActionResult Details(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dev = _devService.GetDevById(id);
            if (dev == null)
            {
                return NotFound();
            }

            return View(dev);
        }

        // GET: Devs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Devs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudioName,Id")] Dev dev)
        {
            if (ModelState.IsValid)
            {
                dev.Id = Guid.NewGuid();
                _devService.AddDev(dev);
                return RedirectToAction(nameof(Index));
            }
            return View(dev);
        }

        // GET: Devs/Edit/5
        public IActionResult Edit(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dev = _devService.GetDevById(id);
            if (dev == null)
            {
                return NotFound();
            }
            return View(dev);
        }

        // POST: Devs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("StudioName,Id")] Dev dev)
        {
            if (id != dev.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _devService.UpdateDev(dev);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DevExists(dev.Id))
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
            return View(dev);
        }

        // GET: Devs/Delete/5
        public IActionResult Delete(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dev = _devService.GetDevById(id);
            if (dev == null)
            {
                return NotFound();
            }

            return View(dev);
        }

        // POST: Devs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var dev = _devService.GetDevById(id);
            if (dev != null)
            {
                _devService.DeleteDev(id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DevExists(Guid id)
        {
            return _devService.GetAllDevs().Any(e => e.Id == id);
        }
    }
}
