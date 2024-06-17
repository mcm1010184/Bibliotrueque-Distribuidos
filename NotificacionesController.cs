using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR_BIBLIOTRUEQUE.Data;
using PR_BIBLIOTRUEQUE.Models;

namespace PR_BIBLIOTRUEQUE.Controllers
{
    public class NotificacionesController : Controller
    {

        private readonly DBprbibliotruequeContext _context;

        public NotificacionesController(DBprbibliotruequeContext context)
        {
            _context = context;

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


            var notificacionesNoRespondidas = _context.SolicitudesIntercambios
                             .Include(x => x.IdSolicitadoNavigation)
                             .Include(x => x.IdLibroSolicitandoNavigation)
                             .Include(x => x.IdSolicitanteNavigation)
                             .Include(x => x.IdLibroSolicitanteNavigation)
                             .AsEnumerable()
                             .Where(s => s.Estado == "Expirada" && s.Notificacion == 0 && s.IdSolicitante == UserID.GetIdUser(HttpContext))
                             .GroupBy(s => s.IdSolicitadoNavigation.CorreoElectronico)
                             .ToList();
            return View(Tuple.Create(notificacionesAceptadas, notificacionesRechazadas, notificacionesNoRespondidas));



        }
    }
}
