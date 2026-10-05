//[INICIO][5/10/2026][jgarciad8][Lectura de filas protegida contra escrituras concurrentes]
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace CustomCore.API.Data.Auditoria;

internal static class BloqueoAuditoria
{
    internal static string Identificador(string nombre) => "\"" + nombre.Replace("\"", "\"\"") + "\"";

    internal static DbCommand Crear(AppDbContext db, EntityEntry entrada)
    {
        var tipo = entrada.Metadata;
        var tabla = StoreObjectIdentifier.Table(tipo.GetTableName()!, tipo.GetSchema());
        var comando = db.Database.GetDbConnection().CreateCommand();
        comando.Transaction = db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("El bloqueo de auditoría requiere una transacción.");
        comando.CommandTimeout = db.Database.GetCommandTimeout() ?? comando.CommandTimeout;
        var condiciones = new List<string>();
        foreach (var propiedad in tipo.FindPrimaryKey()!.Properties)
        {
            var parametro = "p" + condiciones.Count;
            condiciones.Add(Identificador(propiedad.GetColumnName(tabla)!) + " = @" + parametro);
            comando.Parameters.Add(new NpgsqlParameter(parametro, entrada.Property(propiedad.Name).CurrentValue!));
        }
        comando.CommandText = $"SELECT 1 FROM {Identificador(tipo.GetSchema()!)}.{Identificador(tipo.GetTableName()!)} WHERE {string.Join(" AND ", condiciones)} FOR UPDATE";
        return comando;
    }

    internal static IQueryable<T> Consulta<T>(AppDbContext db, int id) where T : class
    {
        var tipo = db.Model.FindEntityType(typeof(T))!;
        var tabla = StoreObjectIdentifier.Table(tipo.GetTableName()!, tipo.GetSchema());
        var clave = tipo.FindPrimaryKey()!.Properties.Single();
        // Solo identificadores del modelo; el valor del cliente siempre viaja parametrizado.
        var sql = $"SELECT * FROM {Identificador(tipo.GetSchema()!)}.{Identificador(tipo.GetTableName()!)} WHERE {Identificador(clave.GetColumnName(tabla)!)} = @id FOR UPDATE";
        return db.Set<T>().FromSqlRaw(sql, new NpgsqlParameter("id", id)).AsNoTracking();
    }
}
//[FIN][5/10/2026][jgarciad8][Lectura de filas protegida contra escrituras concurrentes]
