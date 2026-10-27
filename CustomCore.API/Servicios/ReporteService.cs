//[INICIO][27/10/2026][Rodriale][Consultas de reportería: cada reporte arma su tabla y GeneradorReportes la vuelca al formato pedido]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public sealed class ReporteService(AppDbContext db, SucursalActual sucursal)
{
    public async Task<ArchivoReporte> GenerarAsync(
        TipoReporte tipo,
        ConsultaReporte consulta,
        CancellationToken cancellationToken)
    {
        //[INICIO][27/10/2026][Rodriale][El empleado solo reporta su sede; quien ve todas puede filtrar o ver el total]
        var sede = await sucursal.AlcanceAsync(cancellationToken) ?? consulta.SucursalId;
        //[FIN][27/10/2026][Rodriale][El empleado solo reporta su sede]

        var sedes = await db.Sucursales.AsNoTracking()
            .ToDictionaryAsync(s => s.IdSucursal, s => s.Nombre, cancellationToken);

        //[INICIO][27/10/2026][Rodriale][Por defecto, el mes en curso en hora de Guatemala; el rango se cierra al final del día "hasta"]
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, HorarioTaller.Zona).DateTime);
        var desde = consulta.Desde ?? new DateOnly(hoy.Year, hoy.Month, 1);
        var hasta = consulta.Hasta ?? hoy;
        var inicio = InicioDelDia(desde);
        var fin = InicioDelDia(hasta.AddDays(1));
        //[FIN][27/10/2026][Rodriale][Por defecto, el mes en curso]

        var alcance = sede is { } id ? $"Sede {sedes.GetValueOrDefault(id, $"#{id}")}" : "Todas las sedes";
        var periodo = $"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy} · {alcance}";

        var tabla = tipo switch
        {
            TipoReporte.Ventas => await VentasAsync(inicio, fin, sede, sedes, periodo, cancellationToken),
            TipoReporte.Ordenes => await OrdenesAsync(inicio, fin, sede, sedes, periodo, cancellationToken),
            TipoReporte.Inventario => await InventarioAsync(sede, sedes, alcance, cancellationToken),
            _ => await CitasAsync(inicio, fin, sede, sedes, periodo, cancellationToken)
        };

        return GeneradorReportes.Generar(tabla, consulta.Formato, $"reporte-{tipo.ToString().ToLowerInvariant()}");
    }

    private static DateTimeOffset InicioDelDia(DateOnly fecha)
    {
        var local = fecha.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, HorarioTaller.Zona.GetUtcOffset(local)).ToUniversalTime();
    }

    private async Task<TablaReporte> VentasAsync(
        DateTimeOffset inicio, DateTimeOffset fin, int? sede, Dictionary<int, string> sedes, string periodo,
        CancellationToken cancellationToken)
    {
        var facturas = await db.FacturasEncabezado.AsNoTracking()
            .Where(f => f.FechaEmision >= inicio && f.FechaEmision < fin && (sede == null || f.OrdenTrabajo.SucursalId == sede))
            .OrderBy(f => f.FechaEmision)
            .Select(f => new
            {
                f.IdFacturaEncabezado,
                f.FechaEmision,
                f.NombreFacturacion,
                f.NitFacturacion,
                f.OrdenTrabajoId,
                f.OrdenTrabajo.SucursalId,
                f.Subtotal,
                f.MontoImpuesto,
                f.TotalPagado,
                f.EstadoPago
            })
            .ToListAsync(cancellationToken);

        return new TablaReporte(
            "Reporte de ventas",
            periodo,
            [
                new("Factura", TipoColumna.Entero), new("Fecha", TipoColumna.Fecha), new("Cliente"), new("NIT"),
                new("Orden", TipoColumna.Entero), new("Sede"), new("Subtotal", TipoColumna.Moneda),
                new("IVA", TipoColumna.Moneda), new("Total", TipoColumna.Moneda), new("Estado")
            ],
            facturas.Select(f => new object?[]
            {
                f.IdFacturaEncabezado, f.FechaEmision, f.NombreFacturacion, f.NitFacturacion ?? "CF", f.OrdenTrabajoId,
                sedes.GetValueOrDefault(f.SucursalId), f.Subtotal, f.MontoImpuesto, f.TotalPagado,
                f.EstadoPago == EstadoPago.Pagado ? "Pagada" : "Pendiente"
            }).ToList());
    }

    private async Task<TablaReporte> OrdenesAsync(
        DateTimeOffset inicio, DateTimeOffset fin, int? sede, Dictionary<int, string> sedes, string periodo,
        CancellationToken cancellationToken)
    {
        var ordenes = await db.OrdenesTrabajoEncabezado.AsNoTracking()
            .Where(o => o.FechaIngreso >= inicio && o.FechaIngreso < fin && (sede == null || o.SucursalId == sede))
            .OrderBy(o => o.FechaIngreso)
            .Select(o => new
            {
                o.IdOrdenEncabezado,
                o.FechaIngreso,
                o.FechaFinalizacion,
                Cliente = o.Vehiculo.Cliente.NombreCompleto,
                o.Vehiculo.Placa,
                Mecanico = o.MecanicoAsignado != null ? o.MecanicoAsignado.NombreCompleto : null,
                o.SucursalId,
                o.Estado,
                o.TotalEstimado
            })
            .ToListAsync(cancellationToken);

        return new TablaReporte(
            "Reporte de órdenes de trabajo",
            periodo,
            [
                new("Orden", TipoColumna.Entero), new("Ingreso", TipoColumna.Fecha), new("Finalización", TipoColumna.Fecha),
                new("Cliente"), new("Placa"), new("Mecánico"), new("Sede"), new("Estado"), new("Total", TipoColumna.Moneda)
            ],
            ordenes.Select(o => new object?[]
            {
                o.IdOrdenEncabezado, o.FechaIngreso, o.FechaFinalizacion, o.Cliente, o.Placa, o.Mecanico ?? "Sin asignar",
                sedes.GetValueOrDefault(o.SucursalId), EstadoOrden(o.Estado), o.TotalEstimado
            }).ToList());
    }

    private async Task<TablaReporte> InventarioAsync(
        int? sede, Dictionary<int, string> sedes, string alcance, CancellationToken cancellationToken)
    {
        var existencias = await (
                from inventario in db.InventariosSucursal.AsNoTracking()
                join repuesto in db.Repuestos.AsNoTracking() on inventario.RepuestoId equals repuesto.IdRepuesto
                where repuesto.Activo && (sede == null || inventario.SucursalId == sede)
                orderby inventario.SucursalId, repuesto.Nombre
                select new
                {
                    repuesto.CodigoStock,
                    repuesto.Nombre,
                    inventario.SucursalId,
                    inventario.Cantidad,
                    repuesto.PrecioUnitario
                })
            .ToListAsync(cancellationToken);

        return new TablaReporte(
            "Reporte de inventario",
            $"Existencias al {TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, HorarioTaller.Zona):dd/MM/yyyy HH:mm} · {alcance}",
            [
                new("Código"), new("Repuesto"), new("Sede"), new("Cantidad", TipoColumna.Entero),
                new("Precio", TipoColumna.Moneda), new("Valor", TipoColumna.Moneda)
            ],
            existencias.Select(e => new object?[]
            {
                e.CodigoStock, e.Nombre, sedes.GetValueOrDefault(e.SucursalId), e.Cantidad, e.PrecioUnitario,
                e.Cantidad * e.PrecioUnitario
            }).ToList());
    }

    private async Task<TablaReporte> CitasAsync(
        DateTimeOffset inicio, DateTimeOffset fin, int? sede, Dictionary<int, string> sedes, string periodo,
        CancellationToken cancellationToken)
    {
        var citas = await db.Citas.AsNoTracking()
            .Where(c => c.FechaHora >= inicio && c.FechaHora < fin && (sede == null || c.SucursalId == sede))
            .OrderBy(c => c.FechaHora)
            .Select(c => new
            {
                c.IdCita,
                c.FechaHora,
                Cliente = c.Vehiculo.Cliente.NombreCompleto,
                c.Vehiculo.Placa,
                Servicio = c.Servicio != null ? c.Servicio.Nombre : null,
                c.SucursalId,
                c.Estado
            })
            .ToListAsync(cancellationToken);

        return new TablaReporte(
            "Reporte de citas",
            periodo,
            [
                new("Cita", TipoColumna.Entero), new("Fecha", TipoColumna.Fecha), new("Cliente"), new("Placa"),
                new("Servicio"), new("Sede"), new("Estado")
            ],
            citas.Select(c => new object?[]
            {
                c.IdCita, c.FechaHora, c.Cliente, c.Placa, c.Servicio ?? "Sin servicio", sedes.GetValueOrDefault(c.SucursalId),
                c.Estado.ToString()
            }).ToList());
    }

    private static string EstadoOrden(EstadoOrdenTrabajo estado) => estado switch
    {
        EstadoOrdenTrabajo.EnRecepcion => "En recepción",
        EstadoOrdenTrabajo.EnProceso => "En proceso",
        _ => "Finalizada"
    };
}
//[FIN][27/10/2026][Rodriale][Consultas de reportería]
