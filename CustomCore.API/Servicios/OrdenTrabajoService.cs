//[INICIO][31/8/2026][Rodriale][Servicio de órdenes de trabajo: aquí vive la parte pesada del taller, crear la orden sin dejar el inventario descuadrado]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class OrdenTrabajoService(AppDbContext db, SucursalActual sucursal, NotificacionWhatsAppService whatsApp)
    {
        //[INICIO][12/10/2026][Rodriale][Lo que hace falta saber de una orden antes de tocarla]
        internal sealed record OrdenEditable(EstadoOrdenTrabajo Estado, int SucursalId, int? MecanicoAsignadoId);
        //[FIN][12/10/2026][Rodriale][Lo que hace falta saber de una orden antes de tocarla]

        //[INICIO][31/8/2026][Rodriale][Listado del tablero: filtros opcionales que se van encadenando solo si vienen]
        public async Task<ResultadoPaginado<OrdenListaDto>> ObtenerAsync(
            ConsultaOrdenes consulta,
            CancellationToken cancellationToken)
        {
            var query = await FiltrarAsync(consulta, cancellationToken);

            if (consulta.Estado is { } estado)
                query = query.Where(o => o.Estado == estado);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(o => o.FechaIngreso)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(o => new OrdenListaDto(
                    o.IdOrdenEncabezado,
                    o.SucursalId,
                    o.VehiculoId,
                    o.Vehiculo.Placa,
                    o.Vehiculo.Cliente.NombreCompleto,
                    o.FechaIngreso,
                    o.FechaFinalizacion,
                    o.Estado,
                    o.TotalEstimado,
                    o.MecanicoAsignado != null ? o.MecanicoAsignado.NombreCompleto : null,
                    o.Cita != null && o.Cita.Servicio != null ? o.Cita.Servicio.Nombre : null))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<OrdenListaDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }
        //[FIN][31/8/2026][Rodriale][Listado del tablero]

        //[INICIO][26/10/2026][Rodriale][Cuántas órdenes hay en cada estado con los mismos filtros del listado, para los contadores del filtro]
        public async Task<ConteoOrdenesPorEstadoDto> ContarPorEstadoAsync(
            ConsultaOrdenes consulta,
            CancellationToken cancellationToken)
        {
            var query = await FiltrarAsync(consulta, cancellationToken);

            var conteos = await query
                .GroupBy(o => o.Estado)
                .Select(g => new { Estado = g.Key, Total = g.Count() })
                .ToDictionaryAsync(g => g.Estado, g => g.Total, cancellationToken);

            return new ConteoOrdenesPorEstadoDto(
                conteos.GetValueOrDefault(EstadoOrdenTrabajo.EnRecepcion),
                conteos.GetValueOrDefault(EstadoOrdenTrabajo.EnProceso),
                conteos.GetValueOrDefault(EstadoOrdenTrabajo.Finalizado));
        }
        //[FIN][26/10/2026][Rodriale][Cuántas órdenes hay en cada estado]

        //[INICIO][26/10/2026][Rodriale][Filtros comunes del listado y del conteo; el estado lo aplica cada quien]
        private async Task<IQueryable<OrdenTrabajoEncabezado>> FiltrarAsync(
            ConsultaOrdenes consulta,
            CancellationToken cancellationToken)
        {
            var query = db.OrdenesTrabajoEncabezado.AsNoTracking();

            //[INICIO][12/10/2026][Rodriale][El empleado solo ve su sede aunque pida otra; quien ve todas puede filtrar]
            var filtroSucursal = await sucursal.AlcanceAsync(cancellationToken) ?? consulta.SucursalId;
            if (filtroSucursal is { } sucursalId)
                query = query.Where(o => o.SucursalId == sucursalId);
            //[FIN][12/10/2026][Rodriale][El empleado solo ve su sede]

            //[INICIO][25/10/2026][Rodriale][El mecánico restringido solo ve las suyas aunque no lo pida]
            var mecanico = await sucursal.MecanicoRestringidoAsync(cancellationToken);
            if (mecanico is null && consulta.AsignadasAMi)
                mecanico = await sucursal.UsuarioIdAsync(cancellationToken);
            if (mecanico is { } idMecanico)
                query = query.Where(o => o.MecanicoAsignadoId == idMecanico);
            //[FIN][25/10/2026][Rodriale][El mecánico restringido solo ve las suyas]

            if (consulta.VehiculoId is { } vehiculoId)
                query = query.Where(o => o.VehiculoId == vehiculoId);

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

            return query;
        }
        //[FIN][26/10/2026][Rodriale][Filtros comunes del listado y del conteo]

        //[INICIO][31/8/2026][Rodriale][Detalle completo en una sola consulta: orden, vehículo, dueño, responsables y líneas, sin caer en N+1]
        public async Task<OrdenDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
        {
            //[INICIO][12/10/2026][Rodriale][Una orden de otra sede se comporta como inexistente]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Una orden de otra sede se comporta como inexistente]
            //[INICIO][25/10/2026][Rodriale][La orden de otro mecánico también se comporta como inexistente]
            var mecanico = await sucursal.MecanicoRestringidoAsync(cancellationToken);
            //[FIN][25/10/2026][Rodriale][La orden de otro mecánico también se comporta como inexistente]

            return await db.OrdenesTrabajoEncabezado
                .AsNoTracking()
                .Where(o => o.IdOrdenEncabezado == id && (alcance == null || o.SucursalId == alcance))
                .Where(o => mecanico == null || o.MecanicoAsignadoId == mecanico)
                .Select(o => new OrdenDetalleDto(
                    o.IdOrdenEncabezado,
                    o.SucursalId,
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
                            //[INICIO][26/10/2026][Rodriale][La línea adicional viene de una cotización aceptada]
                            d.RepuestoId != null ? "Repuesto" : d.ServicioId != null ? "Servicio" : "Adicional",
                            d.RepuestoId,
                            d.ServicioId,
                            d.RepuestoId != null ? d.Repuesto!.Nombre : d.ServicioId != null ? d.Servicio!.Nombre : d.Descripcion!,
                            //[FIN][26/10/2026][Rodriale][La línea adicional viene de una cotización aceptada]
                            d.Cantidad,
                            d.PrecioUnitario,
                            d.SubtotalEstimado))
                        .ToList(),
                    o.CitaId,
                    o.MotivoIngreso,
                    o.MecanicoAsignadoId,
                    o.Fotos
                        .OrderBy(f => f.FechaSubida)
                        .Select(f => new FotoOrdenDto(f.IdFotoOrden, f.Url, f.Descripcion, f.FechaSubida))
                        .ToList()))
                .FirstOrDefaultAsync(cancellationToken);
        }
        //[FIN][31/8/2026][Rodriale][Detalle completo en una sola consulta]

        //[INICIO][31/8/2026][Rodriale][Crear orden: es todo o nada, si una línea se cae por stock no queda ni la orden ni el descuento]
        public async Task<int> CrearAsync(CrearOrdenRequest request, CancellationToken cancellationToken)
        {
            //[INICIO][31/8/2026][Rodriale][Como activamos EnableRetryOnFailure, EF exige que las transacciones manuales vayan envueltas en la estrategia de reintento; si no, revienta al abrir la transacción]
            //[INICIO][5/10/2026][jgarciad8][Unidad completa con verificación de COMMIT y restauración para reintentos]
            var idOrden = await db.EjecutarUnidadAuditadaAsync(() => CrearInternoAsync(request, cancellationToken), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Unidad completa con verificación de COMMIT y restauración para reintentos]
            //[FIN][31/8/2026][Rodriale][Transacción manual envuelta en la estrategia de reintento]

            //[INICIO][26/10/2026][Rodriale][El aviso sale después del COMMIT, para no avisar de una orden que se deshizo]
            await whatsApp.VehiculoRecibidoAsync(idOrden);
            //[FIN][26/10/2026][Rodriale][El aviso sale después del COMMIT]

            return idOrden;
        }

        private async Task<int> CrearInternoAsync(CrearOrdenRequest request, CancellationToken cancellationToken)
        {
            //[INICIO][25/10/2026][Rodriale][Toda orden nace de una cita: se bloquea primero y de ella salen el carro y la sede]
            var cita = await ValidarCitaAsync(request.CitaId, cancellationToken);
            //[FIN][25/10/2026][Rodriale][Toda orden nace de una cita]

            //[INICIO][12/10/2026][Rodriale][La sede se decide antes de mover stock: de ella sale lo que se descuenta]
            var sucursalId = await sucursal.ResolverAsync(cita.SucursalId, cancellationToken);
            //[FIN][12/10/2026][Rodriale][La sede se decide antes de mover stock]

            await ValidarMecanicoAsync(request.MecanicoAsignadoId, sucursalId, cancellationToken);

            //[INICIO][25/10/2026][Rodriale][El servicio de la cita entra solo como primera línea, si sigue activo y no viene ya en el request]
            var lineas = new List<CrearLineaOrdenRequest>();
            if (cita.ServicioId is { } servicioCita
                && !(request.Lineas ?? []).Any(l => l.ServicioId == servicioCita)
                && await db.Servicios.AsNoTracking().AnyAsync(s => s.IdServicio == servicioCita && s.Activo, cancellationToken))
            {
                lineas.Add(new CrearLineaOrdenRequest(null, servicioCita, 1));
            }
            lineas.AddRange(request.Lineas ?? []);
            //[FIN][25/10/2026][Rodriale][El servicio de la cita entra solo como primera línea]

            //[INICIO][31/8/2026][Rodriale][Primero se valida que todo lo pedido exista y esté activo, antes de mover una sola pieza de bodega]
            var repuestos = await CargarRepuestosAsync(lineas, cancellationToken);
            var servicios = await CargarServiciosAsync(lineas, cancellationToken);
            //[FIN][31/8/2026][Rodriale][Primero se valida que todo exista y esté activo]

            await DescontarStockAsync(sucursalId, lineas, repuestos, cancellationToken);

            var orden = new OrdenTrabajoEncabezado
            {
                VehiculoId = cita.VehiculoId,
                SucursalId = sucursalId,
                UsuarioRecepcionId = request.UsuarioRecepcionId,
                MecanicoAsignadoId = request.MecanicoAsignadoId,
                FechaIngreso = DateTimeOffset.UtcNow,
                MotivoIngreso = string.IsNullOrWhiteSpace(request.MotivoIngreso) ? null : request.MotivoIngreso.Trim(),
                Estado = EstadoOrdenTrabajo.EnRecepcion,
                CitaId = cita.IdCita
            };

            //[INICIO][31/8/2026][Rodriale][El precio sale del catálogo, no del request: así nadie cotiza al precio que se le antoje, y queda congelado por si mañana sube]
            foreach (var linea in lineas)
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

            //[INICIO][27/10/2026][Rodriale][Lo cotizado y aceptado antes de la visita entra a la orden al precio acordado]
            var cotizado = await db.Cotizaciones.AsNoTracking()
                .Where(c => c.CitaId == cita.IdCita && c.Estado == EstadoCotizacion.Aceptada)
                .SelectMany(c => c.Detalles)
                .OrderBy(d => d.IdCotizacionDetalle)
                .ToListAsync(cancellationToken);

            foreach (var detalle in cotizado)
            {
                orden.Detalles.Add(new OrdenTrabajoDetalle
                {
                    Descripcion = detalle.Descripcion,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    SubtotalEstimado = detalle.Subtotal
                });
            }
            //[FIN][27/10/2026][Rodriale][Lo cotizado antes de la visita entra a la orden]

            orden.TotalEstimado = orden.Detalles.Sum(d => d.SubtotalEstimado);

            db.OrdenesTrabajoEncabezado.Add(orden);
            await db.SaveChangesAsync(cancellationToken);

            //[INICIO][24/10/2026][Rodriale][Si el carro ya llegó, la cita se dio: se confirma sola para que el seguimiento no se quede en "agendada"]
            if (cita is { Estado: EstadoCita.Pendiente })
            {
                await db.Citas
                    .Where(c => c.IdCita == cita.IdCita)
                    .ActualizarAuditadoAsync(db, cita.IdCita, s => s
                        .SetProperty(c => c.Estado, EstadoCita.Confirmada),
                        cancellationToken);
            }
            //[FIN][24/10/2026][Rodriale][Si el carro ya llegó, la cita se dio]

            return orden.IdOrdenEncabezado;
        }
        //[FIN][31/8/2026][Rodriale][Crear orden: es todo o nada]

        //[INICIO][24/10/2026][Rodriale][La cita debe seguir vigente y no tener ya otra orden; la sede la valida ResolverAsync]
        private async Task<Cita> ValidarCitaAsync(
            int citaId,
            CancellationToken cancellationToken)
        {
            var cita = await db.BloquearFilaAsync<Cita>(citaId, cancellationToken)
                ?? throw new ConflictoNegocioException("La cita no existe.");

            if (cita.Estado == EstadoCita.Cancelada)
                throw new ConflictoNegocioException("La cita está cancelada; no se le puede abrir una orden.");

            //[INICIO][27/10/2026][Rodriale][Fuera del catálogo, el vehículo se recibe solo con la cotización aceptada (la cita ya confirmada)]
            if (cita is { ServicioPersonalizado: true, Estado: EstadoCita.Pendiente })
                throw new ConflictoNegocioException("Primero el cliente debe aceptar la cotización de esta cita.");
            //[FIN][27/10/2026][Rodriale][Fuera del catálogo, se recibe con la cotización aceptada]

            if (await db.OrdenesTrabajoEncabezado.AsNoTracking()
                    .AnyAsync(o => o.CitaId == citaId, cancellationToken))
                throw new ConflictoNegocioException("Esa cita ya tiene una orden de trabajo.");

            return cita;
        }
        //[FIN][24/10/2026][Rodriale][La cita debe seguir vigente y no tener ya otra orden]

        //[INICIO][31/8/2026][Rodriale][Actualizar el encabezado: notas del mecánico y a quién se le asigna. No toca líneas ni totales]
        public Task<bool> ActualizarAsync(
            int ordenId,
            ActualizarOrdenRequest request,
            CancellationToken cancellationToken)
            => db.EjecutarUnidadAuditadaAsync<bool>(async () =>
        {
            //[INICIO][5/10/2026][jgarciad8][Validación y cambio bajo el mismo bloqueo transaccional]
            await db.BloquearFilaAsync<OrdenTrabajoEncabezado>(ordenId, cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Validación y cambio bajo el mismo bloqueo transaccional]

            var orden = await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken);
            if (orden is null)
                return false;

            //[INICIO][12/10/2026][Rodriale][Solo se valida si cambia: un mecánico ya asignado que luego cambió de sede no debe bloquear editar el diagnóstico]
            if (request.MecanicoAsignadoId != orden.MecanicoAsignadoId)
            {
                //[INICIO][25/10/2026][Rodriale][Reasignar es cosa de recepción; el mecánico no se quita ni se pasa órdenes]
                if (await sucursal.MecanicoRestringidoAsync(cancellationToken) is not null)
                    throw new AccesoDenegadoException("Solo recepción puede cambiar el mecánico asignado.");
                //[FIN][25/10/2026][Rodriale][Reasignar es cosa de recepción]
                await ValidarMecanicoAsync(request.MecanicoAsignadoId, orden.SucursalId, cancellationToken);
            }
            //[FIN][12/10/2026][Rodriale][Solo se valida si cambia]

            await db.OrdenesTrabajoEncabezado
                .Where(o => o.IdOrdenEncabezado == ordenId)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, ordenId, s => s
                    .SetProperty(o => o.MecanicoAsignadoId, request.MecanicoAsignadoId)
                    .SetProperty(o => o.Diagnostico, request.Diagnostico),
                    cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return true;
        }, cancellationToken);
        //[FIN][31/8/2026][Rodriale][Actualizar el encabezado]

        //[INICIO][25/10/2026][Rodriale][Solo el diagnóstico: el mecánico lo escribe sin poder tocar la asignación]
        public Task<bool> ActualizarDiagnosticoAsync(
            int ordenId,
            ActualizarDiagnosticoRequest request,
            CancellationToken cancellationToken)
            => db.EjecutarUnidadAuditadaAsync<bool>(async () =>
        {
            await db.BloquearFilaAsync<OrdenTrabajoEncabezado>(ordenId, cancellationToken);

            if (await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken) is null)
                return false;

            var diagnostico = string.IsNullOrWhiteSpace(request.Diagnostico) ? null : request.Diagnostico.Trim();

            await db.OrdenesTrabajoEncabezado
                .Where(o => o.IdOrdenEncabezado == ordenId)
                .ActualizarAuditadoAsync(db, ordenId, s => s.SetProperty(o => o.Diagnostico, diagnostico), cancellationToken);

            return true;
        }, cancellationToken);
        //[FIN][25/10/2026][Rodriale][Solo el diagnóstico]

        //[INICIO][29/10/2026][Rodriale][Fotos del diagnóstico: mismas reglas que el texto, la orden no puede estar facturada]
        private const int MaximoFotosPorOrden = 20;

        public Task<int?> RegistrarFotoAsync(
            int ordenId,
            RegistrarFotoOrdenRequest request,
            int usuarioId,
            CancellationToken cancellationToken)
            => db.EjecutarUnidadAuditadaAsync<int?>(async () =>
        {
            // Solo se aceptan archivos de Vercel Blob: así nadie registra una URL externa que el cliente terminaría abriendo.
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var url)
                || url.Scheme != Uri.UriSchemeHttps
                || !url.Host.EndsWith(".public.blob.vercel-storage.com", StringComparison.OrdinalIgnoreCase))
                throw new SolicitudInvalidaException("La foto debe estar guardada en el almacenamiento del taller.");

            await db.BloquearFilaAsync<OrdenTrabajoEncabezado>(ordenId, cancellationToken);

            if (await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken) is null)
                return null;

            if (await db.FotosOrden.CountAsync(f => f.OrdenTrabajoId == ordenId, cancellationToken) >= MaximoFotosPorOrden)
                throw new ConflictoNegocioException($"La orden ya tiene {MaximoFotosPorOrden} fotos; quita alguna antes de subir otra.");

            var foto = new FotoOrden
            {
                OrdenTrabajoId = ordenId,
                Url = request.Url,
                Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
                FechaSubida = DateTimeOffset.UtcNow,
                UsuarioSubeId = usuarioId
            };

            db.FotosOrden.Add(foto);
            await db.SaveChangesAsync(cancellationToken);

            return foto.IdFotoOrden;
        }, cancellationToken);

        // Devuelve la URL de la foto quitada para que el front borre ese archivo y no uno que le mande el navegador.
        public Task<string?> EliminarFotoAsync(
            int ordenId,
            int fotoId,
            CancellationToken cancellationToken)
            => db.EjecutarUnidadAuditadaAsync<string?>(async () =>
        {
            await db.BloquearFilaAsync<OrdenTrabajoEncabezado>(ordenId, cancellationToken);

            if (await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken) is null)
                return null;

            var fotos = db.FotosOrden.Where(f => f.IdFotoOrden == fotoId && f.OrdenTrabajoId == ordenId);
            var url = await fotos.Select(f => f.Url).FirstOrDefaultAsync(cancellationToken);
            if (url is null)
                return null;

            await fotos.EliminarAuditadoAsync(db, fotoId, cancellationToken);
            return url;
        }, cancellationToken);
        //[FIN][29/10/2026][Rodriale][Fotos del diagnóstico]

        //[INICIO][25/10/2026][Rodriale][Mecánicos de la sede con sus órdenes activas: disponibles primero, luego los menos cargados]
        public async Task<IReadOnlyList<MecanicoDisponibleDto>> ObtenerMecanicosAsync(
            int? sucursalId,
            CancellationToken cancellationToken)
        {
            var sede = await sucursal.AlcanceAsync(cancellationToken) ?? sucursalId
                ?? throw new SolicitudInvalidaException("Indica la sucursal (sucursalId).");

            return await db.Usuarios
                .AsNoTracking()
                .Where(u => u.Activo && u.SucursalId == sede
                    && u.UsuarioRoles.Any(ur => ur.Rol.RolPermisos.Any(
                        rp => rp.Permiso.NombreCodigo == PermisosSistema.OrdenesTrabajar)))
                .Select(u => new
                {
                    u.IdUsuario,
                    u.NombreCompleto,
                    OrdenesActivas = db.OrdenesTrabajoEncabezado.Count(o => o.MecanicoAsignadoId == u.IdUsuario
                        && o.Estado != EstadoOrdenTrabajo.Finalizado)
                })
                .OrderBy(m => m.OrdenesActivas)
                .ThenBy(m => m.NombreCompleto)
                .Select(m => new MecanicoDisponibleDto(m.IdUsuario, m.NombreCompleto, m.OrdenesActivas))
                .ToListAsync(cancellationToken);
        }
        //[FIN][25/10/2026][Rodriale][Mecánicos de la sede con sus órdenes activas]

        //[INICIO][31/8/2026][Rodriale][Cambiar de estado; al finalizar se sella la fecha de salida, y si se reabre se limpia para no dejar una fecha mentirosa]
        public async Task<bool> CambiarEstadoAsync(
            int ordenId,
            CambiarEstadoOrdenRequest request,
            CancellationToken cancellationToken)
        {
            //[INICIO][26/10/2026][Rodriale][Se guarda de qué estado venía para avisar al cliente solo cuando de verdad avanza]
            EstadoOrdenTrabajo? anterior = null;
            //[FIN][26/10/2026][Rodriale][Se guarda de qué estado venía]

            var actualizada = await db.EjecutarUnidadAuditadaAsync<bool>(async () =>
            {
                anterior = null;
                //[INICIO][5/10/2026][jgarciad8][Validación y cambio bajo el mismo bloqueo transaccional]
                var actual = await db.BloquearFilaAsync<OrdenTrabajoEncabezado>(ordenId, cancellationToken);
                //[FIN][5/10/2026][jgarciad8][Validación y cambio bajo el mismo bloqueo transaccional]

                if (await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken) is null)
                    return false;

                //[INICIO][5/10/2026][jgarciad8][Repetir el mismo estado no cambia la fecha ni crea otro log]
                if (actual?.Estado == request.Estado) return true;
                //[FIN][5/10/2026][jgarciad8][Repetir el mismo estado no cambia la fecha ni crea otro log]
                anterior = actual?.Estado;
                var finalizacion = request.Estado == EstadoOrdenTrabajo.Finalizado
                    ? DateTimeOffset.UtcNow
                    : (DateTimeOffset?)null;

                await db.OrdenesTrabajoEncabezado
                    .Where(o => o.IdOrdenEncabezado == ordenId)
                    //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                    .ActualizarAuditadoAsync(db, ordenId, s => s
                        .SetProperty(o => o.Estado, request.Estado)
                        .SetProperty(o => o.FechaFinalizacion, finalizacion),
                        cancellationToken);
                //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

                return true;
            }, cancellationToken);

            //[INICIO][26/10/2026][Rodriale][Empezar avisa solo si venía de recepción (reabrir una terminada no repite el aviso); terminar siempre avisa]
            if (actualizada && anterior == EstadoOrdenTrabajo.EnRecepcion && request.Estado == EstadoOrdenTrabajo.EnProceso)
                await whatsApp.TrabajoIniciadoAsync(ordenId);
            else if (actualizada && anterior is not null && request.Estado == EstadoOrdenTrabajo.Finalizado)
                await whatsApp.TrabajoTerminadoAsync(ordenId);
            //[FIN][26/10/2026][Rodriale][Avisos al cambiar de estado]

            return actualizada;
        }
        //[FIN][31/8/2026][Rodriale][Cambiar de estado]

        //[INICIO][31/8/2026][Rodriale][Agregar una línea a una orden ya abierta: mismo cuidado que al crearla, transacción y descuento de stock]
        public Task<int?> AgregarLineaAsync(
            int ordenId,
            CrearLineaOrdenRequest request,
            CancellationToken cancellationToken)
        {
            //[INICIO][5/10/2026][jgarciad8][Unidad completa con verificación de COMMIT y restauración para reintentos]
            return db.EjecutarUnidadAuditadaAsync(() => AgregarLineaInternoAsync(ordenId, request, cancellationToken), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Unidad completa con verificación de COMMIT y restauración para reintentos]
        }

        private async Task<int?> AgregarLineaInternoAsync(
            int ordenId,
            CrearLineaOrdenRequest request,
            CancellationToken cancellationToken)
        {
            //[INICIO][5/10/2026][jgarciad8][Serializa detalles y total de una misma orden]
            await db.BloquearFilaAsync<OrdenTrabajoEncabezado>(ordenId, cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Serializa detalles y total de una misma orden]
            var orden = await EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken);
            if (orden is null)
                return null;

            IReadOnlyList<CrearLineaOrdenRequest> lineas = [request];

            var repuestos = await CargarRepuestosAsync(lineas, cancellationToken);
            var servicios = await CargarServiciosAsync(lineas, cancellationToken);

            //[INICIO][12/10/2026][Rodriale][El stock sale de la sede de la orden, no de la del usuario que agrega la línea]
            await DescontarStockAsync(orden.SucursalId, lineas, repuestos, cancellationToken);
            //[FIN][12/10/2026][Rodriale][El stock sale de la sede de la orden]

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

            return linea.IdOrdenDetalle;
        }
        //[FIN][31/8/2026][Rodriale][Agregar una línea a una orden ya abierta]

        //[INICIO][31/8/2026][Rodriale][Quitar una línea devuelve el repuesto a bodega; si no se repone, el inventario queda descuadrado para siempre]
        public Task<bool> QuitarLineaAsync(
            int ordenId,
            int lineaId,
            CancellationToken cancellationToken)
        {
            //[INICIO][5/10/2026][jgarciad8][Unidad completa con verificación de COMMIT y restauración para reintentos]
            return db.EjecutarUnidadAuditadaAsync(() => QuitarLineaInternoAsync(ordenId, lineaId, cancellationToken), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Unidad completa con verificación de COMMIT y restauración para reintentos]
        }

        private async Task<bool> QuitarLineaInternoAsync(
            int ordenId,
            int lineaId,
            CancellationToken cancellationToken)
        {
            //[INICIO][5/10/2026][jgarciad8][Serializa detalles y total de una misma orden]
            await db.BloquearFilaAsync<OrdenTrabajoEncabezado>(ordenId, cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Serializa detalles y total de una misma orden]
            var orden = await EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken);
            if (orden is null)
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
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .EliminarAuditadoAsync(db, lineaId, cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            //[INICIO][31/8/2026][Rodriale][Solo los repuestos regresan a bodega; la mano de obra no tiene existencias que devolver]
            if (linea.RepuestoId is { } repuestoId)
            {
                //[INICIO][12/10/2026][Rodriale][Regresa a la bodega de la sede de la orden]
                await DevolverStockAsync(orden.SucursalId, repuestoId, linea.Cantidad, cancellationToken);
                //[FIN][12/10/2026][Rodriale][Regresa a la bodega de la sede de la orden]
            }
            //[FIN][31/8/2026][Rodriale][Solo los repuestos regresan a bodega]

            await RecalcularTotalAsync(ordenId, cancellationToken);

            return true;
        }
        //[FIN][31/8/2026][Rodriale][Quitar una línea devuelve el repuesto a bodega]

        //[INICIO][31/8/2026][Rodriale][Una orden ya facturada es intocable: cambiarla alteraría un documento fiscal ya emitido]
        private async Task<OrdenEditable?> ExisteYNoEstaFacturadaAsync(
            int ordenId,
            CancellationToken cancellationToken)
        {
            //[INICIO][12/10/2026][Rodriale][Editar una orden de otra sede responde 404, igual que si no existiera]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Editar una orden de otra sede responde 404]
            //[INICIO][25/10/2026][Rodriale][El mecánico restringido no toca órdenes ajenas: 404]
            var mecanico = await sucursal.MecanicoRestringidoAsync(cancellationToken);
            //[FIN][25/10/2026][Rodriale][El mecánico restringido no toca órdenes ajenas]

            var info = await db.OrdenesTrabajoEncabezado
                .AsNoTracking()
                .Where(o => o.IdOrdenEncabezado == ordenId && (alcance == null || o.SucursalId == alcance))
                .Where(o => mecanico == null || o.MecanicoAsignadoId == mecanico)
                .Select(o => new
                {
                    o.Estado,
                    o.SucursalId,
                    o.MecanicoAsignadoId,
                    TieneFactura = db.FacturasEncabezado.Any(f => f.OrdenTrabajoId == o.IdOrdenEncabezado)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (info is null)
                return null;

            if (info.TieneFactura)
                throw new ConflictoNegocioException(
                    "La orden ya fue facturada y no admite cambios.");

            return new OrdenEditable(info.Estado, info.SucursalId, info.MecanicoAsignadoId);
        }
        //[FIN][31/8/2026][Rodriale][Una orden ya facturada es intocable]

        //[INICIO][31/8/2026][Rodriale][Además de no estar facturada, para tocar líneas la orden no puede estar finalizada: primero se reabre]
        internal async Task<OrdenEditable?> EstaAbiertaParaEditarLineasAsync(
            int ordenId,
            CancellationToken cancellationToken)
        {
            var orden = await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken);

            if (orden is null)
                return null;

            if (orden.Estado == EstadoOrdenTrabajo.Finalizado)
                throw new ConflictoNegocioException(
                    "La orden está finalizada; reábrela antes de modificar sus líneas.");

            return orden;
        }
        //[FIN][31/8/2026][Rodriale][Para tocar líneas la orden no puede estar finalizada]

        //[INICIO][31/8/2026][Rodriale][El total siempre se recalcula desde las líneas, nunca se va sumando a mano: así no se desincroniza]
        internal async Task RecalcularTotalAsync(int ordenId, CancellationToken cancellationToken)
        {
            var total = await db.OrdenesTrabajoDetalle
                .Where(d => d.OrdenTrabajoId == ordenId)
                .SumAsync(d => (decimal?)d.SubtotalEstimado, cancellationToken) ?? 0m;

            await db.OrdenesTrabajoEncabezado
                .Where(o => o.IdOrdenEncabezado == ordenId)
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, ordenId, s => s.SetProperty(o => o.TotalEstimado, total), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
        }
        //[FIN][31/8/2026][Rodriale][El total siempre se recalcula desde las líneas]

        //[INICIO][12/10/2026][Rodriale][El mecánico asignado tiene que trabajar en la sede de la orden]
        private async Task ValidarMecanicoAsync(
            int? mecanicoId,
            int sucursalId,
            CancellationToken cancellationToken)
        {
            if (mecanicoId is not { } id)
                return;

            var esDeLaSede = await db.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.IdUsuario == id && u.Activo && u.SucursalId == sucursalId
                    //[INICIO][25/10/2026][Rodriale][Solo se asigna a quien es mecánico]
                    && u.UsuarioRoles.Any(ur => ur.Rol.RolPermisos.Any(
                        rp => rp.Permiso.NombreCodigo == PermisosSistema.OrdenesTrabajar)),
                    //[FIN][25/10/2026][Rodriale][Solo se asigna a quien es mecánico]
                    cancellationToken);

            if (!esDeLaSede)
                throw new ConflictoNegocioException(
                    "El mecánico asignado no existe, está inactivo, no es mecánico o no pertenece a la sucursal de la orden.");
        }
        //[FIN][12/10/2026][Rodriale][El mecánico asignado tiene que trabajar en la sede de la orden]

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
            int sucursalId,
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

            if (consumoPorRepuesto.Count == 0)
                return;

            //[INICIO][12/10/2026][Rodriale][Una sola consulta trae las filas de inventario de la sede; si un repuesto no tiene fila, en esa sede hay 0]
            var ids = consumoPorRepuesto.Keys.ToList();
            var inventario = await db.InventariosSucursal
                .AsNoTracking()
                .Where(i => i.SucursalId == sucursalId && ids.Contains(i.RepuestoId))
                .ToDictionaryAsync(i => i.RepuestoId, i => i.IdInventario, cancellationToken);
            //[FIN][12/10/2026][Rodriale][Una sola consulta trae las filas de inventario de la sede]

            //[INICIO][5/10/2026][jgarciad8][Orden estable de bloqueos de inventario]
            foreach (var (repuestoId, cantidad) in consumoPorRepuesto.OrderBy(p => p.Key))
            //[FIN][5/10/2026][jgarciad8][Orden estable de bloqueos de inventario]
            {
                var filasAfectadas = 0;

                if (inventario.TryGetValue(repuestoId, out var idInventario))
                {
                    filasAfectadas = await db.InventariosSucursal
                        .Where(i => i.IdInventario == idInventario && i.Cantidad >= cantidad)
                        //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                        .ActualizarAuditadoAsync(db, idInventario, s => s
                            .SetProperty(i => i.Cantidad, i => i.Cantidad - cantidad),
                            cancellationToken);
                    //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                }

                if (filasAfectadas == 0)
                    throw new ConflictoNegocioException(
                        $"Stock insuficiente del repuesto '{repuestos[repuestoId].Nombre}' en esta sucursal: se solicitaron {cantidad} unidades.");
            }
        }
        //[FIN][31/8/2026][Rodriale][Descuento de stock con UPDATE condicionado]

        //[INICIO][12/10/2026][Rodriale][La fila existe: el descuento que se revierte la exigió]
        private async Task DevolverStockAsync(
            int sucursalId,
            int repuestoId,
            int cantidad,
            CancellationToken cancellationToken)
        {
            var idInventario = await db.InventariosSucursal
                .AsNoTracking()
                .Where(i => i.SucursalId == sucursalId && i.RepuestoId == repuestoId)
                .Select(i => i.IdInventario)
                .FirstAsync(cancellationToken);

            await db.InventariosSucursal
                .Where(i => i.IdInventario == idInventario)
                .ActualizarAuditadoAsync(db, idInventario, s => s
                    .SetProperty(i => i.Cantidad, i => i.Cantidad + cantidad),
                    cancellationToken);
        }
        //[FIN][12/10/2026][Rodriale][La fila existe]
    }
}
//[FIN][31/8/2026][Rodriale][Servicio de órdenes de trabajo]
