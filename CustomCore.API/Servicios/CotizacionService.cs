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
                .Where(c => c.OrdenTrabajo.CitaId == idCita && c.OrdenTrabajo.Vehiculo.ClienteId == clienteId)
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

            var cotizacion = new Cotizacion
            {
                OrdenTrabajoId = ordenId,
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

        //[INICIO][26/10/2026][Rodriale][Respuesta del cliente: aceptar agrega las líneas a la orden en la misma transacción]
        public Task<bool> ResponderAsync(
            int idCotizacion,
            int clienteId,
            bool aceptar,
            CancellationToken cancellationToken)
        {
            return db.EjecutarUnidadAuditadaAsync(
                () => ResponderInternoAsync(idCotizacion, clienteId, aceptar, cancellationToken),
                cancellationToken);
        }

        private async Task<bool> ResponderInternoAsync(
            int idCotizacion,
            int clienteId,
            bool aceptar,
            CancellationToken cancellationToken)
        {
            var ordenId = await db.Cotizaciones
                .Where(c => c.IdCotizacion == idCotizacion && c.OrdenTrabajo.Vehiculo.ClienteId == clienteId)
                .Select(c => (int?)c.OrdenTrabajoId)
                .FirstOrDefaultAsync(cancellationToken);

            if (ordenId is not { } idOrden)
                return false;

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
