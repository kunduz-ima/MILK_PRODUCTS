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
    [Authorize(Roles = "Administrator, Director, Manager")]
    public class FinproductsController : BaseController
    {
        private readonly DbmsContext _context;

        public FinproductsController(DbmsContext context)
        {
            _context = context;
        }

        // GET: Finproducts
        public async Task<IActionResult> Index()
        {
            var dbmsContext = _context.Finproducts.Include(f => f.UnitNavigation);
            return View(await dbmsContext.ToListAsync());
        }

        // GET: Finproducts/Details/5
        public async Task<IActionResult> Details(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var finproduct = await _context.Finproducts
                .Include(f => f.UnitNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (finproduct == null)
            {
                return NotFound();
            }

            return View(finproduct);
        }

        // GET: Finproducts/Create
        public IActionResult Create()
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name");
            return View();
        }

        // POST: Finproducts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Unit,CountProducts,Sum")] Finproduct finproduct)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (ModelState.IsValid)
            {
                // Check for duplicate FinProduct names
                var isDuplicate = await _context.Finproducts.AnyAsync(f => f.Name == finproduct.Name);

                if (isDuplicate)
                {
                    ModelState.AddModelError("Name", "A FinProduct with the same name already exists.");
                    ViewData["Unit"] = new SelectList(_context.Units, "Id", "Id", finproduct.Unit);
                    return View(finproduct);
                }

                _context.Add(finproduct);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name", finproduct.Unit);
            return View(finproduct);
        }

        // GET: Finproducts/Edit/5
        public async Task<IActionResult> Edit(short? id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id == null)
            {
                return NotFound();
            }

            var finproduct = await _context.Finproducts.FindAsync(id);
            if (finproduct == null)
            {
                return NotFound();
            }
            ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name", finproduct.Unit);
            return View(finproduct);
        }

        // POST: Finproducts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, [Bind("Id,Name,Unit,CountProducts,Sum")] Finproduct finproduct)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id != finproduct.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(finproduct);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FinproductExists(finproduct.Id))
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
            ViewData["Unit"] = new SelectList(_context.Units, "Id", "Name", finproduct.Unit);
            return View(finproduct);
        }

        // GET: Finproducts/Delete/5
        public async Task<IActionResult> Delete(short? id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id == null)
            {
                return NotFound();
            }

            var finproduct = await _context.Finproducts
                .Include(f => f.UnitNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (finproduct == null)
            {
                return NotFound();
            }

            return View(finproduct);
        }

        // POST: Finproducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var finproduct = await _context.Finproducts.FindAsync(id);
            if (finproduct != null)
            {
                _context.Finproducts.Remove(finproduct);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FinproductExists(short id)
        {
            return _context.Finproducts.Any(e => e.Id == id);
        }
    }
}
