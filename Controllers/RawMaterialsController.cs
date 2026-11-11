using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MILK_PRODUCTS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MILK_PRODUCTS.Controllers
{
    [Authorize(Roles = "Administrator, Director, Technologist")]
    public class RawMaterialsController : BaseController
    {
        private readonly DbmsContext _context;

        public RawMaterialsController(DbmsContext context)
        {
            _context = context;
        }

        // GET: RawMaterials
        public async Task<IActionResult> Index()
        {
            var dbmsContext = _context.RawMaterials.Include(r => r.UnitNavigation);
            return View(await dbmsContext.ToListAsync());
        }

        // GET: RawMaterials/Details/5
        public async Task<IActionResult> Details(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rawMaterial = await _context.RawMaterials
                .Include(r => r.UnitNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rawMaterial == null)
            {
                return NotFound();
            }

            return View(rawMaterial);
        }

        // GET: RawMaterials/Create
        public IActionResult Create()
        {
            var check = CheckEditPermission();
            if (check != null) return check;

            ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name");
            return View();
        }

        // POST: RawMaterials/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Unit,CountRawMaterials,Sum")] RawMaterial rawMaterial)
        {
            var check = CheckEditPermission();
            if (check != null) return check;

            if (ModelState.IsValid)
            {
                // Check for duplicate raw material names
                var isDuplicate = await _context.RawMaterials.AnyAsync(r => r.Name == rawMaterial.Name);

                if (isDuplicate)
                {
                    ModelState.AddModelError("Name", "A raw material with the same name already exists.");
                    ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name", rawMaterial.Unit);
                    return View(rawMaterial);
                }
                _context.Add(rawMaterial);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name", rawMaterial.Unit);
            return View(rawMaterial);
        }

        // GET: RawMaterials/Edit/5
        public async Task<IActionResult> Edit(short? id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;

            if (id == null)
            {
                return NotFound();
            }

            var rawMaterial = await _context.RawMaterials.FindAsync(id);
            if (rawMaterial == null)
            {
                return NotFound();
            }
            ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name", rawMaterial.Unit);
            return View(rawMaterial);
        }

        // POST: RawMaterials/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, [Bind("Id,Name,Unit,CountRawMaterials,Sum")] RawMaterial rawMaterial)
        {
            var check = CheckEditPermission();
            if (check != null) return check;

            if (id != rawMaterial.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rawMaterial);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RawMaterialExists(rawMaterial.Id))
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
            ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name", rawMaterial.Unit);
            return View(rawMaterial);
        }

        // GET: RawMaterials/Delete/5
        public async Task<IActionResult> Delete(short? id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id == null)
            {
                return NotFound();
            }

            var rawMaterial = await _context.RawMaterials
                .Include(r => r.UnitNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rawMaterial == null)
            {
                return NotFound();
            }

            return View(rawMaterial);
        }

        // POST: RawMaterials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var rawMaterial = await _context.RawMaterials.FindAsync(id);
            if (rawMaterial != null)
            {
                _context.RawMaterials.Remove(rawMaterial);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RawMaterialExists(short id)
        {
            return _context.RawMaterials.Any(e => e.Id == id);
        }
    }
}
