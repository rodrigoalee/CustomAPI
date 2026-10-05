//[INICIO][2/10/2026][jgarciad8][Interceptor de auditoría sin recursión y dentro de la transacción del cambio]
using System.Data.Common;
using System.Globalization;
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace CustomCore.API.Data.Auditoria;

public sealed class AuditoriaInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    //[INICIO][5/10/2026][jgarciad8][Captura de originales reales bajo bloqueo y responsable validado]
    internal int? ObtenerUsuario()
    {
        var identidad = httpContextAccessor.HttpContext?.User;
        if (identidad?.Identity?.IsAuthenticated != true) return null;
        if (!int.TryParse(identidad.FindFirst("sub")?.Value, NumberStyles.None,
            CultureInfo.InvariantCulture, out var usuarioId) || usuarioId <= 0)
            throw new InvalidOperationException("La identidad autenticada no tiene un usuario válido.");
        return usuarioId;
    }

    private async ValueTask Capturar(DbContextEventData datos, bool asincrono, CancellationToken ct)
    {
        if (datos.Context is not AppDbContext db) return;
        db.ChangeTracker.DetectChanges();
        var entradas = db.ChangeTracker.Entries().Where(CambioAuditoria.EsAuditable).ToList();
        db.CambiosAuditoria = [];
        db.UltimoLogAuditoria = null;
        if (entradas.Count == 0) return;
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La auditoría necesita la transacción de AppDbContext.");
        db.UsuarioAuditoria = ObtenerUsuario();
        db.FechaAuditoria = DateTimeOffset.UtcNow;
        foreach (var entrada in entradas)
        {
            Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues? originales = null;
            if (entrada.State is EntityState.Modified or EntityState.Deleted)
            {
                using var bloqueo = BloqueoAuditoria.Crear(db, entrada);
                if (asincrono) await bloqueo.ExecuteScalarAsync(ct); else bloqueo.ExecuteScalar();
                originales = asincrono ? await entrada.GetDatabaseValuesAsync(ct) : entrada.GetDatabaseValues();
            }
            db.CambiosAuditoria.Add(new CambioAuditoria(entrada, originales));
        }
    }
    //[FIN][5/10/2026][jgarciad8][Captura de originales reales bajo bloqueo y responsable validado]

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capturar(eventData, false, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await Capturar(eventData, true, cancellationToken);
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is AppDbContext db)
            foreach (var cambio in db.CambiosAuditoria)
            {
                //[INICIO][5/10/2026][jgarciad8][Valores persistidos y marcador de confirmación de toda la unidad]
                var actuales = cambio.Eliminacion ? null : cambio.Entrada.GetDatabaseValues();
                var log = cambio.CrearLog(db.UsuarioAuditoria, db.FechaAuditoria, actuales);
                if (log is null) continue;
                using var comando = CrearComando(db, log);
                db.IdConfirmacionAuditoria = db.UltimoLogAuditoria = (int)comando.ExecuteScalar()!;
                //[FIN][5/10/2026][jgarciad8][Valores persistidos y marcador de confirmación de toda la unidad]
            }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext db)
            foreach (var cambio in db.CambiosAuditoria)
            {
                //[INICIO][5/10/2026][jgarciad8][Valores persistidos y marcador de confirmación de toda la unidad]
                var actuales = cambio.Eliminacion ? null : await cambio.Entrada.GetDatabaseValuesAsync(cancellationToken);
                var log = cambio.CrearLog(db.UsuarioAuditoria, db.FechaAuditoria, actuales);
                if (log is null) continue;
                await using var comando = CrearComando(db, log);
                db.IdConfirmacionAuditoria = db.UltimoLogAuditoria = (int)(await comando.ExecuteScalarAsync(cancellationToken))!;
                //[FIN][5/10/2026][jgarciad8][Valores persistidos y marcador de confirmación de toda la unidad]
            }
        return result;
    }

    //[INICIO][5/10/2026][jgarciad8][Escritor compartido con las operaciones directas]
    internal static DbCommand CrearComando(AppDbContext db, LogAccion log)
    {
    
        var comando = db.Database.GetDbConnection().CreateCommand();
        comando.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        comando.CommandTimeout = db.Database.GetCommandTimeout() ?? comando.CommandTimeout;
        comando.CommandText = """
            INSERT INTO customcore."LogsAcciones"
                ("UsuarioId", "TablaAfectada", "Accion", "LlavePrimaria",
                 "ValoresAnteriores", "ValoresNuevos", "FechaHora")
            VALUES (@usuario, @tabla, @accion, @llave, @anteriores, @nuevos, @fecha)
            RETURNING "IdLog";
            """;
        comando.Parameters.Add(new NpgsqlParameter("usuario", NpgsqlDbType.Integer) { Value = (object?)log.UsuarioId ?? DBNull.Value });
        comando.Parameters.Add(new NpgsqlParameter("tabla", log.TablaAfectada));
        comando.Parameters.Add(new NpgsqlParameter("accion", log.Accion.ToString()));
        comando.Parameters.Add(new NpgsqlParameter("llave", log.LlavePrimaria));
        comando.Parameters.Add(new NpgsqlParameter("anteriores", NpgsqlDbType.Jsonb) { Value = (object?)log.ValoresAnteriores ?? DBNull.Value });
        comando.Parameters.Add(new NpgsqlParameter("nuevos", NpgsqlDbType.Jsonb) { Value = (object?)log.ValoresNuevos ?? DBNull.Value });
        comando.Parameters.Add(new NpgsqlParameter("fecha", NpgsqlDbType.TimestampTz) { Value = log.FechaHora });
        return comando;
    }
    //[FIN][5/10/2026][jgarciad8][Escritor compartido con las operaciones directas]
}
//[FIN][2/10/2026][jgarciad8][Interceptor de auditoría sin recursión y dentro de la transacción del cambio]
