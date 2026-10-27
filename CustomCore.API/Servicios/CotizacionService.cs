//[INICIO][26/10/2026][Rodriale][Cotizaciones: el taller cotiza trabajo fuera del catálogo y, si el cliente acepta, pasa a la orden]
using System.Linq.Expressions;
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class CotizacionService(
        AppDbContext db,
        SucursalActual sucursal,
        OrdenTrabajoService ordenes,
        NotificacionWhatsAppService whatsApp)
    {
        private static readonly Expression<Func<Cotizacion, CotizacionDto>> AProyeccion = c => new CotizacionDto(
            c.IdCotizacion,
            c.Estado,
            c.Nota,
            c.Total,
            c.FechaCreacion,
            c.FechaRespuesta,
            c.Detalles
                .OrderBy(d => d.IdCotizacionDetalle)
                .Select(d => new LineaCotizacionDto(d.Descripcion, d.Cantidad, d.PrecioUnitario, d.Subtotal))
                .ToList());

        //[INICIO][26/10/2026][Rodriale][Mismo alcance que el detalle de la orden: sede y, para el mecánico, solo las suyas]
        public async Task<IReadOnlyList<CotizacionDto>> ObtenerDeOrdenAsync(int ordenId, CancellationToken cancellationToken)
        {
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            var mecanico = await sucursal.MecanicoRestringidoAsync(cancellationToken);

            return await db.Cotizaciones
                .AsNoTracking()
                .Where(c => c.OrdenTrabajoId == ordenId)
                .Where(c => alcance == null || c.OrdenTrabajo.SucursalId == alcance)
                .Where(c => mecanico == null || c.OrdenTrabajo.MecanicoAsignadoId == mecanico)
                .OrderByDescending(c => c.IdCotizacion)
                .Select(AProyeccion)
                .ToListAsync(cancellationToken);
        }
        //[FIN][26/10/2026][Rodriale][Mismo alcance que el detalle de la orden]

        //[INICIO][26/10/2026][Rodriale][El cliente solo ve las cotizaciones de sus propias citas]
        public async Task<IReadOnlyList<CotizacionDto>> ObtenerDeCitaDelClienteAsync(
            int idCita,
            int clienteId,
            CancellationToken cancellationToken)
        {
            return await db.Cotizaciones
                .AsNoTracking()
                //[INICIO][27/10/2026][Rodriale][Incluye la cotización previa a la visita, que cuelga directo de la cita]
                .Where(c => (c.CitaId == idCita && c.Cita!.Vehiculo.ClienteId == clienteId)
                    || (c.OrdenTrabajo!.CitaId == idCita && c.OrdenTrabajo.Vehiculo.ClienteId == clienteId))
                //[FIN][27/10/2026][Rodriale][Incluye la cotización previa a la visita]
                .OrderByDescending(c => c.IdCotizacion)
                .Select(AProyeccion)
                .ToListAsync(cancellationToken);
        }
        //[FIN][26/10/2026][Rodriale][El cliente solo ve las cotizaciones de sus propias citas]

        //[INICIO][26/10/2026][Rodriale][Alta: la orden debe estar abierta; el aviso sale cuando ya quedó guardada]
        public async Task<int?> CrearAsync(
            int ordenId,
            CrearCotizacionRequest request,
            int usuarioId,
            CancellationToken cancellationToken)
        {
            if (await ordenes.EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken) is null)
                return null;

            var cotizacion = Armar(request, usuarioId);
            cotizacion.OrdenTrabajoId = ordenId;

            await db.EjecutarUnidadAuditadaAsync(async () =>
            {
                db.Cotizaciones.Add(cotizacion);
                await db.SaveChangesAsync(cancellationToken);
                return cotizacion.IdCotizacion;
            }, cancellationToken);

            await whatsApp.CotizacionEnviadaAsync(ordenId, cotizacion.Total);
            return cotizacion.IdCotizacion;
        }
        //[FIN][26/10/2026][Rodriale][Alta de la cotización]

        //[INICIO][27/10/2026][Rodriale][Cotización previa a la visita: solo para citas fuera del catálogo, pendientes y sin otra cotización esperando respuesta]
        public async Task<IReadOnlyList<CotizacionDto>> ObtenerDeCitaAsync(int citaId, CancellationToken cancellationToken)
        {
            var alcance = await sucursal.AlcanceAsync(cancellationToken);

            return await db.Cotizaciones
                .AsNoTracking()
                .Where(c => c.CitaId == citaId && (alcance == null || c.Cita!.SucursalId == alcance))
                .OrderByDescending(c => c.IdCotizacion)
                .Select(AProyeccion)
                .ToListAsync(cancellationToken);
        }

        public async Task<int?> CrearDeCitaAsync(
            int citaId,
            CrearCotizacionRequest request,
            int usuarioId,
            CancellationToken cancellationToken)
        {
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            var cotizacion = Armar(request, usuarioId);
            cotizacion.CitaId = citaId;

            var creada = await db.EjecutarUnidadAuditadaAsync<bool>(async () =>
            {
                var cita = await db.BloquearFilaAsync<Cita>(citaId, cancellationToken);
                if (cita is null || (alcance is not null && cita.SucursalId != alcance))
                    return false;

                if (!cita.ServicioPersonalizado || cita.Estado != EstadoCita.Pendiente)
                    throw new ConflictoNegocioException("Solo se cotizan citas pendientes con un servicio fuera del catálogo.");

                if (await db.Cotizaciones.AnyAsync(c => c.CitaId == citaId && c.Estado == EstadoCotizacion.Pendiente, cancellationToken))
                    throw new ConflictoNegocioException("Esta cita ya tiene una cotización esperando respuesta del cliente.");

                db.Cotizaciones.Add(cotizacion);
                await db.SaveChangesAsync(cancellationToken);
                return true;
            }, cancellationToken);

            if (!creada)
                return null;

            await whatsApp.CotizacionCitaEnviadaAsync(citaId, cotizacion.Total);
            return cotizacion.IdCotizacion;
        }
        //[FIN][27/10/2026][Rodriale][Cotización previa a la visita]

        private static Cotizacion Armar(CrearCotizacionRequest request, int usuarioId)
        {
            var cotizacion = new Cotizacion
            {
                Nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim(),
                FechaCreacion = DateTimeOffset.UtcNow,
                UsuarioCreaId = usuarioId
            };

            foreach (var linea in request.Lineas)
            {
                cotizacion.Detalles.Add(new CotizacionDetalle
                {
                    Descripcion = linea.Descripcion.Trim(),
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = linea.PrecioUnitario,
                    Subtotal = linea.PrecioUnitario * linea.Cantidad
                });
            }

            cotizacion.Total = cotizacion.Detalles.Sum(d => d.Subtotal);
            return cotizacion;
        }

        //[INICIO][26/10/2026][Rodriale][Respuesta del cliente: aceptar agrega las líneas a la orden en la misma transacción]
        //[INICIO][27/10/2026][Rodriale][Responde el cliente (clienteId) o recepción en su nombre (null, dentro de su sede); aceptar la de una cita la confirma]
        public async Task<bool> ResponderAsync(
            int idCotizacion,
            int? clienteId,
            bool aceptar,
            CancellationToken cancellationToken)
        {
            var alcance = clienteId is null ? await sucursal.AlcanceAsync(cancellationToken) : null;
            int? citaConfirmada = null;

            var respondida = await db.EjecutarUnidadAuditadaAsync<bool>(async () =>
            {
                citaConfirmada = null;
                var duenos = await db.Cotizaciones
                    .Where(c => c.IdCotizacion == idCotizacion)
                    .Where(c => clienteId == null
                        ? alcance == null || (c.CitaId != null ? c.Cita!.SucursalId : c.OrdenTrabajo!.SucursalId) == alcance
                        : (c.CitaId != null ? c.Cita!.Vehiculo.ClienteId : c.OrdenTrabajo!.Vehiculo.ClienteId) == clienteId)
                    .Select(c => new { c.OrdenTrabajoId, c.CitaId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (duenos is null)
                    return false;

                if (duenos.CitaId is { } idCita)
                {
                    if (await ResponderDeCitaAsync(idCotizacion, idCita, aceptar, cancellationToken))
                        citaConfirmada = idCita;
                    return true;
                }

                return await ResponderDeOrdenAsync(idCotizacion, duenos.OrdenTrabajoId!.Value, aceptar, cancellationToken);
            }, cancellationToken);

            if (citaConfirmada is { } confirmada)
                await whatsApp.CitaConfirmadaAsync(confirmada);

            return respondida;
        }

        private async Task<bool> ResponderDeCitaAsync(
            int idCotizacion,
            int idCita,
            bool aceptar,
            CancellationToken cancellationToken)
        {
            var cita = await db.BloquearFilaAsync<Cita>(idCita, cancellationToken)
                ?? throw new ConflictoNegocioException("La cita ya no existe.");

            var cotizacion = await db.Cotizaciones.SingleAsync(c => c.IdCotizacion == idCotizacion, cancellationToken);
            if (cotizacion.Estado != EstadoCotizacion.Pendiente)
                throw new ConflictoNegocioException("Esta cotización ya fue respondida.");

            if (cita.Estado != EstadoCita.Pendiente)
                throw new ConflictoNegocioException("La cita ya no está esperando la cotización.");

            cotizacion.Estado = aceptar ? EstadoCotizacion.Aceptada : EstadoCotizacion.Rechazada;
            cotizacion.FechaRespuesta = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            if (aceptar)
            {
                await db.Citas
                    .Where(c => c.IdCita == idCita)
                    .ActualizarAuditadoAsync(db, idCita, s => s.SetProperty(c => c.Estado, EstadoCita.Confirmada), cancellationToken);
            }

            return aceptar;
        }
        //[FIN][27/10/2026][Rodriale][Responde el cliente o recepción en su nombre]

        private async Task<bool> ResponderDeOrdenAsync(
            int idCotizacion,
            int idOrden,
            bool aceptar,
            CancellationToken cancellationToken)
        {
            // Serializa con los cambios de líneas de la misma orden.
            await db.BloquearFilaAsync<OrdenTrabajoEncabezado>(idOrden, cancellationToken);

            var cotizacion = await db.Cotizaciones
                .Include(c => c.Detalles)
                .SingleAsync(c => c.IdCotizacion == idCotizacion, cancellationToken);

            if (cotizacion.Estado != EstadoCotizacion.Pendiente)
                throw new ConflictoNegocioException("Esta cotización ya fue respondida.");

            if (aceptar)
            {
                var abierta = await db.OrdenesTrabajoEncabezado
                    .AnyAsync(o => o.IdOrdenEncabezado == idOrden
                        && o.Estado != EstadoOrdenTrabajo.Finalizado
                        && !db.FacturasEncabezado.Any(f => f.OrdenTrabajoId == idOrden),
                        cancellationToken);

                if (!abierta)
                    throw new ConflictoNegocioException(
                        "El trabajo de tu vehículo ya se cerró; comunícate con el taller para agregar este trabajo.");

                db.OrdenesTrabajoDetalle.AddRange(cotizacion.Detalles.Select(d => new OrdenTrabajoDetalle
                {
                    OrdenTrabajoId = idOrden,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    SubtotalEstimado = d.Subtotal
                }));
            }

            cotizacion.Estado = aceptar ? EstadoCotizacion.Aceptada : EstadoCotizacion.Rechazada;
            cotizacion.FechaRespuesta = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            if (aceptar)
                await ordenes.RecalcularTotalAsync(idOrden, cancellationToken);

            return true;
        }
        //[FIN][26/10/2026][Rodriale][Respuesta del cliente]
    }
}
//[FIN][26/10/2026][Rodriale][Cotizaciones]
