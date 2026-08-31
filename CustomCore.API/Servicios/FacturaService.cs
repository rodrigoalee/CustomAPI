//[INICIO][31/8/2026][Rodriale][Servicio de facturación: convierte una orden terminada en un documento de cobro congelado en el tiempo]
using CustomCore.API.Configuracion;
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomCore.API.Servicios
{
    public sealed class FacturaService(AppDbContext db, IOptions<OpcionesFacturacion> opciones)
    {
        private readonly OpcionesFacturacion _opciones = opciones.Value;

        //[INICIO][31/8/2026][Rodriale][Historial de cobros con filtros; el más reciente primero, que es lo que siempre se busca]
        public async Task<ResultadoPaginado<FacturaListaDto>> ObtenerAsync(
            ConsultaFacturas consulta,
            CancellationToken cancellationToken)
        {
            var query = db.FacturasEncabezado.AsNoTracking();

            if (consulta.ClienteId is { } clienteId)
                query = query.Where(f => f.ClienteId == clienteId);

            if (consulta.EstadoPago is { } estadoPago)
                query = query.Where(f => f.EstadoPago == estadoPago);

            if (consulta.Desde is { } desde)
                query = query.Where(f => f.FechaEmision >= desde);

            if (consulta.Hasta is { } hasta)
                query = query.Where(f => f.FechaEmision <= hasta);

            //[INICIO][31/8/2026][Rodriale][Se busca por el nombre congelado en la factura, no por el actual del cliente]
            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(f => EF.Functions.ILike(f.NombreFacturacion, patron));
            }
            //[FIN][31/8/2026][Rodriale][Se busca por el nombre congelado en la factura]

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(f => f.FechaEmision)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(f => new FacturaListaDto(
                    f.IdFacturaEncabezado,
                    f.OrdenTrabajoId,
                    f.FechaEmision,
                    f.NombreFacturacion,
                    f.NitFacturacion,
                    f.TotalPagado,
                    f.EstadoPago))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<FacturaListaDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }
        //[FIN][31/8/2026][Rodriale][Historial de cobros]

        //[INICIO][31/8/2026][Rodriale][Factura completa con sus renglones, todo en una sola consulta]
        public Task<FacturaDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
            db.FacturasEncabezado
                .AsNoTracking()
                .Where(f => f.IdFacturaEncabezado == id)
                .Select(f => new FacturaDetalleDto(
                    f.IdFacturaEncabezado,
                    f.OrdenTrabajoId,
                    f.FechaEmision,
                    f.ClienteId,
                    f.NombreFacturacion,
                    f.NitFacturacion,
                    f.OrdenTrabajo.Vehiculo.Placa,
                    f.Subtotal,
                    f.TasaImpuesto,
                    f.MontoImpuesto,
                    f.TotalPagado,
                    f.EstadoPago,
                    f.StripePaymentId,
                    f.UsuarioCajero.NombreCompleto,
                    f.Detalles
                        .OrderBy(d => d.IdFacturaDetalle)
                        .Select(d => new LineaFacturaDto(
                            d.IdFacturaDetalle,
                            d.DescripcionItem,
                            d.Cantidad,
                            d.PrecioUnitario,
                            d.Subtotal))
                        .ToList()))
                .FirstOrDefaultAsync(cancellationToken);
        //[FIN][31/8/2026][Rodriale][Factura completa con sus renglones]

        //[INICIO][31/8/2026][Rodriale][Emitir la factura: se copia todo de la orden y del cliente para que el documento no cambie nunca más]
        public async Task<int?> CrearAsync(CrearFacturaRequest request, CancellationToken cancellationToken)
        {
            var orden = await db.OrdenesTrabajoEncabezado
                .AsNoTracking()
                .Where(o => o.IdOrdenEncabezado == request.OrdenTrabajoId)
                .Select(o => new
                {
                    o.IdOrdenEncabezado,
                    o.Estado,
                    o.Vehiculo.ClienteId,
                    NombreCliente = o.Vehiculo.Cliente.NombreCompleto,
                    NitCliente = o.Vehiculo.Cliente.Nit,
                    YaFacturada = db.FacturasEncabezado.Any(f => f.OrdenTrabajoId == o.IdOrdenEncabezado),
                    Lineas = o.Detalles
                        .OrderBy(d => d.IdOrdenDetalle)
                        .Select(d => new
                        {
                            Descripcion = d.RepuestoId != null ? d.Repuesto!.Nombre : d.Servicio!.Nombre,
                            d.Cantidad,
                            d.PrecioUnitario,
                            d.SubtotalEstimado
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (orden is null)
                return null;

            //[INICIO][31/8/2026][Rodriale][Tres candados antes de emitir: que no esté facturada, que el trabajo esté terminado y que haya algo que cobrar]
            if (orden.YaFacturada)
                throw new ConflictoNegocioException("Esta orden ya tiene una factura emitida.");

            if (orden.Estado != EstadoOrdenTrabajo.Finalizado)
                throw new ConflictoNegocioException("Solo se puede facturar una orden finalizada.");

            if (orden.Lineas.Count == 0)
                throw new ConflictoNegocioException("La orden no tiene líneas de trabajo; no hay nada que cobrar.");
            //[FIN][31/8/2026][Rodriale][Tres candados antes de emitir]

            var totalLineas = orden.Lineas.Sum(l => l.SubtotalEstimado);
            var (subtotal, montoImpuesto, total) = CalcularImpuesto(totalLineas);

            var factura = new FacturaEncabezado
            {
                OrdenTrabajoId = orden.IdOrdenEncabezado,
                FechaEmision = DateTimeOffset.UtcNow,

                //[INICIO][31/8/2026][Rodriale][Snapshot fiscal: si mañana el cliente corrige su NIT, esta factura sigue diciendo lo que decía hoy]
                ClienteId = orden.ClienteId,
                NombreFacturacion = orden.NombreCliente,
                NitFacturacion = orden.NitCliente,
                //[FIN][31/8/2026][Rodriale][Snapshot fiscal]

                Subtotal = subtotal,
                TasaImpuesto = _opciones.TasaImpuesto,
                MontoImpuesto = montoImpuesto,
                TotalPagado = total,
                EstadoPago = EstadoPago.Pendiente,
                UsuarioCajeroId = request.UsuarioCajeroId
            };

            //[INICIO][31/8/2026][Rodriale][Los renglones se copian con su precio de la orden, no se recalculan desde el catálogo]
            foreach (var linea in orden.Lineas)
            {
                factura.Detalles.Add(new FacturaDetalle
                {
                    DescripcionItem = linea.Descripcion,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = linea.PrecioUnitario,
                    Subtotal = linea.SubtotalEstimado
                });
            }
            //[FIN][31/8/2026][Rodriale][Los renglones se copian con su precio de la orden]

            db.FacturasEncabezado.Add(factura);
            await db.SaveChangesAsync(cancellationToken);

            return factura.IdFacturaEncabezado;
        }
        //[FIN][31/8/2026][Rodriale][Emitir la factura]

        //[INICIO][31/8/2026][Rodriale][Registrar el cobro; una factura ya pagada no se vuelve a pagar]
        public async Task<bool> RegistrarPagoAsync(
            int id,
            RegistrarPagoRequest request,
            CancellationToken cancellationToken)
        {
            var estado = await db.FacturasEncabezado
                .AsNoTracking()
                .Where(f => f.IdFacturaEncabezado == id)
                .Select(f => (EstadoPago?)f.EstadoPago)
                .FirstOrDefaultAsync(cancellationToken);

            if (estado is null)
                return false;

            if (estado == EstadoPago.Pagado)
                throw new ConflictoNegocioException("Esta factura ya está pagada.");

            await db.FacturasEncabezado
                .Where(f => f.IdFacturaEncabezado == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(f => f.EstadoPago, EstadoPago.Pagado)
                    .SetProperty(f => f.StripePaymentId, request.StripePaymentId),
                    cancellationToken);

            return true;
        }
        //[FIN][31/8/2026][Rodriale][Registrar el cobro]

        //[INICIO][31/8/2026][Rodriale][Si el precio ya trae IVA se desglosa hacia atrás; si no, se suma encima. Se redondea a 2 decimales porque es dinero]
        private (decimal Subtotal, decimal MontoImpuesto, decimal Total) CalcularImpuesto(decimal totalLineas)
        {
            if (_opciones.PreciosIncluyenImpuesto)
            {
                var total = Redondear(totalLineas);
                var subtotal = Redondear(total / (1 + _opciones.TasaImpuesto));
                return (subtotal, total - subtotal, total);
            }

            var baseImponible = Redondear(totalLineas);
            var impuesto = Redondear(baseImponible * _opciones.TasaImpuesto);
            return (baseImponible, impuesto, baseImponible + impuesto);
        }

        private static decimal Redondear(decimal valor) =>
            Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        //[FIN][31/8/2026][Rodriale][Cálculo y redondeo del impuesto]
    }
}
//[FIN][31/8/2026][Rodriale][Servicio de facturación]
