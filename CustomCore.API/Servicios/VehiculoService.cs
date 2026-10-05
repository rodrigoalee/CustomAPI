using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public class VehiculoService(AppDbContext db)
    {

        public async Task<ResultadoPaginado<VehiculoListaDto>> ObtenerAsync(
            ConsultaVehiculos consulta,
            CancellationToken cancellationToken)
            {
            var query = db.Vehiculos.AsNoTracking();

            if (consulta.ClienteId is { } clienteId)
                query = query.Where(v => v.ClienteId == clienteId);

            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(v =>
                    EF.Functions.ILike(v.Placa, patron) ||
                    EF.Functions.ILike(v.Marca, patron) ||
                    EF.Functions.ILike(v.Modelo, patron));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
           .OrderBy(v => v.Placa)
           .Skip((consulta.Pagina - 1) * consulta.Tamanio)
           .Take(consulta.Tamanio)
           .Select(v => new VehiculoListaDto(
               v.IdVehiculo,
               v.Placa,
               v.Marca,
               v.Modelo,
               v.Anio,
               v.ClienteId,
               v.Cliente.NombreCompleto))
           .ToListAsync(cancellationToken
           );

            return new ResultadoPaginado<VehiculoListaDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }

        //[INICIO][30/8/2026][Rodriale][Detalle con datos del dueño y conteos de historial en una sola consulta]
        public Task<VehiculoDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
            db.Vehiculos
            .AsNoTracking()
            .Where(v => v.IdVehiculo == id)
            .Select(v => new VehiculoDetalleDto(
                v.IdVehiculo,
                v.Placa,
                v.Marca,
                v.Modelo,
                v.Anio,
                v.ClienteId,
                v.Cliente.NombreCompleto,
                v.Cliente.Telefono,
                v.Citas.Count,
                db.OrdenesTrabajoEncabezado.Count(o => o.VehiculoId == v.IdVehiculo)))
            .FirstOrDefaultAsync(cancellationToken);
        //[FIN][30/8/2026][Rodriale][Detalle con datos del dueño y conteos de historial en una sola consulta]

        public async Task<int> CrearAsync(CrearVehiculosRequest request, CancellationToken cancellationToken)
        {
            var vehiculo = new Vehiculo
            {
                ClienteId = request.ClienteId,
                Placa = request.Placa,
                Marca = request.Marca,
                Modelo = request.Modelo,
                Anio = request.Anio
            };

            db.Vehiculos.Add(vehiculo);
            await db.SaveChangesAsync(cancellationToken);

            return vehiculo.IdVehiculo;
        }

        public async Task<bool> ActualizarAsync(
            int id,
            ActualizarVehiculoRequest request,
            CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Vehiculos
                .Where(v => v.IdVehiculo == id)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, s => s
                    .SetProperty(v => v.ClienteId, request.ClienteId)
                    .SetProperty(v => v.Placa, request.Placa)
                    .SetProperty(v => v.Marca, request.Marca)
                    .SetProperty(v => v.Modelo, request.Modelo)
                    .SetProperty(v => v.Anio, request.Anio),
                    cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        
        }
        //[INICIO][30/8/2026][Rodriale][Si el vehículo tiene citas u órdenes, la FK Restrict lo impide y el manejador responde 409]
        public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Vehiculos
                .Where(v => v.IdVehiculo == id)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .EliminarAuditadoAsync(db, id, cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        }
        //[FIN][30/8/2026][Rodriale][Si el vehículo tiene citas u órdenes, la FK Restrict lo impide y el manejador responde 409]
    }
    //[FIN][30/8/2026][Rodriale][Servicio de vehículos: consultas proyectadas y escrituras mínimas]
}

