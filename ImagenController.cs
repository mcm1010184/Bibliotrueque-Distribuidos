using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PR_BIBLIOTRUEQUE.Data;
using PR_BIBLIOTRUEQUE.Models;

namespace PR_BIBLIOTRUEQUE.Controllers
{
    public class ImagenController : Controller
    {
        private readonly DBprbibliotruequeContext _context;

        public ImagenController(DBprbibliotruequeContext context)
        {
            _context = context;
        }

        // GET: Imagen
        public async Task<IActionResult> Index()
        {
            var dBprbibliotruequeContext = _context.Imagens.Include(i => i.IdLibroNavigation);
            return View(await dBprbibliotruequeContext.ToListAsync());
        }

        // GET: Imagen/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Imagens == null)
            {
                return NotFound();
            }

            var imagen = await _context.Imagens
                .Include(i => i.IdLibroNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (imagen == null)
            {
                return NotFound();
            }

            return View(imagen);
        }

        // GET: Imagen/Create
        public IActionResult Create()
        {
            ViewData["IdLibro"] = new SelectList(_context.Libros, "Id", "Id");
            return View();
        }

        // POST: Imagen/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Ruta,IdLibro,Estado,FechaRegistro,FechaActualizacion")] Imagen imagen)
        {
            if (ModelState.IsValid)
            {
                _context.Add(imagen);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdLibro"] = new SelectList(_context.Libros, "Id", "Autor", imagen.IdLibro);
            return View(imagen);
        }

        // GET: Imagen/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Imagens == null)
            {
                return NotFound();
            }

            var imagen = await _context.Imagens.FindAsync(id);
            if (imagen == null)
            {
                return NotFound();
            }
            ViewData["IdLibro"] = new SelectList(_context.Libros, "Id", "Autor", imagen.IdLibro);
            return View(imagen);
        }

        // POST: Imagen/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ruta,IdLibro,Estado,FechaRegistro,FechaActualizacion")] Imagen imagen)
        {
            if (id != imagen.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(imagen);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ImagenExists(imagen.Id))
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
            ViewData["IdLibro"] = new SelectList(_context.Libros, "Id", "Autor", imagen.IdLibro);
            return View(imagen);
        }

        // GET: Imagen/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Imagens == null)
            {
                return NotFound();
            }

            var imagen = await _context.Imagens
                .Include(i => i.IdLibroNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (imagen == null)
            {
                return NotFound();
            }

            return View(imagen);
        }

        // POST: Imagen/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int idLibro)
        {
            if (_context.Imagens == null)
            {
                return Problem("Entity set 'DBprbibliotruequeContext.Imagens'  is null.");
            }
            var imagen = await _context.Imagens.FindAsync(id);
            if (imagen != null)
            {
                _context.Imagens.Remove(imagen);
            }
            
            await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(Index));
            return RedirectToAction("Edit", "Libros", new { id = idLibro });
        }

        private bool ImagenExists(int id)
        {
          return (_context.Imagens?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
