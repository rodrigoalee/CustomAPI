//[INICIO][17/9/2026][Rodriale][Servicio de sucursales: catálogo con baja lógica, mismo patrón que repuestos y servicios]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class SucursalService(AppDbContext db)
    {
        public async Task<ResultadoPaginado<SucursalDto>> ObtenerAsync(
            ConsultaCatalogo consulta,
            CancellationToken cancellationToken)
        {
            var query = db.Sucursales.AsNoTracking();

            if (consulta.SoloActivos)
                query = query.Where(s => s.Activo);

            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(s =>
                    EF.Functions.ILike(s.Nombre, patron) ||
                    EF.Functions.ILike(s.Direccion, patron));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(s => s.Nombre)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(s => new SucursalDto(s.IdSucursal, s.Nombre, s.Direccion, s.Telefono, s.Activo))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<SucursalDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }

        public Task<SucursalDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
            db.Sucursales
                .AsNoTracking()
                .Where(s => s.IdSucursal == id)
                .Select(s => new SucursalDto(s.IdSucursal, s.Nombre, s.Direccion, s.Telefono, s.Activo))
                .FirstOrDefaultAsync(cancellationToken);

        //[INICIO][17/9/2026][Rodriale][Un nombre repetido lo rechaza el índice único y el manejador global responde 409]
        public async Task<int> CrearAsync(CrearSucursalRequest request, CancellationToken cancellationToken)
        {
            var sucursal = new Sucursal
            {
                Nombre = request.Nombre.Trim(),
                Direccion = request.Direccion.Trim(),
                Telefono = request.Telefono
            };

            db.Sucursales.Add(sucursal);
            await db.SaveChangesAsync(cancellationToken);

            return sucursal.IdSucursal;
        }
        //[FIN][17/9/2026][Rodriale][Un nombre repetido lo rechaza el índice único]

        public async Task<bool> ActualizarAsync(
            int id,
            ActualizarSucursalRequest request,
            CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Sucursales
                .Where(s => s.IdSucursal == id)
                //[INICIO][12/10/2026][Rodriale][Operación directa con bloqueo y log atómico, como el resto de catálogos]
                .ActualizarAuditadoAsync(db, id, p => p
                    .SetProperty(s => s.Nombre, request.Nombre.Trim())
                    .SetProperty(s => s.Direccion, request.Direccion.Trim())
                    .SetProperty(s => s.Telefono, request.Telefono)
                    .SetProperty(s => s.Activo, request.Activo),
                    cancellationToken);
            //[FIN][12/10/2026][Rodriale][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        }

        //[INICIO][17/9/2026][Rodriale][Baja lógica la sede deja de ofrecerse, pero su historial queda intacto]
        public async Task<bool> DarDeBajaAsync(int id, CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Sucursales
                .Where(s => s.IdSucursal == id)
                //[INICIO][12/10/2026][Rodriale][Operación directa con bloqueo y log atómico, como el resto de catálogos]
                .ActualizarAuditadoAsync(db, id, p => p.SetProperty(s => s.Activo, false), cancellationToken);
            //[FIN][12/10/2026][Rodriale][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        }
        //[FIN][17/9/2026][Rodriale][Baja lógica la sede deja de ofrecerse, pero su historial queda intacto]
    }
}
//[FIN][17/9/2026][Rodriale][Servicio de sucursales]