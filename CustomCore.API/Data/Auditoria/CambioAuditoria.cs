//[INICIO][2/10/2026][jgarciad8][Captura explícita de campos auditables del módulo de seguridad]
using System.Text.Json;
//[INICIO][5/10/2026][jgarciad8][Estados legibles en las instantáneas JSON]
using System.Text.Json.Serialization;
//[FIN][5/10/2026][jgarciad8][Estados legibles en las instantáneas JSON]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CustomCore.API.Data.Auditoria;

internal sealed class CambioAuditoria
{

    private static readonly Dictionary<Type, string[]> Campos = new()
    {
        [typeof(Usuario)] = [nameof(Usuario.IdUsuario), nameof(Usuario.NombreCompleto),
            nameof(Usuario.Correo), nameof(Usuario.Activo)],
        [typeof(Rol)] = [nameof(Rol.IdRol), nameof(Rol.Nombre), nameof(Rol.Descripcion)],
        [typeof(Permiso)] = [nameof(Permiso.IdPermiso), nameof(Permiso.NombreCodigo),
            nameof(Permiso.Descripcion), nameof(Permiso.Modulo)],
        [typeof(UsuarioRol)] = [nameof(UsuarioRol.UsuarioId), nameof(UsuarioRol.RolId)],
        [typeof(RolPermiso)] = [nameof(RolPermiso.RolId), nameof(RolPermiso.PermisoId)],
        //[INICIO][5/10/2026][jgarciad8][Campos permitidos del taller y facturación; sin datos de tarjeta ni secretos Stripe]
        [typeof(Cliente)] = [
            nameof(Cliente.IdCliente), nameof(Cliente.NombreCompleto), nameof(Cliente.Telefono),
            nameof(Cliente.Correo), nameof(Cliente.Nit)],
        [typeof(Vehiculo)] = [
            nameof(Vehiculo.IdVehiculo), nameof(Vehiculo.ClienteId), nameof(Vehiculo.Placa),
            nameof(Vehiculo.Marca), nameof(Vehiculo.Modelo), nameof(Vehiculo.Anio)],
        [typeof(Servicio)] = [nameof(Servicio.IdServicio), nameof(Servicio.Nombre), nameof(Servicio.TarifaManoObra), nameof(Servicio.Activo)],
        [typeof(Repuesto)] = [
            nameof(Repuesto.IdRepuesto), nameof(Repuesto.CodigoStock), nameof(Repuesto.Nombre),
            nameof(Repuesto.PrecioUnitario), nameof(Repuesto.CantidadStock), nameof(Repuesto.Activo)],
        [typeof(Cita)] = [
            nameof(Cita.IdCita), nameof(Cita.VehiculoId), nameof(Cita.FechaHora),
            nameof(Cita.DuracionMinutos), nameof(Cita.Motivo), nameof(Cita.Estado)],
        [typeof(OrdenTrabajoEncabezado)] = [
            nameof(OrdenTrabajoEncabezado.IdOrdenEncabezado), nameof(OrdenTrabajoEncabezado.VehiculoId), nameof(OrdenTrabajoEncabezado.FechaIngreso),
            nameof(OrdenTrabajoEncabezado.FechaFinalizacion), nameof(OrdenTrabajoEncabezado.Diagnostico), nameof(OrdenTrabajoEncabezado.Estado),
            nameof(OrdenTrabajoEncabezado.TotalEstimado), nameof(OrdenTrabajoEncabezado.UsuarioRecepcionId), nameof(OrdenTrabajoEncabezado.MecanicoAsignadoId)],
        [typeof(OrdenTrabajoDetalle)] = [
            nameof(OrdenTrabajoDetalle.IdOrdenDetalle), nameof(OrdenTrabajoDetalle.OrdenTrabajoId), nameof(OrdenTrabajoDetalle.RepuestoId),
            nameof(OrdenTrabajoDetalle.ServicioId), nameof(OrdenTrabajoDetalle.Cantidad), nameof(OrdenTrabajoDetalle.PrecioUnitario),
            nameof(OrdenTrabajoDetalle.SubtotalEstimado)],
        [typeof(FacturaEncabezado)] = [
            nameof(FacturaEncabezado.IdFacturaEncabezado), nameof(FacturaEncabezado.OrdenTrabajoId), nameof(FacturaEncabezado.FechaEmision),
            nameof(FacturaEncabezado.ClienteId), nameof(FacturaEncabezado.NombreFacturacion), nameof(FacturaEncabezado.NitFacturacion),
            nameof(FacturaEncabezado.Subtotal), nameof(FacturaEncabezado.TasaImpuesto), nameof(FacturaEncabezado.MontoImpuesto),
            nameof(FacturaEncabezado.TotalPagado), nameof(FacturaEncabezado.StripePaymentId), nameof(FacturaEncabezado.EstadoPago),
            nameof(FacturaEncabezado.UsuarioCajeroId)],
        [typeof(FacturaDetalle)] = [
            nameof(FacturaDetalle.IdFacturaDetalle), nameof(FacturaDetalle.FacturaId), nameof(FacturaDetalle.DescripcionItem),
            nameof(FacturaDetalle.Cantidad), nameof(FacturaDetalle.PrecioUnitario), nameof(FacturaDetalle.Subtotal)]
        //[FIN][5/10/2026][jgarciad8][Campos permitidos del taller y facturación; sin datos de tarjeta ni secretos Stripe]
    };

