//[INICIO][28/8/2026][Rodriale][Mapeo de Cita, estado como texto y relación con Vehiculo]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class CitaConfiguration : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> builder)
    {
        builder.ToTable("Citas");
        builder.HasKey(c => c.IdCita);

        builder.Property(c => c.DuracionMinutos).HasDefaultValue(60);

        builder.Property(c => c.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.FechaHora);
        builder.HasIndex(c => new { c.VehiculoId, c.FechaHora });

        builder.HasOne(c => c.Vehiculo)
            .WithMany(v => v.Citas)
            .HasForeignKey(c => c.VehiculoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Cita, estado como texto y relación con Vehiculo]