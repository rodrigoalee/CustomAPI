//[INICIO][31/8/2026][Rodriale][Clase que agenda con validación de solapamiento]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class CitaService(AppDbContext db, SucursalActual sucursal, NotificacionWhatsAppService whatsApp)
    {
        //[INICIO][31/8/2026][Rodriale][Margen para acotar la ventana que se trae a memoria al validar solapamiento]
        private const int VentanaDias = 1;
        //[FIN][31/8/2026][Rodriale][Margen para acotar la ventana que se trae a memoria al validar solapamiento]

        public async Task<ResultadoPaginado<CitaListaDto>> ObtenerAsync(
            ConsultaCitas consulta,
            CancellationToken cancellationToken)
        {
            var query = db.Citas.AsNoTracking();

            //[INICIO][12/10/2026][Rodriale][El empleado solo ve la agenda de su sede; quien ve todas puede filtrar]
            var filtroSucursal = await sucursal.AlcanceAsync(cancellationToken) ?? consulta.SucursalId;
            if (filtroSucursal is { } sucursalId)
                query = query.Where(c => c.SucursalId == sucursalId);
            //[FIN][12/10/2026][Rodriale][El empleado solo ve la agenda de su sede]

            if (consulta.VehiculoId is { } vehiculoId)
                query = query.Where(c => c.VehiculoId == vehiculoId);

            if (consulta.Desde is { } desde)
                query = query.Where(c => c.FechaHora >= desde);

            if (consulta.Hasta is { } hasta)
                query = query.Where(c => c.FechaHora <= hasta);

            if (consulta.Estado is { } estado)
                query = query.Where(c => c.Estado == estado);

            //[INICIO][25/10/2026][Rodriale][Solo las que aún no tienen orden de trabajo]
            if (consulta.SinOrden)
                query = query.Where(c => !db.OrdenesTrabajoEncabezado.Any(o => o.CitaId == c.IdCita));
            //[FIN][25/10/2026][Rodriale][Solo las que aún no tienen orden de trabajo]

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
                    c.SucursalId,
                    c.VehiculoId,
                    c.Vehiculo.Placa,
                    c.Vehiculo.Cliente.NombreCompleto,
                    c.FechaHora,
                    c.DuracionMinutos,
                    c.Estado,
                    c.Servicio != null ? c.Servicio.Nombre : null,
                    db.OrdenesTrabajoEncabezado.Where(o => o.CitaId == c.IdCita).Select(o => (int?)o.IdOrdenEncabezado).FirstOrDefault()))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<CitaListaDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }

        public async Task<CitaDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
        {
            //[INICIO][12/10/2026][Rodriale][Una cita de otra sede se comporta como inexistente]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Una cita de otra sede se comporta como inexistente]

            return await db.Citas
                .AsNoTracking()
                .Where(c => c.IdCita == id && (alcance == null || c.SucursalId == alcance))
                .Select(c => new CitaDetalleDto(
                    c.IdCita,
                    c.SucursalId,
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
                    c.Estado,
                    c.Servicio != null ? c.Servicio.Nombre : null,
                    db.OrdenesTrabajoEncabezado.Where(o => o.CitaId == c.IdCita).Select(o => (int?)o.IdOrdenEncabezado).FirstOrDefault()))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CrearAsync(CrearCitaRequest request, CancellationToken cancellationToken)
        {
            if (request.FechaHora < DateTimeOffset.UtcNow)
                throw new ConflictoNegocioException("No se puede agendar una cita en una fecha pasada.");

            //[INICIO][12/10/2026][Rodriale][La cita queda en la sede de quien la agenda]
            var sucursalId = await sucursal.ResolverAsync(request.SucursalId, cancellationToken);
            //[FIN][12/10/2026][Rodriale][La cita queda en la sede de quien la agenda]

            await ValidarSolapamientoAsync(
                request.VehiculoId,
                request.FechaHora,
                request.FechaHora.AddMinutes(request.DuracionMinutos),
                citaExcluida: null,
                cancellationToken);

            var cita = new Cita
            {
                VehiculoId = request.VehiculoId,
                SucursalId = sucursalId,
                FechaHora = request.FechaHora,
                DuracionMinutos = request.DuracionMinutos,
                Motivo = request.Motivo,
                Estado = EstadoCita.Pendiente
            };

            db.Citas.Add(cita);
            await db.SaveChangesAsync(cancellationToken);

            return cita.IdCita;
        }

        public Task<bool> ReprogramarAsync(
            int id,
            ReprogramarCitaRequest request,
            CancellationToken cancellationToken)
            => db.EjecutarUnidadAuditadaAsync<bool>(async () =>
        {
            //[INICIO][5/10/2026][jgarciad8][Validación y cambio bajo el mismo bloqueo transaccional]
            await db.BloquearFilaAsync<Cita>(id, cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Validación y cambio bajo el mismo bloqueo transaccional]

            //[INICIO][12/10/2026][Rodriale][Reprogramar una cita de otra sede responde 404]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Reprogramar una cita de otra sede responde 404]

            var cita = await db.Citas
                .AsNoTracking()
                .Where(c => c.IdCita == id && (alcance == null || c.SucursalId == alcance))
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
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, s => s
                    .SetProperty(c => c.FechaHora, request.FechaHora)
                    .SetProperty(c => c.DuracionMinutos, request.DuracionMinutos)
                    .SetProperty(c => c.Motivo, request.Motivo),
                    cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return true;
        }, cancellationToken);

        public async Task<bool> CambiarEstadoAsync(
            int id,
            CambiarEstadoCitaRequest request,
            CancellationToken cancellationToken)
        {
            //[INICIO][12/10/2026][Rodriale][Con la sede en el WHERE, una cita ajena afecta 0 filas y responde 404]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Con la sede en el WHERE]

            var filasAfectadas = await db.Citas
                .Where(c => c.IdCita == id && (alcance == null || c.SucursalId == alcance))
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, s => s.SetProperty(c => c.Estado, request.Estado), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            //[INICIO][25/10/2026][Rodriale][Al confirmar, el cliente recibe el aviso por WhatsApp]
            if (filasAfectadas > 0 && request.Estado == EstadoCita.Confirmada)
                await whatsApp.CitaConfirmadaAsync(id);
            //[FIN][25/10/2026][Rodriale][Al confirmar, el cliente recibe el aviso por WhatsApp]

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