    private readonly EntityEntry entrada;
    private readonly EntityState estado;
    private readonly string? anteriores;

    public static bool EsAuditable(EntityEntry entrada) =>
        Campos.ContainsKey(entrada.Metadata.ClrType)
        && entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    //[INICIO][5/10/2026][jgarciad8][Instantáneas leídas bajo bloqueo y comparación para evitar logs sin cambios]
    internal EntityEntry Entrada => entrada;
    internal bool Eliminacion => estado == EntityState.Deleted;
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    internal static string Serializar(Type tipo, PropertyValues valores) =>
        JsonSerializer.Serialize(Campos[tipo].ToDictionary(nombre => nombre, nombre => valores[nombre]), OpcionesJson);

    public CambioAuditoria(EntityEntry entrada, PropertyValues? originales = null)
    {
        this.entrada = entrada;
        estado = entrada.State;
        anteriores = estado == EntityState.Added ? null : Serializar(entrada.Metadata.ClrType, originales ?? entrada.OriginalValues);
    }

    public LogAccion? CrearLog(int? usuarioId, DateTimeOffset fecha, PropertyValues? actuales = null)
    {
        var nuevos = estado == EntityState.Deleted ? null : Serializar(entrada.Metadata.ClrType, actuales ?? entrada.CurrentValues);
        if (estado == EntityState.Modified && anteriores == nuevos) return null;
     
        var llave = JsonSerializer.Serialize(entrada.Metadata.FindPrimaryKey()!.Properties
            .ToDictionary(p => p.Name, p => estado == EntityState.Deleted
                ? entrada.Property(p.Name).OriginalValue : entrada.Property(p.Name).CurrentValue));
        if (llave.Length > 50)
            throw new InvalidOperationException("La llave de auditoría supera el tamaño de LogsAcciones.");

        return new LogAccion
        {
            UsuarioId = usuarioId,
            TablaAfectada = entrada.Metadata.GetTableName()!,
            Accion = estado switch
            {
                EntityState.Added => AccionLog.Insert,
                EntityState.Modified => AccionLog.Update,
                _ => AccionLog.Delete
            },
            LlavePrimaria = llave,
            ValoresAnteriores = anteriores,
            ValoresNuevos = nuevos,
            FechaHora = fecha
        };
    }
    //[FIN][5/10/2026][jgarciad8][Instantáneas leídas bajo bloqueo y comparación para evitar logs sin cambios]
}
//[FIN][2/10/2026][jgarciad8][Captura explícita de campos auditables del módulo de seguridad]
