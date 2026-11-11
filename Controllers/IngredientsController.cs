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
    public class IngredientsController : BaseController
    {
        private readonly DbmsContext _context;

        public IngredientsController(DbmsContext context)
        {
            _context = context;
        }

        // GET: Ingredients
        public async Task<IActionResult> Index(short? productId)
        {
            ViewBag.SelectedProductId = productId; 
            ViewBag.Products = await _context.Finproducts.ToListAsync();

            var ingredients = productId.HasValue
        ? await _context.Ingredients.Where(i => i.Product == productId.Value).ToListAsync()
        : new List<Ingredient>(); // Возвращаем пустой список, если productId не задан

            return View(ingredients);
        }

        // Получаем ингредиенты по выбранному продукту
        [HttpGet]
        public async Task<IActionResult> GetIngredientsByProduct(short productId)
        {
            var ingredients = await _context.Ingredients
                .Include(i => i.RawMaterialsNavigation)
                .Where(i => i.Product == productId)
                .Select(i => new
                {
                    Id = i.Id, 
                    RawMaterialName = i.RawMaterialsNavigation.Name,
                    CountIngredients = i.CountIngredients
                })
                .ToListAsync();

            return Json(ingredients);
        }

       

        // GET: Ingredients/Create
public IActionResult Create(short? productId)
{
            var check = CheckEditPermission();
            if (check != null) return check;
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", productId);
    ViewData["RawMaterials"] = new SelectList(_context.RawMaterials, "Id", "Name");
    return View();
}

// POST: Ingredients/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create([Bind("Id,Product,RawMaterials,CountIngredients")] Ingredient ingredient, short? productId)
{
            var check = CheckEditPermission();
            if (check != null) return check;
            if (ModelState.IsValid)
    {
                // Check if the productId parameter is provided and use it to set the Product property of the ingredient
                if (productId.HasValue)
                {
                    ingredient.Product = productId.Value;
                }

                // Проверка существования ингредиента для выбранного продукта
                var exists = await _context.Ingredients
            .AnyAsync(i => i.Product == ingredient.Product && i.RawMaterials == ingredient.RawMaterials);

        if (exists)
        {
            ModelState.AddModelError("", "Данный ингредиент уже существует для выбранного продукта.");
        }
        else
        {
            _context.Add(ingredient);
            await _context.SaveChangesAsync();

                    
                    return RedirectToAction(nameof(Index), new { productId = ingredient.Product });
        }
    }

    // Если модель не валидна или ингредиент уже существует, возвращаем форму
    ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", ingredient.Product);
    ViewData["RawMaterials"] = new SelectList(_context.RawMaterials, "Id", "Name", ingredient.RawMaterials);
    return View(ingredient);
}

        // GET: Ingredients/Edit/5
        public async Task<IActionResult> Edit(short? id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", ingredient.Product);
            ViewData["RawMaterials"] = new SelectList(_context.RawMaterials, "Id", "Name", ingredient.RawMaterials);
            return View(ingredient);
        }

        // POST: Ingredients/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, [Bind("Id,Product,RawMaterials,CountIngredients")] Ingredient ingredient)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id != ingredient.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ingredient);
                    await _context.SaveChangesAsync();
                    // Redirect to Index with the selected product ID
                    return RedirectToAction(nameof(Index), new { productId = ingredient.Product });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IngredientExists(ingredient.Id))
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
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Id", ingredient.Product);
            ViewData["RawMaterials"] = new SelectList(_context.RawMaterials, "Id", "Id", ingredient.RawMaterials);
            return View(ingredient);
        }

        // GET: Ingredients/Delete/5
        public async Task<IActionResult> Delete(short? id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients
                .Include(i => i.ProductNavigation)
                .Include(i => i.RawMaterialsNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ingredient == null)
            {
                return NotFound();
            }

            return View(ingredient);
        }

        // POST: Ingredients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient != null)
            {
                _context.Ingredients.Remove(ingredient);
            }

            await _context.SaveChangesAsync();
            // Redirect to Index with the selected product ID
            return RedirectToAction(nameof(Index), new { productId = ingredient.Product });
        }

        private bool IngredientExists(short id)
        {
            return _context.Ingredients.Any(e => e.Id == id);
        }
    }
}
