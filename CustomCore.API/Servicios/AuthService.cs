//[INICIO][17/9/2026][jgarciad8][Autenticación y cambio de contraseña]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public enum ResultadoCambioPassword
{
    Exito,
    NoAutorizado,
    ActualIncorrecta,
    NuevaIgual,
    Conflicto
}

public sealed class AuthService(
    AppDbContext db,
    PasswordService passwordService,
    JwtService jwtService)
{

    private static readonly string HashFicticio =
        new PasswordService().CrearHash(Guid.NewGuid().ToString("N"));

    public async Task<LoginResponse?> IniciarSesionAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();

        var usuario = await db.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.Correo == correo,
                cancellationToken);

        //[INICIO][23/10/2026][Rodriale][El ?? HashFicticio ahora también cubre a las cuentas de Google: con hash nulo Verificar corta antes de BCrypt y el tiempo de respuesta delataría que el correo existe]
        var passwordCorrecta = passwordService.Verificar(
            request.Password,
            usuario?.PasswordHash ?? HashFicticio);

        if (usuario is null || !usuario.Activo || !passwordCorrecta)
            return null;

        return jwtService.CrearToken(
            new UsuarioSesionDto(
                usuario.IdUsuario,
                usuario.NombreCompleto,
                usuario.Correo,
                usuario.ClienteId is not null),
            usuario.VersionCredenciales);
    }

    public Task<UsuarioSesionDto?> ObtenerUsuarioActualAsync(
        int idUsuario,
        CancellationToken cancellationToken)
    {
        return db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario && u.Activo)
            .Select(u => new UsuarioSesionDto(
                u.IdUsuario, u.NombreCompleto, u.Correo, u.ClienteId != null,
                //[INICIO][25/10/2026][Rodriale][Mismo camino que PermisoHandler: usuario → roles → permisos]
                u.UsuarioRoles
                    .SelectMany(ur => ur.Rol.RolPermisos.Select(rp => rp.Permiso.NombreCodigo))
                    .Distinct()
                    .ToList()))
                //[FIN][25/10/2026][Rodriale][Mismo camino que PermisoHandler]
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ResultadoCambioPassword> CambiarPasswordAsync(
        int idUsuario,
        CambiarPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await db.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.IdUsuario == idUsuario && u.Activo,
                cancellationToken);

        if (usuario is null)
            return ResultadoCambioPassword.NoAutorizado;

        if (!passwordService.Verificar(
            request.PasswordActual, usuario.PasswordHash))
        {
            return ResultadoCambioPassword.ActualIncorrecta;
        }

        if (passwordService.Verificar(
            request.PasswordNueva, usuario.PasswordHash))
        {
            return ResultadoCambioPassword.NuevaIgual;
        }

        var hashAnterior = usuario.PasswordHash;
        var hashNuevo = passwordService.CrearHash(request.PasswordNueva);
        //[INICIO][23/10/2026][Rodriale][Precalculada: ExecuteUpdate traduce el valor a SQL y Guid.NewGuid().ToString() no es traducible]
        var versionNueva = Guid.NewGuid().ToString("N");
        //[FIN][23/10/2026][Rodriale][Precalculada]

        var filas = await db.Usuarios
            .Where(u =>
                u.IdUsuario == idUsuario
                && u.Activo
                && u.PasswordHash == hashAnterior)
            //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
            .ActualizarAuditadoAsync(db, idUsuario,
                //[INICIO][23/10/2026][Rodriale][Rotar VersionCredenciales es lo que mantiene vivo el cierre de sesiones al cambiar contraseña, ahora que cred_ver ya no sale del hash]
                cambios => cambios
                    .SetProperty(u => u.PasswordHash, hashNuevo)
                    .SetProperty(u => u.VersionCredenciales, versionNueva),
                //[FIN][23/10/2026][Rodriale][Rotar VersionCredenciales mantiene vivo el cierre de sesiones]
                cancellationToken, cambioPassword: true);
        //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

        return filas == 1
            ? ResultadoCambioPassword.Exito
            : ResultadoCambioPassword.Conflicto;
    }
}
//[FIN][17/9/2026][jgarciad8][Autenticación y cambio de contraseña]
