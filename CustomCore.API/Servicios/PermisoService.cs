//[INICIO][17/9/2026][jgarciad8][Servicio del catálogo de permisos]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public sealed class PermisoService(
    AppDbContext db,
    IAuthorizationPolicyProvider politicas)
{
    public Task<List<PermisoDto>> ObtenerAsync(CancellationToken ct)
        => db.Permisos.AsNoTracking()
            .OrderBy(p => p.Modulo)
            .ThenBy(p => p.NombreCodigo)
            .Select(p => new PermisoDto(
                p.IdPermiso, p.NombreCodigo, p.Descripcion, p.Modulo))
            .ToListAsync(ct);

    public Task<PermisoDto?> ObtenerPorIdAsync(int id, CancellationToken ct)
        => db.Permisos.AsNoTracking()
            .Where(p => p.IdPermiso == id)
            .Select(p => new PermisoDto(
                p.IdPermiso, p.NombreCodigo, p.Descripcion, p.Modulo))
            .SingleOrDefaultAsync(ct);

    public async Task<PermisoDto> CrearAsync(
        GuardarPermisoRequest request,
        CancellationToken ct)
    {
        if (await db.Permisos.AnyAsync(
            p => p.NombreCodigo == request.NombreCodigo, ct))
        {
            throw new ConflictoNegocioException(
                "Ya existe un permiso con ese código.");
        }

        var permiso = new Permiso
        {
            NombreCodigo = request.NombreCodigo,
            Descripcion = request.Descripcion?.Trim(),
            Modulo = request.Modulo.Trim()
        };

        db.Permisos.Add(permiso);
        await db.SaveChangesAsync(ct);

        return new PermisoDto(
            permiso.IdPermiso, permiso.NombreCodigo,
            permiso.Descripcion, permiso.Modulo);
    }

    public async Task<bool> ActualizarAsync(
        int id,
        GuardarPermisoRequest request,
        CancellationToken ct)
    {
        var permiso = await db.Permisos
            .SingleOrDefaultAsync(p => p.IdPermiso == id, ct);

        if (permiso is null)
            return false;


        if (permiso.NombreCodigo != request.NombreCodigo)
        {
            throw new ConflictoNegocioException(
                "El código del permiso no puede modificarse.");
        }

        permiso.Descripcion = request.Descripcion?.Trim();
        permiso.Modulo = request.Modulo.Trim();

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken ct)
    {
        var permiso = await db.Permisos
            .SingleOrDefaultAsync(p => p.IdPermiso == id, ct);

        if (permiso is null)
            return false;

        if (await politicas.GetPolicyAsync(permiso.NombreCodigo) is not null)
        {
            throw new ConflictoNegocioException(
                "Este permiso pertenece a una política del sistema " +
                "y no puede eliminarse.");
        }

        if (await db.RolPermisos.AnyAsync(rp => rp.PermisoId == id, ct))
        {
            throw new ConflictoNegocioException(
                "Retira el permiso de sus roles antes de eliminarlo.");
        }

        db.Permisos.Remove(permiso);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
//[FIN][16/9/2026][jgarciad8][Servicio del catálogo de permisos]