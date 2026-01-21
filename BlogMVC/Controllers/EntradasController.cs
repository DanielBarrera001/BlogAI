using BlogMVC.Datos;
using BlogMVC.Entidades;
using BlogMVC.Models;
using BlogMVC.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Controllers
{
    public class EntradasController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly string contenedor = "entradas";

        public EntradasController(ApplicationDbContext context,
            IAlmacenadorArchivos almacenadorArchivos,
            IServicioUsuarios servicioUsuarios)
        {
            this.context = context;
            this.almacenadorArchivos = almacenadorArchivos;
            this.servicioUsuarios = servicioUsuarios;
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var entrada = await context.Entradas
                .Include(x => x.UsuarioCreacion)
                .Include(x => x.Comentarios)
                .ThenInclude(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id);

            if(entrada is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = new EntradaDetalleViewModel
            {
                Id = entrada.Id,
                Titulo = entrada.Titulo,
                Cuerpo = entrada.Cuerpo,
                PortadaUrl = entrada.PortadaUrl,
                FechaPublicacion = entrada.FechaPublicacion,
                EscritoPor = entrada.UsuarioCreacion!.Nombre
            };

            return View(modelo);
        }

        [HttpPost]
        [Authorize(Roles =$"{Constantes.RolAdmin}, {Constantes.CRUDEntradas}")]
        public async Task<IActionResult> Crear(EntradaCrearViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string? portadaUrl = null;

            if(modelo.ImagenPortada is not null)
            {
                portadaUrl = await almacenadorArchivos.Almacenar(contenedor, modelo.ImagenPortada);
            }

            string usuarioId = servicioUsuarios.ObtenerUsuarioId()!;

            var entrada = new Entrada
            {
                Titulo = modelo.Titulo,
                Cuerpo = modelo.Cuerpo,
                FechaPublicacion = DateTime.UtcNow,
                PortadaUrl = portadaUrl,
                UsuarioCreacionId = usuarioId
            };

            context.Add(entrada);
            await context.SaveChangesAsync();

            return RedirectToAction("Detalle", new {id = entrada.Id});
        }
    }
}
