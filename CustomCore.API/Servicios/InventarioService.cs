//[INICIO][12/10/2026][Rodriale][Servicio de inventario por sucursal: consultar y fijar existencias]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class InventarioService(AppDbContext db, SucursalActual sucursal)
    {
        //[INICIO][12/10/2026][Rodriale][Catálogo activo con la cantidad de la sede en una sola consulta; sin fila cuenta como 0]
        public async Task<ResultadoPaginado<InventarioDto>?> ObtenerAsync(
            int sucursalId,
            ConsultaPaginada consulta,
            CancellationToken cancellationToken)
        {
            await ValidarAccesoAsync(sucursalId, cancellationToken);

            if (!await db.Sucursales.AsNoTracking().AnyAsync(s => s.IdSucursal == sucursalId, cancellationToken))
                return null;

            var query = db.Repuestos.AsNoTracking().Where(r => r.Activo);

            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(r =>
                    EF.Functions.ILike(r.CodigoStock, patron) ||
                    EF.Functions.ILike(r.Nombre, patron));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(r => r.Nombre)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(r => new InventarioDto(
                    r.IdRepuesto,
                    r.CodigoStock,
                    r.Nombre,
                    r.PrecioUnitario,
                    db.InventariosSucursal
                        .Where(i => i.SucursalId == sucursalId && i.RepuestoId == r.IdRepuesto)
                        .Select(i => i.Cantidad)
                        .FirstOrDefault()))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<InventarioDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }
        //[FIN][12/10/2026][Rodriale][Catálogo activo con la cantidad de la sede]

        //[INICIO][12/10/2026][Rodriale][Fija la existencia: actualiza con auditoría si hay fila, la crea si es la primera vez. false = sede o repuesto inexistente]
        public async Task<bool> FijarAsync(
            int sucursalId,
            int repuestoId,
            int cantidad,
            CancellationToken cancellationToken)
        {
            await ValidarAccesoAsync(sucursalId, cancellationToken);

            var repuestoActivo = await db.Repuestos
                .AsNoTracking()
                .Where(r => r.IdRepuesto == repuestoId)
                .Select(r => (bool?)r.Activo)
                .FirstOrDefaultAsync(cancellationToken);

            if (repuestoActivo is null ||
                !await db.Sucursales.AsNoTracking().AnyAsync(s => s.IdSucursal == sucursalId, cancellationToken))
                return false;

            if (repuestoActivo == false)
                throw new ConflictoNegocioException("El repuesto está dado de baja; no se le asignan existencias.");

            var idInventario = await db.InventariosSucursal
                .AsNoTracking()
                .Where(i => i.SucursalId == sucursalId && i.RepuestoId == repuestoId)
                .Select(i => (int?)i.IdInventario)
                .FirstOrDefaultAsync(cancellationToken);

            if (idInventario is { } id)
            {
                await db.InventariosSucursal
                    .Where(i => i.IdInventario == id)
                    .ActualizarAuditadoAsync(db, id, s => s.SetProperty(i => i.Cantidad, cantidad), cancellationToken);

                return true;
            }

            //[INICIO][12/10/2026][Rodriale][Si dos ajustes crean la misma fila a la vez, el índice único rechaza el segundo con 409]
            db.InventariosSucursal.Add(new InventarioSucursal
            {
                SucursalId = sucursalId,
                RepuestoId = repuestoId,
                Cantidad = cantidad
            });
            await db.SaveChangesAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Si dos ajustes crean la misma fila a la vez]

            return true;
        }
        //[FIN][12/10/2026][Rodriale][Fija la existencia]

        //[INICIO][12/10/2026][Rodriale][La sede viene explícita en la ruta: pedir otra es un 403 claro, no un 404]
        private async Task ValidarAccesoAsync(int sucursalId, CancellationToken cancellationToken)
        {
            if (await sucursal.AlcanceAsync(cancellationToken) is { } propia && propia != sucursalId)
                throw new AccesoDenegadoException("Solo puedes ver o ajustar el inventario de tu sucursal.");
        }
        //[FIN][12/10/2026][Rodriale][La sede viene explícita en la ruta]
    }
}
//[FIN][12/10/2026][Rodriale][Servicio de inventario por sucursal]
