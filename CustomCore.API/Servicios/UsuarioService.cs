//[INICIO][16/9/2026][jgarciad8][Servicio de consulta de usuarios]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public sealed class UsuarioService(AppDbContext db)
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
}
//[FIN][16/9/2026][jgarciad8][Servicio de consulta de usuarios]