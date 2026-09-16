//[INICIO][16/9/2026][jgarciad8][Servicio de consulta de usuarios]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using Microsoft.EntityFrameworkCore;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;

namespace CustomCore.API.Servicios;

//[INICIO][16/9/2026][jgarciad8][Dependencias del servicio de usuarios]
public sealed class UsuarioService(
    AppDbContext db,
    PasswordService passwordService)
//[FIN][16/9/2026][jgarciad8][Dependencias del servicio de usuarios]

{
    //[INICIO][16/9/2026][jgarciad8][Consulta paginada de usuarios con búsqueda]
    public async Task<ResultadoPaginado<UsuarioResumenDto>> ObtenerAsync(
        ConsultaPaginada consulta,
        CancellationToken cancellationToken)
    {
        var query = db.Usuarios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
        {
            var patron = $"%{consulta.Busqueda.Trim()}%";

            query = query.Where(usuario =>
                EF.Functions.ILike(usuario.NombreCompleto, patron)
                || EF.Functions.ILike(usuario.Correo, patron));
        }

        var total = await query.CountAsync(cancellationToken);

      
        var desplazamiento =
            ((long)consulta.Pagina - 1) * consulta.Tamanio;

        if (desplazamiento >= total)
        {
            return new ResultadoPaginado<UsuarioResumenDto>(
                Array.Empty<UsuarioResumenDto>(),
                consulta.Pagina,
                consulta.Tamanio,
                total);
        }

        var items = await query
            .OrderBy(usuario => usuario.NombreCompleto)
            .ThenBy(usuario => usuario.IdUsuario)
            .Skip((int)desplazamiento)
            .Take(consulta.Tamanio)
            .Select(usuario => new UsuarioResumenDto(
                usuario.IdUsuario,
                usuario.NombreCompleto,
                usuario.Correo,
                usuario.Activo))
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<UsuarioResumenDto>(
            items,
            consulta.Pagina,
            consulta.Tamanio,
            total);
    }
    //[FIN][16/9/2026][jgarciad8][Consulta paginada de usuarios con búsqueda]

    //[INICIO][16/9/2026][jgarciad8][Consulta individual sin exponer la contraseña]
    public Task<UsuarioResumenDto?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return db.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.IdUsuario == id)
            .Select(usuario => new UsuarioResumenDto(
                usuario.IdUsuario,
                usuario.NombreCompleto,
                usuario.Correo,
                usuario.Activo))
            .SingleOrDefaultAsync(cancellationToken);
    }
    //[FIN][16/9/2026][jgarciad8][Consulta individual sin exponer la contraseña]

    //[INICIO][16/9/2026][jgarciad8][Creación de usuarios con contraseña BCrypt]
    public async Task<UsuarioResumenDto> CrearAsync(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        
        var correoNormalizado = request.Correo.Trim().ToLowerInvariant();


        var correoExiste = await db.Usuarios
            .AsNoTracking()
            .AnyAsync(
                usuario => usuario.Correo.ToLower() == correoNormalizado,
                cancellationToken);

        if (correoExiste)
        {
            throw new ConflictoNegocioException(
                "Ya existe un usuario registrado con ese correo.");
        }

        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            Correo = correoNormalizado,
            PasswordHash = passwordService.CrearHash(request.Password),
            Activo = true
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);

     
        return new UsuarioResumenDto(
            usuario.IdUsuario,
            usuario.NombreCompleto,
            usuario.Correo,
            usuario.Activo);
    }
    //[FIN][16/9/2026][jgarciad8][Creación de usuarios con contraseña BCrypt]


}
//[FIN][16/9/2026][jgarciad8][Servicio de consulta de usuarios]