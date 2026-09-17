//[INICIO][16/9/2026][jgarciad8][Servicio de roles y asignaciones]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public sealed class RolService(AppDbContext db)
{
    
    public Task<List<RolDto>> ObtenerAsync(CancellationToken ct)
    {
        return db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .ThenBy(r => r.IdRol)
            .Select(r => new RolDto(r.IdRol, r.Nombre, r.Descripcion))
            .ToListAsync(ct);
    }

    public Task<RolDto?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        return db.Roles
            .AsNoTracking()
            .Where(r => r.IdRol == id)
            .Select(r => new RolDto(r.IdRol, r.Nombre, r.Descripcion))
            .SingleOrDefaultAsync(ct);
    }

    
    private async Task ValidarNombreAsync(
        string nombre,
        int? idExcluir,
        CancellationToken ct)
    {
        var normalizado = nombre.Trim().ToLowerInvariant();

        var existe = await db.Roles.AnyAsync(
            r => r.Nombre.ToLower() == normalizado
                && (!idExcluir.HasValue || r.IdRol != idExcluir.Value),
            ct);

        if (existe)
        {
            throw new ConflictoNegocioException(
                "Ya existe otro rol con ese nombre.");
        }
    }

    public async Task<RolDto> CrearAsync(
        GuardarRolRequest request,
        CancellationToken ct)
    {
        await ValidarNombreAsync(request.Nombre, null, ct);

        var rol = new Rol
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim()
        };

        db.Roles.Add(rol);
        await db.SaveChangesAsync(ct);

        return new RolDto(rol.IdRol, rol.Nombre, rol.Descripcion);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        GuardarRolRequest request,
        CancellationToken ct)
    {
        var rol = await db.Roles
            .SingleOrDefaultAsync(r => r.IdRol == id, ct);

        if (rol is null)
            return false;

        await ValidarNombreAsync(request.Nombre, id, ct);

        rol.Nombre = request.Nombre.Trim();
        rol.Descripcion = request.Descripcion?.Trim();

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken ct)
    {
        
        var rol = await db.Roles
            .Include(r => r.RolPermisos)
            .SingleOrDefaultAsync(r => r.IdRol == id, ct);

        if (rol is null)
            return false;

        if (await db.UsuarioRoles.AnyAsync(r => r.RolId == id, ct))
        {
            throw new ConflictoNegocioException(
                "No puedes eliminar un rol asignado a usuarios. " +
                "Retira primero sus asignaciones.");
        }

        db.Roles.Remove(rol);
        await db.SaveChangesAsync(ct);
        return true;
    }


    public async Task<bool> CambiarAsignacionAsync(
        int rolId,
        int usuarioId,
        int usuarioActualId,
        bool asignar,
        CancellationToken ct)
    {
        if (usuarioId == usuarioActualId)
        {
            throw new ConflictoNegocioException(
                "No puedes modificar tus propias asignaciones de roles.");
        }

        var existeRol = await db.Roles
            .AnyAsync(r => r.IdRol == rolId, ct);

        var existeUsuario = await db.Usuarios
            .AnyAsync(u => u.IdUsuario == usuarioId, ct);

        if (!existeRol || !existeUsuario)
            return false;

        if (asignar)
        {

            var excedePermisos = await db.RolPermisos
                .Where(rp => rp.RolId == rolId)
                .AnyAsync(
                    rp => !db.UsuarioRoles.Any(
                        ur => ur.UsuarioId == usuarioActualId
                            && ur.Usuario.Activo
                            && ur.Rol.RolPermisos.Any(
                                propio => propio.PermisoId == rp.PermisoId)),
                    ct);

            if (excedePermisos)
            {
                throw new ConflictoNegocioException(
                    "No puedes asignar un rol con permisos que no posees.");
            }
        }

        var relacion = await db.UsuarioRoles.SingleOrDefaultAsync(
            ur => ur.UsuarioId == usuarioId && ur.RolId == rolId,
            ct);

        if (asignar && relacion is null)
        {
            db.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = usuarioId,
                RolId = rolId
            });
        }
        else if (!asignar && relacion is not null)
        {
            db.UsuarioRoles.Remove(relacion);
        }
        else
        {

            return true;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
//[FIN][16/9/2026][jgarciad8][Servicio de roles y asignaciones]