//[INICIO][23/10/2026][Rodriale][Endpoints del portal del cliente; solo piden sesión porque el rol Cliente no trae permisos]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Dtos;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/portal")]
    [Authorize]
    public sealed class PortalController(PortalService servicio) : ControllerBase
    {
        [HttpGet("citas")]
        [ProducesResponseType(typeof(IReadOnlyList<CitaPortalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<CitaPortalDto>>> ObtenerCitas(
            CancellationToken cancellationToken)
        {
            if (!TryObtenerIdUsuario(out var idUsuario))
                return Unauthorized();

            return Ok(await servicio.ObtenerCitasAsync(idUsuario, cancellationToken));
        }

        //[INICIO][24/10/2026][Rodriale][Seguimiento de una cita del cliente; 404 si no existe o es de otro]
        [HttpGet("citas/{id:int}/seguimiento")]
        [ProducesResponseType(typeof(SeguimientoCitaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SeguimientoCitaDto>> ObtenerSeguimiento(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerIdUsuario(out var idUsuario))
                return Unauthorized();

            var seguimiento = await servicio.ObtenerSeguimientoAsync(idUsuario, id, cancellationToken);
            return seguimiento is null ? NotFound() : Ok(seguimiento);
        }
        //[FIN][24/10/2026][Rodriale][Seguimiento de una cita del cliente]

        [HttpPost("citas")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AgendarCita(
            AgendarCitaRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerIdUsuario(out var idUsuario))
                return Unauthorized();

            var idCita = await servicio.AgendarCitaAsync(idUsuario, request, cancellationToken);
            return idCita is null
                ? NotFound()
                : Created("/api/portal/citas", new { idCita });
        }

        [HttpGet("perfil")]
        [ProducesResponseType(typeof(PerfilPortalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PerfilPortalDto>> ObtenerPerfil(
            CancellationToken cancellationToken)
        {
            if (!TryObtenerIdUsuario(out var idUsuario))
                return Unauthorized();

            return Ok(await servicio.ObtenerPerfilAsync(idUsuario, cancellationToken));
        }

        [HttpGet("vehiculos")]
        [ProducesResponseType(typeof(IReadOnlyList<VehiculoPortalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<VehiculoPortalDto>>> ObtenerVehiculos(
            CancellationToken cancellationToken)
        {
            if (!TryObtenerIdUsuario(out var idUsuario))
                return Unauthorized();

            return Ok(await servicio.ObtenerVehiculosAsync(idUsuario, cancellationToken));
        }

        [HttpPost("vehiculos")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RegistrarVehiculo(
            RegistrarVehiculoRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerIdUsuario(out var idUsuario))
                return Unauthorized();

            var idVehiculo = await servicio.RegistrarVehiculoAsync(idUsuario, request, cancellationToken);
            return Created("/api/portal/vehiculos", new { idVehiculo });
        }

        [HttpGet("sucursales")]
        [ProducesResponseType(typeof(IReadOnlyList<SucursalPortalDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SucursalPortalDto>>> ObtenerSucursales(
            CancellationToken cancellationToken) =>
            Ok(await servicio.ObtenerSucursalesAsync(cancellationToken));

        [HttpGet("servicios")]
        [ProducesResponseType(typeof(IReadOnlyList<ServicioPortalDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ServicioPortalDto>>> ObtenerServicios(
            CancellationToken cancellationToken) =>
            Ok(await servicio.ObtenerServiciosAsync(cancellationToken));

        [HttpGet("disponibilidad")]
        [ProducesResponseType(typeof(IReadOnlyList<DiaDisponibilidadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<DiaDisponibilidadDto>>> ObtenerDisponibilidad(
            [FromQuery] ConsultaDisponibilidad consulta,
            CancellationToken cancellationToken) =>
            Ok(await servicio.ObtenerDisponibilidadAsync(consulta, cancellationToken));

        //[INICIO][26/10/2026][Rodriale][Cotizaciones que el taller envió para una cita del cliente]
        [HttpGet("citas/{id:int}/cotizaciones")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<CotizacionDto>>> ObtenerCotizaciones(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerIdUsuario(out var idUsuario))
                return Unauthorized();

            return Ok(await servicio.ObtenerCotizacionesAsync(idUsuario, id, cancellationToken));
        }

        [HttpPost("cotizaciones/{id:int}/respuesta")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> ResponderCotizacion(
            int id,
            ResponderCotizacionRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerIdUsuario(out var idUsuario))
                return Unauthorized();

            var respondida = await servicio.ResponderCotizacionAsync(idUsuario, id, request.Aceptar!.Value, cancellationToken);
            return respondida ? NoContent() : NotFound();
        }
        //[FIN][26/10/2026][Rodriale][Cotizaciones que el taller envió]

        private bool TryObtenerIdUsuario(out int idUsuario) =>
            int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out idUsuario)
            && idUsuario > 0;
    }
}
//[FIN][23/10/2026][Rodriale][Endpoints del portal del cliente]
