//[INICIO][31/8/2026][Rodriale][Servicio de órdenes de trabajo: aquí vive la parte pesada del taller, crear la orden sin dejar el inventario descuadrado]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class OrdenTrabajoService(AppDbContext db)
    {
        //[INICIO][31/8/2026][Rodriale][Listado del tablero: filtros opcionales que se van encadenando solo si vienen]
        public async Task<ResultadoPaginado<OrdenListaDto>> ObtenerAsync(
            ConsultaOrdenes consulta,
            CancellationToken cancellationToken)
        {
            var query = db.OrdenesTrabajoEncabezado.AsNoTracking();

            if (consulta.VehiculoId is { } vehiculoId)
                query = query.Where(o => o.VehiculoId == vehiculoId);

            if (consulta.Estado is { } estado)
                query = query.Where(o => o.Estado == estado);

            if (consulta.Desde is { } desde)
                query = query.Where(o => o.FechaIngreso >= desde);

            if (consulta.Hasta is { } hasta)
                query = query.Where(o => o.FechaIngreso <= hasta);

            //[INICIO][31/8/2026][Rodriale][Se busca por placa o por nombre del dueño, que es como pregunta la gente en recepción]
            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(o =>
                    EF.Functions.ILike(o.Vehiculo.Placa, patron) ||
                    EF.Functions.ILike(o.Vehiculo.Cliente.NombreCompleto, patron));
            }
            //[FIN][31/8/2026][Rodriale][Se busca por placa o por nombre del dueño]

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(o => o.FechaIngreso)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(o => new OrdenListaDto(
                    o.IdOrdenEncabezado,
                    o.VehiculoId,
                    o.Vehiculo.Placa,
                    o.Vehiculo.Cliente.NombreCompleto,
                    o.FechaIngreso,
                    o.FechaFinalizacion,
                    o.Estado,
                    o.TotalEstimado))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<OrdenListaDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }
        //[FIN][31/8/2026][Rodriale][Listado del tablero]

        //[INICIO][31/8/2026][Rodriale][Detalle completo en una sola consulta: orden, vehículo, dueño, responsables y líneas, sin caer en N+1]
        public Task<OrdenDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
            db.OrdenesTrabajoEncabezado
                .AsNoTracking()
                .Where(o => o.IdOrdenEncabezado == id)
                .Select(o => new OrdenDetalleDto(
                    o.IdOrdenEncabezado,
                    o.VehiculoId,
                    o.Vehiculo.Placa,
                    o.Vehiculo.Marca,
                    o.Vehiculo.Modelo,
                    o.Vehiculo.ClienteId,
                    o.Vehiculo.Cliente.NombreCompleto,
                    o.FechaIngreso,
                    o.FechaFinalizacion,
                    o.Diagnostico,
                    o.Estado,
                    o.TotalEstimado,
                    o.UsuarioRecepcion.NombreCompleto,
                    o.MecanicoAsignado != null ? o.MecanicoAsignado.NombreCompleto : null,
                    o.Detalles
                        .OrderBy(d => d.IdOrdenDetalle)
                        .Select(d => new LineaOrdenDto(
                            d.IdOrdenDetalle,
                            d.RepuestoId != null ? "Repuesto" : "Servicio",
                            d.RepuestoId,
                            d.ServicioId,
                            d.RepuestoId != null ? d.Repuesto!.Nombre : d.Servicio!.Nombre,
                            d.Cantidad,
                            d.PrecioUnitario,
                            d.SubtotalEstimado))
                        .ToList()))
                .FirstOrDefaultAsync(cancellationToken);
        //[FIN][31/8/2026][Rodriale][Detalle completo en una sola consulta]

        //[INICIO][31/8/2026][Rodriale][Crear orden: es todo o nada, si una línea se cae por stock no queda ni la orden ni el descuento]
        public Task<int> CrearAsync(CrearOrdenRequest request, CancellationToken cancellationToken)
        {
            //[INICIO][31/8/2026][Rodriale][Como activamos EnableRetryOnFailure, EF exige que las transacciones manuales vayan envueltas en la estrategia de reintento; si no, revienta al abrir la transacción]
            var estrategia = db.Database.CreateExecutionStrategy();
            return estrategia.ExecuteAsync(() => CrearInternoAsync(request, cancellationToken));
            //[FIN][31/8/2026][Rodriale][Transacción manual envuelta en la estrategia de reintento]
        }

        private async Task<int> CrearInternoAsync(CrearOrdenRequest request, CancellationToken cancellationToken)
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            //[INICIO][31/8/2026][Rodriale][Primero se valida que todo lo pedido exista y esté activo, antes de mover una sola pieza de bodega]
            var repuestos = await CargarRepuestosAsync(request.Lineas, cancellationToken);
            var servicios = await CargarServiciosAsync(request.Lineas, cancellationToken);
            //[FIN][31/8/2026][Rodriale][Primero se valida que todo exista y esté activo]

            await DescontarStockAsync(request.Lineas, repuestos, cancellationToken);

            var orden = new OrdenTrabajoEncabezado
            {
                VehiculoId = request.VehiculoId,
                UsuarioRecepcionId = request.UsuarioRecepcionId,
                MecanicoAsignadoId = request.MecanicoAsignadoId,
                FechaIngreso = DateTimeOffset.UtcNow,
                Diagnostico = request.Diagnostico,
                Estado = EstadoOrdenTrabajo.EnRecepcion
            };

            //[INICIO][31/8/2026][Rodriale][El precio sale del catálogo, no del request: así nadie cotiza al precio que se le antoje, y queda congelado por si mañana sube]
            foreach (var linea in request.Lineas)
            {
                var precioUnitario = linea.RepuestoId is { } repuestoId
                    ? repuestos[repuestoId].PrecioUnitario
                    : servicios[linea.ServicioId!.Value].TarifaManoObra;

                orden.Detalles.Add(new OrdenTrabajoDetalle
                {
                    RepuestoId = linea.RepuestoId,
                    ServicioId = linea.ServicioId,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = precioUnitario,
                    SubtotalEstimado = precioUnitario * linea.Cantidad
                });
            }
            //[FIN][31/8/2026][Rodriale][El precio sale del catálogo, no del request]

            orden.TotalEstimado = orden.Detalles.Sum(d => d.SubtotalEstimado);

            db.OrdenesTrabajoEncabezado.Add(orden);
            await db.SaveChangesAsync(cancellationToken);

            await transaccion.CommitAsync(cancellationToken);

            return orden.IdOrdenEncabezado;
        }
        //[FIN][31/8/2026][Rodriale][Crear orden: es todo o nada]

        //[INICIO][31/8/2026][Rodriale][Actualizar el encabezado: notas del mecánico y a quién se le asigna. No toca líneas ni totales]
        public async Task<bool> ActualizarAsync(
            int ordenId,
            ActualizarOrdenRequest request,
            CancellationToken cancellationToken)
        {
            if (await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken) is null)
                return false;

            await db.OrdenesTrabajoEncabezado
                .Where(o => o.IdOrdenEncabezado == ordenId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.MecanicoAsignadoId, request.MecanicoAsignadoId)
                    .SetProperty(o => o.Diagnostico, request.Diagnostico),
                    cancellationToken);

            return true;
        }
        //[FIN][31/8/2026][Rodriale][Actualizar el encabezado]

        //[INICIO][31/8/2026][Rodriale][Cambiar de estado; al finalizar se sella la fecha de salida, y si se reabre se limpia para no dejar una fecha mentirosa]
        public async Task<bool> CambiarEstadoAsync(
            int ordenId,
            CambiarEstadoOrdenRequest request,
            CancellationToken cancellationToken)
        {
            if (await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken) is null)
                return false;

            var finalizacion = request.Estado == EstadoOrdenTrabajo.Finalizado
                ? DateTimeOffset.UtcNow
                : (DateTimeOffset?)null;

            await db.OrdenesTrabajoEncabezado
                .Where(o => o.IdOrdenEncabezado == ordenId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Estado, request.Estado)
                    .SetProperty(o => o.FechaFinalizacion, finalizacion),
                    cancellationToken);

            return true;
        }
        //[FIN][31/8/2026][Rodriale][Cambiar de estado]

        //[INICIO][31/8/2026][Rodriale][Agregar una línea a una orden ya abierta: mismo cuidado que al crearla, transacción y descuento de stock]
        public Task<int?> AgregarLineaAsync(
            int ordenId,
            CrearLineaOrdenRequest request,
            CancellationToken cancellationToken)
        {
            var estrategia = db.Database.CreateExecutionStrategy();
            return estrategia.ExecuteAsync(() => AgregarLineaInternoAsync(ordenId, request, cancellationToken));
        }

        private async Task<int?> AgregarLineaInternoAsync(
            int ordenId,
            CrearLineaOrdenRequest request,
            CancellationToken cancellationToken)
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            if (await EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken) is null)
                return null;

            IReadOnlyList<CrearLineaOrdenRequest> lineas = [request];

            var repuestos = await CargarRepuestosAsync(lineas, cancellationToken);
            var servicios = await CargarServiciosAsync(lineas, cancellationToken);

            await DescontarStockAsync(lineas, repuestos, cancellationToken);

            var precioUnitario = request.RepuestoId is { } repuestoId
                ? repuestos[repuestoId].PrecioUnitario
                : servicios[request.ServicioId!.Value].TarifaManoObra;

            var linea = new OrdenTrabajoDetalle
            {
                OrdenTrabajoId = ordenId,
                RepuestoId = request.RepuestoId,
                ServicioId = request.ServicioId,
                Cantidad = request.Cantidad,
                PrecioUnitario = precioUnitario,
                SubtotalEstimado = precioUnitario * request.Cantidad
            };

            db.OrdenesTrabajoDetalle.Add(linea);
            await db.SaveChangesAsync(cancellationToken);

            await RecalcularTotalAsync(ordenId, cancellationToken);

            await transaccion.CommitAsync(cancellationToken);

            return linea.IdOrdenDetalle;
        }
        //[FIN][31/8/2026][Rodriale][Agregar una línea a una orden ya abierta]

        //[INICIO][31/8/2026][Rodriale][Quitar una línea devuelve el repuesto a bodega; si no se repone, el inventario queda descuadrado para siempre]
        public Task<bool> QuitarLineaAsync(
            int ordenId,
            int lineaId,
            CancellationToken cancellationToken)
        {
            var estrategia = db.Database.CreateExecutionStrategy();
            return estrategia.ExecuteAsync(() => QuitarLineaInternoAsync(ordenId, lineaId, cancellationToken));
        }

        private async Task<bool> QuitarLineaInternoAsync(
            int ordenId,
            int lineaId,
            CancellationToken cancellationToken)
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            if (await EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken) is null)
                return false;

            var linea = await db.OrdenesTrabajoDetalle
                .AsNoTracking()
                .Where(d => d.IdOrdenDetalle == lineaId && d.OrdenTrabajoId == ordenId)
                .Select(d => new { d.RepuestoId, d.Cantidad })
                .FirstOrDefaultAsync(cancellationToken);

            if (linea is null)
                return false;

            await db.OrdenesTrabajoDetalle
                .Where(d => d.IdOrdenDetalle == lineaId)
                .ExecuteDeleteAsync(cancellationToken);

            //[INICIO][31/8/2026][Rodriale][Solo los repuestos regresan a bodega; la mano de obra no tiene existencias que devolver]
            if (linea.RepuestoId is { } repuestoId)
            {
                var cantidad = linea.Cantidad;

                await db.Repuestos
                    .Where(r => r.IdRepuesto == repuestoId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.CantidadStock, r => r.CantidadStock + cantidad),
                        cancellationToken);
            }
            //[FIN][31/8/2026][Rodriale][Solo los repuestos regresan a bodega]

            await RecalcularTotalAsync(ordenId, cancellationToken);

            await transaccion.CommitAsync(cancellationToken);

            return true;
        }
        //[FIN][31/8/2026][Rodriale][Quitar una línea devuelve el repuesto a bodega]

        //[INICIO][31/8/2026][Rodriale][Una orden ya facturada es intocable: cambiarla alteraría un documento fiscal ya emitido]
        private async Task<EstadoOrdenTrabajo?> ExisteYNoEstaFacturadaAsync(
            int ordenId,
            CancellationToken cancellationToken)
        {
            var info = await db.OrdenesTrabajoEncabezado
                .AsNoTracking()
                .Where(o => o.IdOrdenEncabezado == ordenId)
                .Select(o => new
                {
                    o.Estado,
                    TieneFactura = db.FacturasEncabezado.Any(f => f.OrdenTrabajoId == o.IdOrdenEncabezado)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (info is null)
                return null;

            if (info.TieneFactura)
                throw new ConflictoNegocioException(
                    "La orden ya fue facturada y no admite cambios.");

            return info.Estado;
        }
        //[FIN][31/8/2026][Rodriale][Una orden ya facturada es intocable]

        //[INICIO][31/8/2026][Rodriale][Además de no estar facturada, para tocar líneas la orden no puede estar finalizada: primero se reabre]
        private async Task<EstadoOrdenTrabajo?> EstaAbiertaParaEditarLineasAsync(
            int ordenId,
            CancellationToken cancellationToken)
        {
            var estado = await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken);

            if (estado is null)
                return null;

            if (estado == EstadoOrdenTrabajo.Finalizado)
                throw new ConflictoNegocioException(
                    "La orden está finalizada; reábrela antes de modificar sus líneas.");

            return estado;
        }
        //[FIN][31/8/2026][Rodriale][Para tocar líneas la orden no puede estar finalizada]

        //[INICIO][31/8/2026][Rodriale][El total siempre se recalcula desde las líneas, nunca se va sumando a mano: así no se desincroniza]
        private async Task RecalcularTotalAsync(int ordenId, CancellationToken cancellationToken)
        {
            var total = await db.OrdenesTrabajoDetalle
                .Where(d => d.OrdenTrabajoId == ordenId)
                .SumAsync(d => (decimal?)d.SubtotalEstimado, cancellationToken) ?? 0m;

            await db.OrdenesTrabajoEncabezado
                .Where(o => o.IdOrdenEncabezado == ordenId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.TotalEstimado, total), cancellationToken);
        }
        //[FIN][31/8/2026][Rodriale][El total siempre se recalcula desde las líneas]

        //[INICIO][31/8/2026][Rodriale][Trae de un solo golpe los repuestos pedidos y reclama si alguno no existe o está dado de baja]
        private async Task<Dictionary<int, (string Nombre, decimal PrecioUnitario)>> CargarRepuestosAsync(
            IReadOnlyList<CrearLineaOrdenRequest> lineas,
            CancellationToken cancellationToken)
        {
            var ids = lineas.Where(l => l.RepuestoId.HasValue)
                            .Select(l => l.RepuestoId!.Value)
                            .Distinct()
                            .ToList();

            if (ids.Count == 0)
                return [];

            var encontrados = await db.Repuestos
                .AsNoTracking()
                .Where(r => ids.Contains(r.IdRepuesto))
                .Select(r => new { r.IdRepuesto, r.Nombre, r.PrecioUnitario, r.Activo })
                .ToListAsync(cancellationToken);

            var inactivo = encontrados.FirstOrDefault(r => !r.Activo);
            if (inactivo is not null)
                throw new ConflictoNegocioException(
                    $"El repuesto '{inactivo.Nombre}' está dado de baja y no puede agregarse a una orden.");

            var faltantes = ids.Except(encontrados.Select(r => r.IdRepuesto)).ToList();
            if (faltantes.Count > 0)
                throw new ConflictoNegocioException(
                    $"No existen los repuestos con id: {string.Join(", ", faltantes)}.");

            return encontrados.ToDictionary(r => r.IdRepuesto, r => (r.Nombre, r.PrecioUnitario));
        }
        //[FIN][31/8/2026][Rodriale][Carga de repuestos pedidos]

        //[INICIO][31/8/2026][Rodriale][Lo mismo que arriba pero con la mano de obra; el servicio no tiene stock, solo tarifa]
        private async Task<Dictionary<int, (string Nombre, decimal TarifaManoObra)>> CargarServiciosAsync(
            IReadOnlyList<CrearLineaOrdenRequest> lineas,
            CancellationToken cancellationToken)
        {
            var ids = lineas.Where(l => l.ServicioId.HasValue)
                            .Select(l => l.ServicioId!.Value)
                            .Distinct()
                            .ToList();

            if (ids.Count == 0)
                return [];

            var encontrados = await db.Servicios
                .AsNoTracking()
                .Where(s => ids.Contains(s.IdServicio))
                .Select(s => new { s.IdServicio, s.Nombre, s.TarifaManoObra, s.Activo })
                .ToListAsync(cancellationToken);

            var inactivo = encontrados.FirstOrDefault(s => !s.Activo);
            if (inactivo is not null)
                throw new ConflictoNegocioException(
                    $"El servicio '{inactivo.Nombre}' está dado de baja y no puede agregarse a una orden.");

            var faltantes = ids.Except(encontrados.Select(s => s.IdServicio)).ToList();
            if (faltantes.Count > 0)
                throw new ConflictoNegocioException(
                    $"No existen los servicios con id: {string.Join(", ", faltantes)}.");

            return encontrados.ToDictionary(s => s.IdServicio, s => (s.Nombre, s.TarifaManoObra));
        }
        //[FIN][31/8/2026][Rodriale][Carga de servicios pedidos]

        //[INICIO][31/8/2026][Rodriale][El descuento va en un UPDATE con condición: si no alcanzan las existencias afecta 0 filas y ahí mismo se corta. Así dos órdenes al mismo tiempo no pueden vender la misma pieza]
        private async Task DescontarStockAsync(
            IReadOnlyList<CrearLineaOrdenRequest> lineas,
            Dictionary<int, (string Nombre, decimal PrecioUnitario)> repuestos,
            CancellationToken cancellationToken)
        {
            //[INICIO][31/8/2026][Rodriale][Si el mismo repuesto viene en varias líneas, se suma primero para no hacer un UPDATE por línea]
            var consumoPorRepuesto = lineas
                .Where(l => l.RepuestoId.HasValue)
                .GroupBy(l => l.RepuestoId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Cantidad));
            //[FIN][31/8/2026][Rodriale][Si el mismo repuesto viene en varias líneas, se suma primero]

            foreach (var (repuestoId, cantidad) in consumoPorRepuesto)
            {
                var filasAfectadas = await db.Repuestos
                    .Where(r => r.IdRepuesto == repuestoId && r.CantidadStock >= cantidad)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.CantidadStock, r => r.CantidadStock - cantidad),
                        cancellationToken);

                if (filasAfectadas == 0)
                    throw new ConflictoNegocioException(
                        $"Stock insuficiente del repuesto '{repuestos[repuestoId].Nombre}': se solicitaron {cantidad} unidades.");
            }
        }
        //[FIN][31/8/2026][Rodriale][Descuento de stock con UPDATE condicionado]
    }
}
//[FIN][31/8/2026][Rodriale][Servicio de órdenes de trabajo]
