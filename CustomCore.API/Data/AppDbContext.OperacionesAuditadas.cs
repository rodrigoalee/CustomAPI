//[INICIO][5/10/2026][jgarciad8][Transacciones locales reintentables para operaciones de varios pasos]
using CustomCore.API.Data.Auditoria;
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CustomCore.API.Data;

public sealed partial class AppDbContext
{
    internal int? IdConfirmacionAuditoria { get; set; }

    public Task<T?> BloquearFilaAsync<T>(int id, CancellationToken ct) where T : class
    {
        if (Database.CurrentTransaction is null)
            throw new InvalidOperationException("La lectura bloqueada requiere una transacción activa.");
        return BloqueoAuditoria.Consulta<T>(this, id).SingleOrDefaultAsync(ct);
    }

    internal async Task EscribirLogDirectoAsync(LogAccion log, CancellationToken ct)
    {
        if (auditoria is null) throw new InvalidOperationException("Falta el interceptor de auditoría.");
        log.UsuarioId = auditoria.ObtenerUsuario();
        await using var comando = AuditoriaInterceptor.CrearComando(this, log);
        IdConfirmacionAuditoria = (int)(await comando.ExecuteScalarAsync(ct))!;
    }

    public async Task<T> EjecutarUnidadAuditadaAsync<T>(Func<Task<T>> operacion, CancellationToken ct)
    {
        if (auditoria is null) throw new InvalidOperationException("Falta el interceptor de auditoría.");
        var seguimiento = ChangeTracker.Entries().Select(e => new EstadoEntrada(e)).ToList();
        var marcaAnterior = IdConfirmacionAuditoria;
        try
        {
            if (Database.CurrentTransaction is { } transaccion)
            {
                var punto = "operacion_" + Guid.NewGuid().ToString("N");
                await transaccion.CreateSavepointAsync(punto, ct);
                try
                {
                    var resultado = await operacion();
                    await transaccion.ReleaseSavepointAsync(punto, ct);
                    return resultado;
                }
                catch
                {
                    await transaccion.RollbackToSavepointAsync(punto, CancellationToken.None);
                    throw;
                }
            }

            var intento = 0;
            return await Database.CreateExecutionStrategy().ExecuteInTransactionAsync(this,
                async (db, token) =>
                {
                    if (intento++ > 0) RestaurarSeguimiento(seguimiento);
                    db.IdConfirmacionAuditoria = null;
                    return await operacion();
                },
                (db, token) => db.IdConfirmacionAuditoria is int id
                    ? db.LogsAcciones.AsNoTracking().AnyAsync(l => l.IdLog == id, token)
                    : Task.FromResult(false), ct);
        }
        catch
        {
            IdConfirmacionAuditoria = marcaAnterior;
            RestaurarSeguimiento(seguimiento);
            throw;
        }
    }

    private sealed class EstadoEntrada(EntityEntry entrada)
    {
        internal EntityEntry Entrada { get; } = entrada;
        internal EntityState Estado { get; } = entrada.State;
        internal PropertyValues Actuales { get; } = entrada.CurrentValues.Clone();
        internal PropertyValues Originales { get; } = entrada.OriginalValues.Clone();
        internal string[] Modificadas { get; } = entrada.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToArray();
        internal string[] Temporales { get; } = entrada.Properties.Where(p => p.IsTemporary).Select(p => p.Metadata.Name).ToArray();
    }

    private void RestaurarSeguimiento(List<EstadoEntrada> estados)
    {
        // Retira entidades creadas durante un intento fallido; no vuelve a insertar sus instancias.
        ChangeTracker.Clear();
        foreach (var estado in estados)
        {
            var entrada = Entry(estado.Entrada.Entity);
            entrada.CurrentValues.SetValues(estado.Actuales);
            entrada.State = estado.Estado;
            entrada.OriginalValues.SetValues(estado.Originales);
            foreach (var propiedad in entrada.Properties)
            {
                propiedad.IsTemporary = estado.Temporales.Contains(propiedad.Metadata.Name);
                if (estado.Estado == EntityState.Modified)
                    propiedad.IsModified = estado.Modificadas.Contains(propiedad.Metadata.Name);
            }
        }
    }
}
//[FIN][5/10/2026][jgarciad8][Transacciones locales reintentables para operaciones de varios pasos]
