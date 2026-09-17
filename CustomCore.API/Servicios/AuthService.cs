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

        var passwordCorrecta = passwordService.Verificar(
            request.Password,
            usuario?.PasswordHash ?? HashFicticio);

        if (usuario is null || !usuario.Activo || !passwordCorrecta)
            return null;

        return jwtService.CrearToken(
            new UsuarioSesionDto(
                usuario.IdUsuario,
                usuario.NombreCompleto,
                usuario.Correo),
            usuario.PasswordHash);
    }

    public Task<UsuarioSesionDto?> ObtenerUsuarioActualAsync(
        int idUsuario,
        CancellationToken cancellationToken)
    {
        return db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario && u.Activo)
            .Select(u => new UsuarioSesionDto(
                u.IdUsuario, u.NombreCompleto, u.Correo))
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

        var filas = await db.Usuarios
            .Where(u =>
                u.IdUsuario == idUsuario
                && u.Activo
                && u.PasswordHash == hashAnterior)
            .ExecuteUpdateAsync(
                cambios => cambios.SetProperty(
                    u => u.PasswordHash, hashNuevo),
                cancellationToken);

        return filas == 1
            ? ResultadoCambioPassword.Exito
            : ResultadoCambioPassword.Conflicto;
    }
}
//[FIN][17/9/2026][jgarciad8][Autenticación y cambio de contraseña]