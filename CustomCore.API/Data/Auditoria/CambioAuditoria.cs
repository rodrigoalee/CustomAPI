//[INICIO][2/10/2026][jgarciad8][Captura explícita de campos auditables del módulo de seguridad]
using System.Text.Json;
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
        [typeof(RolPermiso)] = [nameof(RolPermiso.RolId), nameof(RolPermiso.PermisoId)]
    };

    private readonly EntityEntry entrada;
    private readonly EntityState estado;
    private readonly string[] campos;
    private readonly string? anteriores;

    public static bool EsAuditable(EntityEntry entrada) =>
        Campos.ContainsKey(entrada.Metadata.ClrType)
        && entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    public CambioAuditoria(EntityEntry entrada)
    {
        this.entrada = entrada;
        estado = entrada.State;
        campos = Campos[entrada.Metadata.ClrType];
        anteriores = estado == EntityState.Added ? null : Serializar(originales: true);
    }

    private string Serializar(bool originales) => JsonSerializer.Serialize(
        campos.ToDictionary(nombre => nombre, nombre => originales
            ? entrada.Property(nombre).OriginalValue : entrada.Property(nombre).CurrentValue));

    public LogAccion CrearLog(int? usuarioId, DateTimeOffset fecha)
    {
     
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
            ValoresNuevos = estado == EntityState.Deleted ? null : Serializar(originales: false),
            FechaHora = fecha
        };
    }
}
//[FIN][2/10/2026][jgarciad8][Captura explícita de campos auditables del módulo de seguridad]
