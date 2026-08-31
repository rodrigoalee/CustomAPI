//[INICIO][31/8/2026][Rodriale][agenda con validación de solapamiento]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class CitaService(AppDbContext db)
    {
        //[INICIO][31/8/2026][Rodriale][Margen para acotar la ventana que se trae a memoria al validar solapamiento]
        private const int VentanaDias = 1;
        //[FIN][31/8/2026][Rodriale][Margen para acotar la ventana que se trae a memoria al validar solapamiento]

        public async Task<ResultadoPaginado<CitaListaDto>> ObtenerAsync(
            ConsultaCitas consulta,
            CancellationToken cancellationToken)
        {
            var query = db.Citas.AsNoTracking();

            if (consulta.VehiculoId is { } vehiculoId)
                query = query.Where(c => c.VehiculoId == vehiculoId);

            if (consulta.Desde is { } desde)
                query = query.Where(c => c.FechaHora >= desde);

            if (consulta.Hasta is { } hasta)
                query = query.Where(c => c.FechaHora <= hasta);

            if (consulta.Estado is { } estado)
                query = query.Where(c => c.Estado == estado);

            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(c =>
                    EF.Functions.ILike(c.Vehiculo.Placa, patron) ||
                    EF.Functions.ILike(c.Vehiculo.Cliente.NombreCompleto, patron));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(c => c.FechaHora)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(c => new CitaListaDto(
                    c.IdCita,
                    c.VehiculoId,
                    c.Vehiculo.Placa,
                    c.Vehiculo.Cliente.NombreCompleto,
                    c.FechaHora,
                    c.DuracionMinutos,
                    c.Estado))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<CitaListaDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }

        public Task<CitaDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
            db.Citas
                .AsNoTracking()
                .Where(c => c.IdCita == id)
                .Select(c => new CitaDetalleDto(
                    c.IdCita,
                    c.VehiculoId,
                    c.Vehiculo.Placa,
                    c.Vehiculo.Marca,
                    c.Vehiculo.Modelo,
                    c.Vehiculo.ClienteId,
                    c.Vehiculo.Cliente.NombreCompleto,
                    c.Vehiculo.Cliente.Telefono,
                    c.FechaHora,
                    c.DuracionMinutos,
                    c.Motivo,
                    c.Estado))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<int> CrearAsync(CrearCitaRequest request, CancellationToken cancellationToken)
        {
            if (request.FechaHora < DateTimeOffset.UtcNow)
                throw new ConflictoNegocioException("No se puede agendar una cita en una fecha pasada.");

            await ValidarSolapamientoAsync(
                request.VehiculoId,
                request.FechaHora,
                request.FechaHora.AddMinutes(request.DuracionMinutos),
                citaExcluida: null,
                cancellationToken);

            var cita = new Cita
            {
                VehiculoId = request.VehiculoId,
                FechaHora = request.FechaHora,
                DuracionMinutos = request.DuracionMinutos,
                Motivo = request.Motivo,
                Estado = EstadoCita.Pendiente
            };

            db.Citas.Add(cita);
            await db.SaveChangesAsync(cancellationToken);

            return cita.IdCita;
        }

        public async Task<bool> ReprogramarAsync(
            int id,
            ReprogramarCitaRequest request,
            CancellationToken cancellationToken)
        {
            var cita = await db.Citas
                .AsNoTracking()
                .Where(c => c.IdCita == id)
                .Select(c => new { c.VehiculoId, c.Estado })
                .FirstOrDefaultAsync(cancellationToken);

            if (cita is null)
                return false;

            if (cita.Estado == EstadoCita.Cancelada)
                throw new ConflictoNegocioException("Una cita cancelada no se puede reprogramar; agenda una nueva.");

            if (request.FechaHora < DateTimeOffset.UtcNow)
                throw new ConflictoNegocioException("No se puede reprogramar una cita a una fecha pasada.");

            await ValidarSolapamientoAsync(
                cita.VehiculoId,
                request.FechaHora,
                request.FechaHora.AddMinutes(request.DuracionMinutos),
                citaExcluida: id,
                cancellationToken);

            await db.Citas
                .Where(c => c.IdCita == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.FechaHora, request.FechaHora)
                    .SetProperty(c => c.DuracionMinutos, request.DuracionMinutos)
                    .SetProperty(c => c.Motivo, request.Motivo),
                    cancellationToken);

            return true;
        }

        public async Task<bool> CambiarEstadoAsync(
            int id,
            CambiarEstadoCitaRequest request,
            CancellationToken cancellationToken)
        {
            var filasAfectadas = await db.Citas
                .Where(c => c.IdCita == id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, request.Estado), cancellationToken);

            return filasAfectadas > 0;
        }

        //[INICIO][31/8/2026][Rodriale][Se acota la ventana en SQL y se compara el traslape exacto en memoria: sumar la duración a la columna no tiene traducción garantizada a SQL]
        private async Task ValidarSolapamientoAsync(
            int vehiculoId,
            DateTimeOffset inicio,
            DateTimeOffset fin,
            int? citaExcluida,
            CancellationToken cancellationToken)
        {
            var desde = inicio.AddDays(-VentanaDias);
            var hasta = fin.AddDays(VentanaDias);

            var candidatas = await db.Citas
                .AsNoTracking()
                .Where(c => c.VehiculoId == vehiculoId
                    && c.Estado != EstadoCita.Cancelada
                    && c.FechaHora >= desde
                    && c.FechaHora <= hasta
                    && (citaExcluida == null || c.IdCita != citaExcluida))
                .Select(c => new { c.IdCita, c.FechaHora, c.DuracionMinutos })
                .ToListAsync(cancellationToken);

            var choque = candidatas.FirstOrDefault(c =>
                c.FechaHora < fin && inicio < c.FechaHora.AddMinutes(c.DuracionMinutos));

            if (choque is not null)
                throw new ConflictoNegocioException(
                    $"El vehículo ya tiene la cita {choque.IdCita} el {choque.FechaHora:dd/MM/yyyy HH:mm} y se solapa con el horario solicitado.");
        }
        //[FIN][31/8/2026][Rodriale][Se acota la ventana en SQL y se compara el traslape exacto en memoria]
    }
}
//[FIN][31/8/2026][Rodriale][agenda con validación de solapamiento]