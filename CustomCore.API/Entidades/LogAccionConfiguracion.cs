//[INICIO][28/8/2026][Rodriale][Mapeo de la bitácora, con índice para consultar por tabla y registro]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class LogAccionConfiguration : IEntityTypeConfiguration<LogAccion>
{
    public void Configure(EntityTypeBuilder<LogAccion> builder)
    {
        builder.ToTable("LogsAcciones");
        builder.HasKey(l => l.IdLog);

        builder.Property(l => l.TablaAfectada).HasMaxLength(50).IsRequired();
        builder.Property(l => l.LlavePrimaria).HasMaxLength(50).IsRequired();

        builder.Property(l => l.Accion)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.ValoresAnteriores).HasColumnType("jsonb");
        builder.Property(l => l.ValoresNuevos).HasColumnType("jsonb");

        builder.HasIndex(l => new { l.TablaAfectada, l.LlavePrimaria });
        builder.HasIndex(l => l.FechaHora);

        builder.HasOne(l => l.Usuario)
            .WithMany()
            .HasForeignKey(l => l.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de la bitácora, con índice para consultar por tabla y registro]