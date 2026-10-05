//[INICIO][28/8/2026][Rodriale][Servicio de clientes: consultas proyectadas y escrituras mínimas]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public class ClienteService(AppDbContext db)
    {
        //[INICIO][28/8/2026][Rodriale][Obtiene clientes con paginación y búsqueda]
        public async Task<ResultadoPaginado<ClienteResumenDto>> ObtenerAsync(ConsultaPaginada consulta, CancellationToken cancellationToken)
        {
            var query = db.Clientes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(c =>
                    EF.Functions.ILike(c.NombreCompleto, patron) ||
                    EF.Functions.ILike(c.Correo, patron));
            }

            var total = await query.CountAsync(cancellationToken);

            //[INICIO][28/8/2026][Rodriale][Aplicamos paginación y proyección a DTOs para optimizar la consulta]
            var items = await query
                .OrderBy(c => c.NombreCompleto)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                     .Select(c => new ClienteResumenDto(
                c.IdCliente,
                c.NombreCompleto,
                c.Telefono,
                c.Correo,
                c.Vehiculos.Count))
            .ToListAsync(cancellationToken);

            return new ResultadoPaginado<ClienteResumenDto>(items, consulta.Pagina, consulta.Tamanio, total);
            //[FIN][28/8/2026][Rodriale][Aplicamos paginación y proyección a DTOs para optimizar la consulta]
        }
        //[FIN][28/8/2026][Rodriale][Obtiene clientes con paginación y búsqueda]

        //[INICIO][28/8/2026][Rodriale][Detalle y vehículos en una sola consulta para evitar N+1]
        public Task<ClienteDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
            db.Clientes
            .AsNoTracking()
            .Where(c => c.IdCliente == id)
            .Select(c => new ClienteDetalleDto(
                c.IdCliente,
                c.NombreCompleto,
                c.Telefono,
                c.Correo,
                c.Nit,
                c.Vehiculos
                    .OrderBy(v => v.Placa)
                    .Select(v => new VehiculoResumenDto(v.IdVehiculo, v.Placa, v.Marca, v.Modelo, v.Anio))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
        //[FIN][28/8/2026][Rodriale][Detalle y vehículos en una sola consulta para evitar N+1]

        public async Task<int> CrearAsync(CrearClienteRequest request, CancellationToken cancellationToken)
        {
            var cliente = new Cliente
            {
                NombreCompleto = request.NombreCompleto,
                Telefono = request.Telefono,
                Correo = request.Correo,
                Nit = request.Nit
            };

            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(cancellationToken);

            return cliente.IdCliente;
        }

        //[INICIO][28/8/2026][Rodriale][UPDATE directo sin traer la entidad a memoria]
        public async Task<bool> ActualizarAsync(int id, ActualizarClienteRequest request, CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Clientes
                .Where(c => c.IdCliente == id)
                //[INICIO][28/8/2026][Rodriale][UPDATE directo sin traer la entidad a memoria]
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, s => s
                    .SetProperty(c => c.NombreCompleto, request.NombreCompleto)
                    .SetProperty(c => c.Telefono, request.Telefono)
                    .SetProperty(c => c.Correo, request.Correo)
                    .SetProperty(c => c.Nit, request.Nit), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
            //[FIN][28/8/2026][Rodriale][UPDATE directo sin traer la entidad a memoria]
            return filasAfectadas > 0;
        }
        //[FIN][28/8/2026][Rodriale][UPDATE directo sin traer la entidad a memoria]
    }
}
//[FIN][28/8/2026][Rodriale][Servicio de clientes: consultas proyectadas y escrituras mínimas]
