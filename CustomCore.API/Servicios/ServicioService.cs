using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class ServicioService(AppDbContext db)
    {
        public async Task<ResultadoPaginado<ServicioDto>> ObtenerAsync(
            ConsultaCatalogo consulta,
            CancellationToken cancellationToken)
        {
            var query = db.Servicios.AsNoTracking();

            if (consulta.SoloActivos)
                query = query.Where(s => s.Activo);

            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(s => EF.Functions.ILike(s.Nombre, patron));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(s => s.Nombre)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(s => new ServicioDto(
                    s.IdServicio,
                    s.Nombre,
                    s.TarifaManoObra,
                    s.Activo))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<ServicioDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }

        public Task<ServicioDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
            db.Servicios
                .AsNoTracking()
                .Where(s => s.IdServicio == id)
                .Select(s => new ServicioDto(
                    s.IdServicio,
                    s.Nombre,
                    s.TarifaManoObra,
                    s.Activo))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<int> CrearAsync(CrearServicioRequest request, CancellationToken cancellationToken)
        {
            var servicio = new Servicio
            {
                Nombre = request.Nombre,
                TarifaManoObra = request.TarifaManoObra
            };

            db.Servicios.Add(servicio);
            await db.SaveChangesAsync(cancellationToken);

            return servicio.IdServicio;
        }

        public async Task<bool> ActualizarAsync(
            int id,
            ActualizarServicioRequest request,
            CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Servicios
                .Where(s => s.IdServicio == id)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, p => p
                    .SetProperty(s => s.Nombre, request.Nombre)
                    .SetProperty(s => s.TarifaManoObra, request.TarifaManoObra)
                    .SetProperty(s => s.Activo, request.Activo),
                    cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        }

        //[INICIO][31/8/2026][Rodriale][Baja lógica: el servicio queda referenciado en órdenes]
        public async Task<bool> DarDeBajaAsync(int id, CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Servicios
                .Where(s => s.IdServicio == id)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, p => p.SetProperty(s => s.Activo, false), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        }
        //[FIN][31/8/2026][Rodriale][Baja lógica: el servicio queda referenciado en órdenes]
    }
}
