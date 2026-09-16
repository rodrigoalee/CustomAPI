//[INICIO][16/9/2026][jgarciad8][Controlador del módulo de usuarios]
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsuariosController(UsuarioService servicio)
    : ControllerBase
{
    //[INICIO][16/9/2026][jgarciad8][Consulta de usuarios protegida por permiso]
    [HttpGet]
    [Authorize(Policy = PermisosSistema.UsuariosLeer)]
    [ProducesResponseType(
        typeof(ResultadoPaginado<UsuarioResumenDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<ResultadoPaginado<UsuarioResumenDto>> Obtener(
        [FromQuery] ConsultaPaginada consulta,
        CancellationToken cancellationToken)
    {
        return servicio.ObtenerAsync(consulta, cancellationToken);
    }
    //[FIN][16/9/2026][jgarciad8][Consulta de usuarios protegida por permiso]
}
//[FIN][16/9/2026][jgarciad8][Controlador del módulo de usuarios]