using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PR_BIBLIOTRUEQUE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PR_BIBLIOTRUEQUE.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace PR_BIBLIOTRUEQUE.Controllers
{
    public class LibrosController : Controller
    {
        private readonly DBprbibliotruequeContext _context;

        public LibrosController(DBprbibliotruequeContext context)
        {
            _context = context;

        }
        public async Task<IActionResult> Index()
        {
            var dBprbibliotruequeContext = _context.Libros
                                            .Where(l => l.IdUsuario == Convert.ToInt32(HttpContext.User.FindFirstValue("UserID")))
                                            .Where(l=> l.Estado == 1)
                                            .Include(l => l.IdCategoriaNavigation)
                                            .Include(l => l.IdTipoTapaNavigation)
                                            .Include(l => l.IdDescripcionNavigation)
                                            .Include(l => l.Imagens)
                                            .Include(l => l.IdUsuarioNavigation);

            //return View(await _context.Libros.Include(b => b.Imagens).ToListAsync());

            return View(await dBprbibliotruequeContext.ToListAsync());
        }
        public async Task<IActionResult> LibrosDonados()
        {
            var dBprbibliotruequeContext = _context.Libros
                                            .Where(l => l.Estado == 2)
                                            .Include(l => l.IdCategoriaNavigation)
                                            .Include(l => l.IdTipoTapaNavigation)
                                            .Include(l => l.IdDescripcionNavigation)
                                            .Include(l => l.Imagens)
                                            .Include(l => l.IdUsuarioNavigation);
            return View(await dBprbibliotruequeContext.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewData["IdCategoria"] = new SelectList(_context.Categoria, "Id", "NombreCategoria");
            ViewData["IdTipoTapa"] = new SelectList(_context.TipoTapas, "Id", "TipoTapa1");
            ViewData["IdUsuario"] = new SelectList(_context.Usuarios, "Id", "Nombre");
            ViewData["IdDescripcion"] = new SelectList(_context.Descripcions, "Id", "NombreDescripcion");
            return View();
        }
        //public IActionResult Details()
        //{
        //    return View();
        //}
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new NotFoundResult();
            }

            // Obtener el libro y sus imágenes
            var book = _context.Libros
                .Include(b => b.Imagens)
                .Include(b=>b.IdCategoriaNavigation)
                .Include(b=>b.IdTipoTapaNavigation)
                .SingleOrDefault(b => b.Id == id);

            //if (book == null)
            //{
            //    return NotFoundResult();
            //}
            ViewData["IdCategoria"] = new SelectList(_context.Categoria, "Id", "NombreCategoria", book.IdCategoria);
            return View(book);
        }




        [HttpPost]
        public ActionResult Create(LibroImagen model, List<string> ImagePreviews)
        {
            var form = Request.Form;
            string descrip = Convert.ToString(model.IdDescripcion);
            double price = (double)model.PrecioOriginal;
            string tit = model.Titulo;
            string Ahs = "#" + Regex.Replace(tit, @"\s", "");
            byte puntaje = 0;
            var donar = form["donar"];
            switch (descrip)
            {
                case "1"://Excelente
                    if (price > 500) puntaje = 5;
                    else puntaje = 4;
                    break;
                case "2"://Bueno
                    if (price > 200 && price < 500) puntaje = 4;
                    else puntaje = 3;
                    break;
                case "3"://Regular
                    if (price > 100 && price > 200) puntaje = 3;
                    else puntaje = 2;
                    break;
                case "4"://Malo
                    if (price > 50 && price < 100) puntaje = 2;
                    else puntaje = 1;
                    break;
            }
            try
            {
                try

                {
                    var book = new Libro();
                    if (donar == "true")
                    {
                        book = new Libro
                        {

                            Titulo = model.Titulo,
                            Autor = model.Autor,
                            Editorial = model.Editorial,
                            Edicion = model.Ediccion,
                            PrecioOriginal = model.PrecioOriginal,
                            Descripcion = model.Descripcion,
                            HashTags = Ahs,
                            Puntaje = puntaje,
                            IdCategoria = (byte)model.IdCategoria,
                            IdTipoTapa = (byte)model.IdTipoTapa,
                            IdDescripcion = model.IdDescripcion,
                            //IdUsuario = model.IdUsuario
                            IdUsuario = UserID.GetIdUser(HttpContext),
                            Estado = 2
                        };
                    }
                    else
                    {
                        book = new Libro
                        {

                            Titulo = model.Titulo,
                            Autor = model.Autor,
                            Editorial = model.Editorial,
                            Edicion = model.Ediccion,
                            PrecioOriginal = model.PrecioOriginal,
                            Descripcion = model.Descripcion,
                            HashTags = Ahs,
                            Puntaje = puntaje,
                            IdCategoria = (byte)model.IdCategoria,
                            IdTipoTapa = (byte)model.IdTipoTapa,
                            IdDescripcion = model.IdDescripcion,
                            //IdUsuario = model.IdUsuario
                            IdUsuario = UserID.GetIdUser(HttpContext)
                        };
                    }
                   

                    _context.Libros.Add(book);
                    _context.SaveChanges();


                    // Procesar las imágenes en la vista previa
                    foreach (var imagePreview in ImagePreviews)
                    {
                        // Convertir la imagen de base64 a un array de bytes
                        var base64Data = imagePreview.Substring(imagePreview.IndexOf(',') + 1);
                        var imageData = Convert.FromBase64String(base64Data);

                        //Agregar la imagen al libro
                        var bookImage = new Imagen
                        {
                            Ruta = imageData,
                            IdLibro = book.Id
                        };
                        _context.Imagens.Add(bookImage);
                    }

                    _context.SaveChanges();

                    return RedirectToAction("Index", "Libros");
                }
                catch (Exception ex)
                {
                    return View("ERROR" + ex);
                }

            }
            catch (Exception ex)
            {
                return View("ERROR" + ex);

            }

        }
        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null || _context.Libros == null)
            {
                return NotFound();
            }
            Imagen ima = new Imagen();
            var libro = _context.Libros.Include(l => l.Imagens)
                .FirstOrDefault(l => l.Id == id);
            DateTime fechaActual = DateTime.Now;
            libro.FechaActualizacion = fechaActual;
            if (libro == null)
            {
                return NotFound();
            }

            ViewData["IdImagen"] = new SelectList(_context.Imagens, "Id", "Ruta", libro.Imagens);
            ViewData["IdCategoria"] = new SelectList(_context.Categoria, "Id", "NombreCategoria", libro.IdCategoria);
            ViewData["IdTipoTapa"] = new SelectList(_context.TipoTapas, "Id", "TipoTapa1", libro.IdTipoTapa);
            ViewData["IdUsuario"] = new SelectList(_context.Usuarios, "Id", "Nombre", libro.IdUsuario);
            ViewData["IdDescripcion"] = new SelectList(_context.Descripcions, "Id", "NombreDescripcion", libro.Descripcion);
            return View(libro);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Libro model, List<string> ImagePreviews)
        {

            model.IdUsuario = UserID.GetIdUser(HttpContext);
            var libroEditado = _context.Libros.Find(id);
            if (_context.Usuarios.Any(u => u.Id == model.IdUsuario))

            {




                try
                {

                    foreach (var imagePreview in ImagePreviews)
                    {
                        // Convertir la imagen de base64 a un array de bytes
                        var base64Data = imagePreview.Substring(imagePreview.IndexOf(',') + 1);
                        var imageData = Convert.FromBase64String(base64Data);



                        // Agregar la imagen al libro
                        var bookImage = new Imagen
                        {
                            Ruta = imageData,
                            IdLibro = model.Id
                        };
                        _context.Imagens.Add(bookImage);
                    }
                    libroEditado.Titulo = model.Titulo;
                    libroEditado.Autor = model.Autor;
                    libroEditado.Editorial = model.Editorial;
                    libroEditado.Edicion = model.Edicion;

                    libroEditado.PrecioOriginal = model.PrecioOriginal;
                    libroEditado.Descripcion = model.Descripcion;
                    libroEditado.HashTags = "#" + model.Titulo;
                    libroEditado.IdCategoria = model.IdCategoria;
                    libroEditado.IdTipoTapa = model.IdTipoTapa;
                    libroEditado.IdUsuario = UserID.GetIdUser(HttpContext);

                    libroEditado.FechaActualizacion = DateTime.Now;






                    _context.Update(libroEditado);
                    await _context.SaveChangesAsync();



                    return RedirectToAction("Index", "Libros");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(model.Id))
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
            else
            {
                ModelState.AddModelError("IdUsuario", "El usuario seleccionado no existe");
                return View(model);
            }
            //ViewData["IdCategoria"] = new SelectList(_context.Categoria, "Id", "NombreCategoria", model.IdCategoria);
            //ViewData["IdTipoTapa"] = new SelectList(_context.TipoTapas, "Id", "Id", model.IdTipoTapa);
            //ViewData["IdUsuario"] = new SelectList(_context.Usuarios, "Id", "Id", model.IdUsuario);
            //ViewData["Imagen"] = new SelectList(_context.Imagens, "Id", "Ruta", model.Imagens);
            //ViewData["IdDescripcion"] = new SelectList(_context.Descripcions, "Id", "nombreDescripcion", model.IdDescripcion);
            //return RedirectToAction("Edit", "Libros", new { id = model.Id });
        }

        public ActionResult GetImage(int id)
        {
            var image = _context.Imagens.Find(id);
            if (image == null)
            {
                return NotFound();
            }

            return File(image.Ruta, "image/jpeg");
        }
        // GET: Libro/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var libro = _context.Libros
                .Include(l => l.Imagens)
                .FirstOrDefault(l => l.Id == id);
            if (libro == null)
            {
                return NotFound();
            }

            return View(libro);
          
        }
        // POST: Libro/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var libro = await _context.Libros
                .Include(l => l.Imagens)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (libro == null)
            {
                return NotFound();
            }
            _context.Libros.Remove(libro);
            if (libro.Imagens != null && libro.Imagens.Count > 0)
            {
                _context.Imagens.RemoveRange(libro.Imagens);
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));     
        }
        private bool BookExists(int id)
        {
            return (_context.Libros?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        public ActionResult Notificaciones()
        {
            var notificacionesAceptadas = _context.SolicitudesIntercambios
            .Include(x => x.IdSolicitadoNavigation)
            .Include(x => x.IdLibroSolicitandoNavigation)
            .Include(x => x.IdSolicitanteNavigation)
            .Include(x => x.IdLibroSolicitanteNavigation)
            .AsEnumerable()
            .Where(s => s.Estado == "Aceptada" && s.Notificacion == 0 && s.IdSolicitante == UserID.GetIdUser(HttpContext))

            .GroupBy(s => s.IdSolicitadoNavigation.CorreoElectronico)
            .ToList();



            var notificacionesRechazadas = _context.SolicitudesIntercambios
            .Include(x => x.IdSolicitadoNavigation)
            .Include(x => x.IdLibroSolicitandoNavigation)
            .Include(x => x.IdSolicitanteNavigation)
            .Include(x => x.IdLibroSolicitanteNavigation)
            .AsEnumerable()
            .Where(s => s.Estado == "Rechazada" && s.Notificacion == 0 && s.IdSolicitante == UserID.GetIdUser(HttpContext))



            .GroupBy(s => s.IdSolicitadoNavigation.CorreoElectronico)
            .ToList();



            return View(Tuple.Create(notificacionesAceptadas, notificacionesRechazadas));



        }
    }
}
