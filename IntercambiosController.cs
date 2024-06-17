using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PR_BIBLIOTRUEQUE.Data;
using PR_BIBLIOTRUEQUE.Models;

namespace PR_BIBLIOTRUEQUE.Controllers
{
    public class IntercambiosController : Controller
    {
        private readonly DBprbibliotruequeContext _context;

        public IntercambiosController(DBprbibliotruequeContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            ViewData["MensajeId"] = "" + UserID.GetIdUser(HttpContext);
            var libros = await _context.Libros.Include(x=>x.Imagens).Include(x => x.IdUsuarioNavigation).Where(x => x.IdUsuario != UserID.GetIdUser(HttpContext)).Where(l=>l.Estado == 1).ToListAsync();
            return View(libros);
        }

        [HttpGet]
        public async Task<IActionResult> EnviarSolicitud(int id, int idUser)
        {
            Libro l = new Libro();
            ViewData["MensajeId"] = "" + UserID.GetIdUser(HttpContext);

            ViewData["IdLibro"] = new SelectList(_context.Libros.Where(x => x.Id == id), "Id", "Titulo");
            
            var libro = await _context.Libros.Include(x => x.IdUsuarioNavigation).FirstOrDefaultAsync(x => x.Id == id);

            var libros = await _context.Libros.Where(x => x.IdUsuario == UserID.GetIdUser(HttpContext) && x.Estado == 1).ToListAsync();

            // Agrega una propiedad adicional a tu modelo que contenga una lista de los libros que ya han sido ofrecidos al usuario especificado
            var librosYaOfrecidos = libros.Where(libro => libro.YaOfrecidoA(idUser, _context)).ToList();

            return View(Tuple.Create(libro, libros, librosYaOfrecidos));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarSolicitud()
        {
            var form = Request.Form;
            var idSolicitante = int.Parse(form["yo"]);
            var idSolicitado = int.Parse(form["el"]);
            var idLibroSolicitado = int.Parse(form["sulibro"]);

            var libros = _context.Libros.Where(x => x.IdUsuario == UserID.GetIdUser(HttpContext)).AsNoTracking();

            int? maxId = _context.SolicitudesIntercambios.Max(s => (int?)s.IdTemp);
            int nextId = (maxId ?? 0) + 1;

            foreach (var libro in libros)
            {
                var isChecked = form["hola " + libro.Id] == "true";
                Console.WriteLine("isChecked para libro " + libro.Id + ": " + isChecked.ToString());

                if (isChecked)
                {
                    var solicitud = new SolicitudesIntercambio
                    {
                        IdTemp = nextId,
                        IdSolicitante = idSolicitante,
                        IdSolicitado = idSolicitado,
                        IdLibroSolicitante = libro.Id,
                        IdLibroSolicitando = idLibroSolicitado,
                        Estado = "Pendiente"
                    };

                    Console.WriteLine("Estos son los datos que se solicitarán " + solicitud.IdSolicitante + " " + solicitud.IdSolicitado + " "
                        + solicitud.IdLibroSolicitante + " " + solicitud.IdLibroSolicitando);

                    _context.Add(solicitud);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        public ActionResult SolicitudesPendientes()
        {
            var solicitudes = _context.SolicitudesIntercambios
            .Include(s => s.IdSolicitanteNavigation)
            .Include(s => s.IdSolicitadoNavigation)
            .Include(s => s.IdLibroSolicitanteNavigation)
            .Include(s => s.IdLibroSolicitandoNavigation)
            .Where(x => x.IdSolicitado == UserID.GetIdUser(HttpContext))
            .ToList();

            var solicitudes2 = _context.SolicitudesIntercambios
            .Include(s => s.IdSolicitanteNavigation)
            .Include(s => s.IdSolicitadoNavigation)
            .Include(s => s.IdLibroSolicitanteNavigation)
            .Include(s => s.IdLibroSolicitandoNavigation)
            .Where(x => x.IdSolicitante == UserID.GetIdUser(HttpContext))
            .ToList();

            //return View(solicitudes);
            return View(Tuple.Create(solicitudes, solicitudes2));
        }

        public List<int> ObtenerIdsOtrosUsuarios(int miIdUsuario)
        {
            // Obtener todas las solicitudes donde el IdSolicitante o IdSolicitado sea igual a miIdUsuario y el Estado sea "Pendiente"
            var solicitudes = _context.SolicitudesIntercambios
                .Where(s => (s.IdSolicitante == miIdUsuario || s.IdSolicitado == miIdUsuario) && s.Estado == "Pendiente")
                .ToList();

            // Crear una lista para almacenar los IDs de los otros usuarios
            List<int> idsOtrosUsuarios = new List<int>();

            // Recorrer todas las solicitudes pendientes
            foreach (var solicitud in solicitudes)
            {
                // Si miIdUsuario es igual al IdSolicitante, agregar el IdSolicitado a la lista
                if (solicitud.IdSolicitante == miIdUsuario)
                    idsOtrosUsuarios.Add(solicitud.IdSolicitado);
                else // Si no, agregar el IdSolicitante a la lista
                    idsOtrosUsuarios.Add(solicitud.IdSolicitante);
            }

            // Devolver la lista de IDs de los otros usuarios
            return idsOtrosUsuarios;
        }

        public void CheckExpiredRequests(int miIdUsuario, int idOtroUsuario)
        {
            // Obtener todas las solicitudes donde el IdSolicitante o IdSolicitado sea igual a miIdUsuario o idOtroUsuario y
            // el Estado sea "Pendiente"
            var solicitudes = _context.SolicitudesIntercambios.Include(x => x.IdSolicitadoNavigation)
                .Include(x => x.IdSolicitanteNavigation)
                .Where(s => (s.IdSolicitante == miIdUsuario || s.IdSolicitado == miIdUsuario || s.IdSolicitante == idOtroUsuario
                || s.IdSolicitado == idOtroUsuario) && s.Estado == "Pendiente")
                .ToList();
            // Recorrer todas las solicitudes pendientes
            foreach (var solicitud in solicitudes)
            {

                // Verificar si la solicitud ha expirado
                if (solicitud.FechaActualizacion != null)
                {
                    if (DateTime.Now - solicitud.FechaActualizacion > TimeSpan.FromMinutes(3))
                    {
                        
                        // Tomar las acciones necesarias en caso de que la solicitud haya expirado
                        // Por ejemplo, cambiar el Estado de la solicitud a "Expirada"
                        solicitud.Estado = "Expirada";
                        solicitud.FechaActualizacion = DateTime.Now;
                        solicitud.Notificacion = 0;
                        solicitud.IdSolicitadoNavigation.Puntaje -= 5;
                        Console.WriteLine("El usuario " + solicitud.IdSolicitadoNavigation.CorreoElectronico +
                            " No Respondio a la contraoferta a " + solicitud.IdSolicitanteNavigation.CorreoElectronico);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    if (DateTime.Now - solicitud.FechaRegistro> TimeSpan.FromMinutes(3))
                    {
                        // Tomar las acciones necesarias en caso de que la solicitud haya expirado
                        // Por ejemplo, cambiar el Estado de la solicitud a "Expirada"
                        solicitud.Estado = "Expirada";
                        solicitud.FechaActualizacion = DateTime.Now;
                        solicitud.Notificacion = 0;
                        solicitud.IdSolicitadoNavigation.Puntaje -= 5;
                        Console.WriteLine("El usaurio " + solicitud.IdSolicitadoNavigation.CorreoElectronico +
                           " No Respondio a la solicitud a " + solicitud.IdSolicitanteNavigation.CorreoElectronico);
                        _context.SaveChanges();
                        _context.SaveChanges();
                    }
                }


            }
        }

        [HttpPost]
        public ActionResult AceptarSolicitud(int id)
        {

            var solicitud = _context.SolicitudesIntercambios.Where(x => x.IdTemp == id)
                .Include(x => x.IdLibroSolicitandoNavigation)
                .Include(x => x.IdSolicitadoNavigation)
                .Include(x => x.IdSolicitanteNavigation)
                .Include(x => x.IdLibroSolicitanteNavigation).ToList();
            if (solicitud == null)
            {
                return NotFound();
            }

            foreach (var item in solicitud)
            {
                item.Estado = "Aceptada";
                item.FechaActualizacion = DateTime.Now;
                item.Notificacion = 0;
                item.IdLibroSolicitanteNavigation.Estado = 0;
                item.IdLibroSolicitandoNavigation.Estado = 0;
                //New
                item.IdSolicitanteNavigation.Puntaje += 5;
                item.IdSolicitadoNavigation.Puntaje += 5;
                //New End
                Console.WriteLine(item.Id + " " + item.IdSolicitante + " " + item.IdSolicitado +
                    " " + item.IdLibroSolicitante + " " + item.IdLibroSolicitando);

            }

            _context.SaveChanges();


            return RedirectToAction("SolicitudesPendientes");
        }

        public int ObtenerIdOtroUsuario(string correoUsuario2)
        {
            // Buscar el ID del otro usuario en la base de datos a partir de su correo electrónico
            int idUsuario2 = _context.Usuarios
                .Where(u => u.CorreoElectronico == correoUsuario2)
                .Select(u => u.Id)
                .FirstOrDefault();
            return idUsuario2;
        }

        // Acción para rechazar una solicitud pendiente
        [HttpPost]
        public ActionResult RechazarSolicitud(int id)
        {
            var solicitud = _context.SolicitudesIntercambios.Where(x => x.IdTemp == id).ToList();
            if (solicitud == null)
            {
                return NotFound();
            }

            foreach (var item in solicitud)
            {
                item.Estado = "Rechazada";
                item.Notificacion = 0;
                item.FechaActualizacion = DateTime.Now;
            }

            _context.SaveChanges();

            return RedirectToAction("SolicitudesPendientes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contraofertaver(int id)
        {
            ViewData["MensajeId"] = "" + UserID.GetIdUser(HttpContext);

            var solicitud = _context.SolicitudesIntercambios.FirstOrDefault(s => s.IdTemp == id);

            var solicitudes = _context.SolicitudesIntercambios
                .Include(x => x.IdSolicitadoNavigation)
                .Include(x => x.IdLibroSolicitandoNavigation)
                .Include(x => x.IdSolicitanteNavigation)
                .Include(x => x.IdLibroSolicitanteNavigation)
                .Where(s => s.IdSolicitado == UserID.GetIdUser(HttpContext) && s.Estado == "Pendiente" && s.IdTemp == id)
                .ToList();

            var librosUnicos = solicitudes.GroupBy(s => s.IdLibroSolicitanteNavigation.Titulo)
                .Select(g => g.First().IdLibroSolicitanteNavigation)
                .ToList();

            foreach (var libro in librosUnicos)
            {
                ViewData["Autor"] = libro.IdUsuarioNavigation.Nombre;
                ViewData["MensajeId2"] = libro.IdUsuarioNavigation.Id;
                Console.WriteLine(libro.Titulo);
            }

            var mislibros = _context.Libros.Where(x => x.IdUsuario == UserID.GetIdUser(HttpContext) && x.Estado == 1).ToList();

            // Obtener la lista de IDs de libros solicitados
            var librosSolicitados = solicitudes.Select(s => s.IdLibroSolicitando).ToList();
            foreach (var item in mislibros)
            {
                Console.WriteLine("Mis libros : " + item.Titulo);
            }

            // Agregar la lista de libros solicitados como tercer elemento de la tupla
            return View(Tuple.Create(solicitudes, mislibros, librosSolicitados));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contraoferta()
        {

            var form = Request.Form;
            int idSolicitud = int.Parse(form["idSolicitud"]);

            var solicitudes = _context.SolicitudesIntercambios.Where(s => s.IdSolicitante == int.Parse(form["el"])
            && s.IdSolicitado == int.Parse(form["yo"]) && s.Estado == "Pendiente" && s.IdTemp == idSolicitud);

            int? maxId = _context.SolicitudesIntercambios.Max(s => (int?)s.IdTemp);
            int nextId = (maxId ?? 0) + 1;

            foreach (var solicitud in solicitudes)
            {
                solicitud.Estado = "Contraoferta";
                solicitud.FechaActualizacion = DateTime.Now;

                Console.WriteLine("id: " + solicitud.Id + " " + solicitud.IdSolicitante + " "
                            + solicitud.IdSolicitado + " " + solicitud.IdLibroSolicitante + " " +
                            solicitud.IdLibroSolicitando);
            }
            _context.SaveChanges();

            var librosEl = Request.Form.Keys
                .Where(key => key.StartsWith("librosEl"))
                .Select(key => new { Id = int.Parse(key.Split(' ')[1]), Value = bool.Parse(Request.Form[key]) });
            var librosMios = Request.Form.Keys
                .Where(key => key.StartsWith("librosMios"))
                .Select(key => new { Id = int.Parse(key.Split(' ')[1]), Value = bool.Parse(Request.Form[key]) });
            // Recorrer los libros seleccionados y crear nuevos registros en la base de datos
            foreach (var libroEl in librosEl)
            {
                foreach (var libroMio in librosMios)
                {
                    var intercambio = new SolicitudesIntercambio
                    {
                        IdTemp = nextId,
                        IdSolicitante = int.Parse(form["yo"]),
                        IdSolicitado = int.Parse(form["el"]),
                        IdLibroSolicitante = libroMio.Id,
                        IdLibroSolicitando = libroEl.Id,
                        Estado = "Pendiente",
                        FechaActualizacion = DateTime.Now
                    };
                    _context.SolicitudesIntercambios.Add(intercambio);

                    Console.WriteLine("ESta linea deberia ir a la base " + intercambio.IdSolicitante + " " + intercambio.IdSolicitado + " "
                        + intercambio.IdLibroSolicitante + " " + intercambio.IdLibroSolicitando);
                }
            }
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

    }
}
