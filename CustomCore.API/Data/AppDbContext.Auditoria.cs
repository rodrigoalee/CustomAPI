//[INICIO][2/10/2026][jgarciad8][Unidad transaccional para datos y auditoría con reintentos de EF Core]
using CustomCore.API.Data.Auditoria;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Data;

public sealed partial class AppDbContext
{
    internal List<CambioAuditoria> CambiosAuditoria { get; set; } = [];
    internal int? UltimoLogAuditoria { get; set; }
    internal int? UsuarioAuditoria { get; set; }
    internal DateTimeOffset FechaAuditoria { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (auditoria is not null) optionsBuilder.AddInterceptors(auditoria);
    }

    private bool RequiereAuditoria()
    {
        if (auditoria is null) return false;
        ChangeTracker.DetectChanges();
        return ChangeTracker.Entries().Any(CambioAuditoria.EsAuditable);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (!RequiereAuditoria()) return base.SaveChanges(acceptAllChangesOnSuccess);
        try
        {
            int resultado;
            if (Database.CurrentTransaction is { } transaccion)
            {
               
                var punto = "auditoria_" + Guid.NewGuid().ToString("N");
                transaccion.CreateSavepoint(punto);
                try
                {
                    resultado = base.SaveChanges(false);
                    transaccion.ReleaseSavepoint(punto);
                }
                catch
                {
                    transaccion.RollbackToSavepoint(punto);
                    throw;
                }
            }
            else
            {
                resultado = Database.CreateExecutionStrategy().ExecuteInTransaction(this,
                    db => db.GuardarSinAceptar(),
                    db => db.UltimoLogAuditoria is int id && db.LogsAcciones.AsNoTracking().Any(l => l.IdLog == id));
            }
            if (acceptAllChangesOnSuccess) ChangeTracker.AcceptAllChanges();
            return resultado;
        }
        finally { CambiosAuditoria.Clear(); }
    }

    private int GuardarSinAceptar() => base.SaveChanges(false);
    private Task<int> GuardarSinAceptarAsync(CancellationToken ct) => base.SaveChangesAsync(false, ct);

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (!RequiereAuditoria()) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        try
        {
            int resultado;
            if (Database.CurrentTransaction is { } transaccion)
            {
                var punto = "auditoria_" + Guid.NewGuid().ToString("N");
                await transaccion.CreateSavepointAsync(punto, cancellationToken);
                try
                {
                    resultado = await base.SaveChangesAsync(false, cancellationToken);
                    await transaccion.ReleaseSavepointAsync(punto, cancellationToken);
                }
                catch
                {
                    
                    await transaccion.RollbackToSavepointAsync(punto, CancellationToken.None);
                    throw;
                }
            }
            else
            {
                resultado = await Database.CreateExecutionStrategy().ExecuteInTransactionAsync(this,
                    (db, ct) => db.GuardarSinAceptarAsync(ct),
                    (db, ct) => db.UltimoLogAuditoria is int id
                        ? db.LogsAcciones.AsNoTracking().AnyAsync(l => l.IdLog == id, ct)
                        : Task.FromResult(false), cancellationToken);
            }
            
            if (acceptAllChangesOnSuccess) ChangeTracker.AcceptAllChanges();
            return resultado;
        }
        finally { CambiosAuditoria.Clear(); }
    }
}
//[FIN][2/10/2026][jgarciad8][Unidad transaccional para datos y auditoría con reintentos de EF Core]
