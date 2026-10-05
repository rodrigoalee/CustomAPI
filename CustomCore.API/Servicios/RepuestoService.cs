using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class RepuestoService(AppDbContext db)
    {
        public async Task<ResultadoPaginado<RepuestoDto>> ObtenerAsync(
            ConsultaCatalogo consulta,
            CancellationToken cancellationToken)
        {
            var query = db.Repuestos.AsNoTracking();

            if (consulta.SoloActivos)
                query = query.Where(r => r.Activo);

            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(r =>
                    EF.Functions.ILike(r.CodigoStock, patron) ||
                    EF.Functions.ILike(r.Nombre, patron));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(r => r.Nombre)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(r => new RepuestoDto(
                    r.IdRepuesto,
                    r.CodigoStock,
                    r.Nombre,
                    r.PrecioUnitario,
                    r.CantidadStock,
                    r.Activo))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<RepuestoDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }

        public Task<RepuestoDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
            db.Repuestos
                .AsNoTracking()
                .Where(r => r.IdRepuesto == id)
                .Select(r => new RepuestoDto(
                    r.IdRepuesto,
                    r.CodigoStock,
                    r.Nombre,
                    r.PrecioUnitario,
                    r.CantidadStock,
                    r.Activo))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<int> CrearAsync(CrearRepuestoRequest request, CancellationToken cancellationToken)
        {
            var repuesto = new Repuesto
            {
                CodigoStock = request.CodigoStock,
                Nombre = request.Nombre,
                PrecioUnitario = request.PrecioUnitario,
                CantidadStock = request.CantidadStock
            };

            db.Repuestos.Add(repuesto);
            await db.SaveChangesAsync(cancellationToken);

            return repuesto.IdRepuesto;
        }

        public async Task<bool> ActualizarAsync(
            int id,
            ActualizarRepuestoRequest request,
            CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Repuestos
                .Where(r => r.IdRepuesto == id)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, s => s
                    .SetProperty(r => r.CodigoStock, request.CodigoStock)
                    .SetProperty(r => r.Nombre, request.Nombre)
                    .SetProperty(r => r.PrecioUnitario, request.PrecioUnitario)
                    .SetProperty(r => r.CantidadStock, request.CantidadStock)
                    .SetProperty(r => r.Activo, request.Activo),
                    cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        }

        //[INICIO][30/8/2026][Rodriale][Baja lógica: el repuesto queda referenciado en órdenes y facturas]
        public async Task<bool> DarDeBajaAsync(int id, CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Repuestos
                .Where(r => r.IdRepuesto == id)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, s => s.SetProperty(r => r.Activo, false), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        }
        //[FIN][30/8/2026][Rodriale][Baja lógica: el repuesto queda referenciado en órdenes y facturas]
    }
}
