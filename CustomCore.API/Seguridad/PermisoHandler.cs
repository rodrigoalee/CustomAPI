//[INICIO][16/9/2026][jgarciad8][Verificación de permisos mediante los roles del usuario]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Seguridad;

public sealed class PermisoHandler(AppDbContext db)
    : AuthorizationHandler<PermisoRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext contexto,
        PermisoRequirement requisito)
    {
      
        if (contexto.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var identificador = contexto.User
            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!int.TryParse(identificador, out var idUsuario)
            || idUsuario <= 0)
        {
            return;
        }

        var cancellationToken = contexto.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;


        var tienePermiso = await db.UsuarioRoles
            .AsNoTracking()
            .AnyAsync(
                usuarioRol =>
                    usuarioRol.UsuarioId == idUsuario
                    && usuarioRol.Usuario.Activo
                    && usuarioRol.Rol.RolPermisos.Any(
                        rolPermiso =>
                            rolPermiso.Permiso.NombreCodigo
                                == requisito.Codigo),
                cancellationToken);

        if (tienePermiso)
        {
            contexto.Succeed(requisito);
        }

    }
}
//[FIN][16/9/2026][jgarciad8][Verificación de permisos mediante los roles del usuario]