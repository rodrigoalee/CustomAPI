//[INICIO][27/10/2026][Rodriale][Descarga de reportes en Excel o PDF]
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    [Authorize(Policy = PermisosSistema.ReportesVer)]
    public sealed class ReportesController(ReporteService servicio) : ControllerBase
    {
        [HttpGet("{tipo}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Descargar(
            TipoReporte tipo,
            [FromQuery] ConsultaReporte consulta,
            CancellationToken cancellationToken)
        {
            var archivo = await servicio.GenerarAsync(tipo, consulta, cancellationToken);
            return File(archivo.Contenido, archivo.TipoContenido, archivo.Nombre);
        }
    }
}
//[FIN][27/10/2026][Rodriale][Descarga de reportes en Excel o PDF]
