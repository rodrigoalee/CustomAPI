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

    //[INICIO][16/9/2026][jgarciad8][Actualización de nombre y correo del usuario]
    public async Task<bool> ActualizarAsync(
        int id,
        ActualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        // Se conserva el seguimiento de EF Core para guardar los cambios.
        var usuario = await db.Usuarios
            .SingleOrDefaultAsync(
                usuario => usuario.IdUsuario == id,
                cancellationToken);

        if (usuario is null)
        {
            return false;
        }

        var correoNormalizado = request.Correo.Trim().ToLowerInvariant();

        // El correo puede conservarse, pero no pertenecer a otro usuario.
        var correoExiste = await db.Usuarios
            .AsNoTracking()
            .AnyAsync(
                otroUsuario =>
                    otroUsuario.IdUsuario != id
                    && otroUsuario.Correo.ToLower() == correoNormalizado,
                cancellationToken);

        if (correoExiste)
        {
            throw new ConflictoNegocioException(
                "Ya existe otro usuario registrado con ese correo.");
        }

        usuario.NombreCompleto = request.NombreCompleto.Trim();
        usuario.Correo = correoNormalizado;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
    //[FIN][16/9/2026][jgarciad8][Actualización de nombre y correo del usuario]

    //[INICIO][16/9/2026][jgarciad8][Activación y desactivación de cuentas]
    public async Task<bool> CambiarEstadoAsync(
        int id,
        bool activo,
        int usuarioActualId,
        CancellationToken cancellationToken)
    {
        // Evita que quien administra usuarios desactive su propia cuenta.
        if (id == usuarioActualId && !activo)
        {
            throw new ConflictoNegocioException(
                "No puedes desactivar tu propia cuenta.");
        }

        var usuario = await db.Usuarios
            .SingleOrDefaultAsync(
                usuario => usuario.IdUsuario == id,
                cancellationToken);

        if (usuario is null)
        {
            return false;
        }

        // Solicitar nuevamente el mismo estado no necesita otra escritura.
        if (usuario.Activo == activo)
        {
            return true;
        }

        usuario.Activo = activo;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
    //[FIN][16/9/2026][jgarciad8][Activación y desactivación de cuentas]

}
//[FIN][16/9/2026][jgarciad8][Servicio de consulta de usuarios]