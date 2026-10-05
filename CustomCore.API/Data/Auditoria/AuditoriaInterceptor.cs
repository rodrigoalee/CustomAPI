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
    private void Capturar(DbContextEventData datos)
    {
        if (datos.Context is not AppDbContext db) return;
        db.ChangeTracker.DetectChanges();
        db.CambiosAuditoria = db.ChangeTracker.Entries()
            .Where(CambioAuditoria.EsAuditable).Select(e => new CambioAuditoria(e)).ToList();
        db.UltimoLogAuditoria = null;
        if (db.CambiosAuditoria.Count == 0) return;
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La auditoría necesita la transacción de AppDbContext.");

        var identidad = httpContextAccessor.HttpContext?.User;
        db.UsuarioAuditoria = null;
        if (identidad?.Identity?.IsAuthenticated == true)
        {
     
            if (!int.TryParse(identidad.FindFirst("sub")?.Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var usuarioId) || usuarioId <= 0)
                throw new InvalidOperationException("La identidad autenticada no tiene un usuario válido.");
            db.UsuarioAuditoria = usuarioId;
        }
        db.FechaAuditoria = DateTimeOffset.UtcNow;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capturar(eventData);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capturar(eventData);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is AppDbContext db)
            foreach (var cambio in db.CambiosAuditoria)
            {
                using var comando = CrearComando(db, cambio.CrearLog(db.UsuarioAuditoria, db.FechaAuditoria));
                db.UltimoLogAuditoria = (int)comando.ExecuteScalar()!;
            }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext db)
            foreach (var cambio in db.CambiosAuditoria)
            {
                await using var comando = CrearComando(db, cambio.CrearLog(db.UsuarioAuditoria, db.FechaAuditoria));
                db.UltimoLogAuditoria = (int)(await comando.ExecuteScalarAsync(cancellationToken))!;
            }
        return result;
    }

    private static DbCommand CrearComando(AppDbContext db, LogAccion log)
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
}
//[FIN][2/10/2026][jgarciad8][Interceptor de auditoría sin recursión y dentro de la transacción del cambio]
