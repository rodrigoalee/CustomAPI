//[INICIO][5/10/2026][jgarciad8][Auditoría explícita de UPDATE y DELETE sin sustituir sus condiciones SQL]
using System.Linq.Expressions;
using System.Text.Json;
using CustomCore.API.Data.Auditoria;
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace CustomCore.API.Data;

public static class OperacionesAuditadas
{
    public static Task<int> ActualizarAuditadoAsync<T>(this IQueryable<T> consulta, AppDbContext db,
        int id, Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> cambios,
        CancellationToken ct, bool cambioPassword = false) where T : class
    {
        consulta = LimitarFila(consulta, db, id);
        return EjecutarAsync<T>(db, id, () => consulta.ExecuteUpdateAsync(cambios, ct), false, cambioPassword, ct);
    }

    public static Task<int> EliminarAuditadoAsync<T>(this IQueryable<T> consulta, AppDbContext db,
        int id, CancellationToken ct) where T : class
    {
        consulta = LimitarFila(consulta, db, id);
        return EjecutarAsync<T>(db, id, () => consulta.ExecuteDeleteAsync(ct), true, false, ct);
    }

    private static IQueryable<T> LimitarFila<T>(IQueryable<T> consulta, AppDbContext db, int id) where T : class
    {
        var clave = db.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!.Properties.Single().Name;
        return consulta.Where(fila => EF.Property<int>(fila, clave) == id);
    }

    private static Task<int> EjecutarAsync<T>(AppDbContext db, int id,
        Func<Task<int>> escritura, bool eliminar, bool cambioPassword, CancellationToken ct) where T : class =>
        db.EjecutarUnidadAuditadaAsync(async () =>
        {
            var anterior = await db.BloquearFilaAsync<T>(id, ct);
            if (anterior is null) return 0;
            var filas = await escritura(); // Conserva WHERE, comparación del hash, stock y estado de pago.
            if (filas == 0) return 0;
            if (filas != 1) throw new InvalidOperationException("La operación auditada debe afectar únicamente la fila bloqueada.");
            var entrada = db.Entry(anterior);
            var actual = eliminar ? null : await entrada.GetDatabaseValuesAsync(ct);
            var anteriores = CambioAuditoria.Serializar(entrada.Metadata.ClrType, entrada.CurrentValues);
            var nuevos = actual is null ? null : CambioAuditoria.Serializar(entrada.Metadata.ClrType, actual);
            var modificada = eliminar || anteriores != nuevos;
            if (cambioPassword)
            {
                // Se compara en memoria; ninguna contraseña, hash o versión de credenciales se serializa.
                modificada = actual is not null && !Equals(entrada.Property(nameof(Usuario.PasswordHash)).CurrentValue,
                    actual[nameof(Usuario.PasswordHash)]);
                anteriores = null;
                nuevos = JsonSerializer.Serialize(new { Evento = "CambioPassword" });
            }
            if (modificada)
            {
                var log = new LogAccion
                {
                    TablaAfectada = entrada.Metadata.GetTableName()!,
                    LlavePrimaria = JsonSerializer.Serialize(entrada.Metadata.FindPrimaryKey()!.Properties
                        .ToDictionary(p => p.Name, p => entrada.Property(p.Name).CurrentValue)),
                    Accion = eliminar ? AccionLog.Delete : AccionLog.Update,
                    ValoresAnteriores = anteriores,
                    ValoresNuevos = nuevos,
                    FechaHora = DateTimeOffset.UtcNow
                };
                await db.EscribirLogDirectoAsync(log, ct);
            }
            return filas; // Una escritura sin cambio conserva la respuesta previa, pero no inventa un log.
        }, ct);
}
//[FIN][5/10/2026][jgarciad8][Auditoría explícita de UPDATE y DELETE sin sustituir sus condiciones SQL]
