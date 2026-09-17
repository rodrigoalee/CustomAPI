//[INICIO][16/9/2026][jgarciad8][Servicio de autenticación de usuarios]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public sealed class AuthService(
    AppDbContext db,
    PasswordService passwordService,
    JwtService jwtService)
{

    private static readonly string HashFicticio =
        new PasswordService().CrearHash(Guid.NewGuid().ToString("N"));

    //[INICIO][16/9/2026][jgarciad8][Validación de credenciales e inicio de sesión]
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

        var passwordValida = passwordService.Verificar(
            request.Password,
            usuario?.PasswordHash ?? HashFicticio);


        if (usuario is null || !usuario.Activo || !passwordValida)
        {
            return null;
        }

        var usuarioSesion = new UsuarioSesionDto(
            usuario.IdUsuario,
            usuario.NombreCompleto,
            usuario.Correo);

        return jwtService.CrearToken(usuarioSesion);
    }
    //[FIN][16/9/2026][jgarciad8][Validación de credenciales e inicio de sesión]

    //[INICIO][16/9/2026][jgarciad8][Consulta de los datos del usuario autenticado]
    public Task<UsuarioSesionDto?> ObtenerUsuarioActualAsync(
        int idUsuario,
        CancellationToken cancellationToken)
    {
        return db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario && u.Activo)
            .Select(u => new UsuarioSesionDto(
                u.IdUsuario,
                u.NombreCompleto,
                u.Correo))
            .SingleOrDefaultAsync(cancellationToken);
    }
    //[FIN][16/9/2026][jgarciad8][Consulta de los datos del usuario autenticado]
}
//[FIN][16/9/2026][jgarciad8][Servicio de autenticación de usuarios]