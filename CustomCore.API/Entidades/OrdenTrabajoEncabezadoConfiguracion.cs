//[INICIO][28/8/2026][Rodriale][Mapeo del encabezado de orden de trabajo]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class OrdenTrabajoEncabezadoConfiguration : IEntityTypeConfiguration<OrdenTrabajoEncabezado>
{
    public void Configure(EntityTypeBuilder<OrdenTrabajoEncabezado> builder)
    {
        builder.ToTable("OrdenesTrabajoEncabezado");
        builder.HasKey(o => o.IdOrdenEncabezado);

        builder.Property(o => o.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.TotalEstimado).HasPrecision(18, 2);

        builder.HasIndex(o => o.VehiculoId);
        builder.HasIndex(o => o.Estado);

        builder.HasOne(o => o.Vehiculo)
            .WithMany()
            .HasForeignKey(o => o.VehiculoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo del encabezado de orden de trabajo]