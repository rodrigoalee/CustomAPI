//[INICIO][12/10/2026][Rodriale][Sede y alcance de quien llama, resueltos una sola vez por petición]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Data;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Seguridad;

public sealed class SucursalActual(AppDbContext db, IHttpContextAccessor http)
{
    private sealed record Perfil(int IdUsuario, int? SucursalId, bool Todas, bool SoloSusOrdenes);

    private Perfil? _perfil;

    //[INICIO][25/10/2026][Rodriale][El mecánico que no abre órdenes solo ve las suyas: devuelve su id para filtrar, null si ve todas]
    public async Task<int?> MecanicoRestringidoAsync(CancellationToken cancellationToken)
    {
        var perfil = await ObtenerPerfilAsync(cancellationToken);
        return perfil.SoloSusOrdenes ? perfil.IdUsuario : null;
    }

    public async Task<int> UsuarioIdAsync(CancellationToken cancellationToken) =>
        (await ObtenerPerfilAsync(cancellationToken)).IdUsuario;
    //[FIN][25/10/2026][Rodriale][El mecánico que no abre órdenes solo ve las suyas]

    //[INICIO][12/10/2026][Rodriale][Para lecturas: null significa sin filtro; sin sede y sin permiso falla cerrado]
    public async Task<int?> AlcanceAsync(CancellationToken cancellationToken)
    {
        var perfil = await ObtenerPerfilAsync(cancellationToken);

        if (perfil.Todas)
            return null;

        return perfil.SucursalId
            ?? throw new AccesoDenegadoException("Tu usuario no tiene una sucursal asignada.");
    }
    //[FIN][12/10/2026][Rodriale][Para lecturas]

    //[INICIO][12/10/2026][Rodriale][Para altas: el empleado opera en su sede; quien ve todas debe decir cuál]
    public async Task<int> ResolverAsync(int? solicitada, CancellationToken cancellationToken)
    {
        var perfil = await ObtenerPerfilAsync(cancellationToken);
        int sucursalId;

        if (perfil.Todas)
        {
            sucursalId = solicitada
                ?? throw new SolicitudInvalidaException("Indica la sucursal (sucursalId).");
        }
        else
        {
            sucursalId = perfil.SucursalId
                ?? throw new AccesoDenegadoException("Tu usuario no tiene una sucursal asignada.");

            if (solicitada is { } pedida && pedida != sucursalId)
                throw new AccesoDenegadoException("Solo puedes operar en tu sucursal.");
        }

        var activa = await db.Sucursales
            .AsNoTracking()
            .AnyAsync(s => s.IdSucursal == sucursalId && s.Activo, cancellationToken);

        if (!activa)
            throw new ConflictoNegocioException("La sucursal no existe o está dada de baja.");

        return sucursalId;
    }
    //[FIN][12/10/2026][Rodriale][Para altas]

    //[INICIO][12/10/2026][Rodriale][Una sola consulta por petición; se lee en la base y no del token para que reasignar sede surta efecto de inmediato]
    private async Task<Perfil> ObtenerPerfilAsync(CancellationToken cancellationToken)
    {
        if (_perfil is not null)
            return _perfil;

        if (!int.TryParse(http.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var idUsuario))
            throw new AccesoDenegadoException("La sesión no identifica a un usuario.");

        _perfil = await db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario)
            .Select(u => new Perfil(
                u.IdUsuario,
                u.SucursalId,
                u.UsuarioRoles.Any(ur => ur.Rol.RolPermisos.Any(
                    rp => rp.Permiso.NombreCodigo == PermisosSistema.SucursalesTodas)),
                //[INICIO][25/10/2026][Rodriale][Mecánico sin permiso de abrir órdenes]
                u.UsuarioRoles.Any(ur => ur.Rol.RolPermisos.Any(
                    rp => rp.Permiso.NombreCodigo == PermisosSistema.OrdenesTrabajar))
                && !u.UsuarioRoles.Any(ur => ur.Rol.RolPermisos.Any(
                    rp => rp.Permiso.NombreCodigo == PermisosSistema.OrdenesCrear))))
                //[FIN][25/10/2026][Rodriale][Mecánico sin permiso de abrir órdenes]
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AccesoDenegadoException("La sesión no identifica a un usuario.");

        return _perfil;
    }
    //[FIN][12/10/2026][Rodriale][Una sola consulta por petición]
}
//[FIN][12/10/2026][Rodriale][Sede y alcance de quien llama]
