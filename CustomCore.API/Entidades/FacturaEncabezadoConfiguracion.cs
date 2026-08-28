//[INICIO][28/8/2026][Rodriale][Mapeo del encabezado de factura, una por orden de trabajo]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class FacturaEncabezadoConfiguration : IEntityTypeConfiguration<FacturaEncabezado>
{
    public void Configure(EntityTypeBuilder<FacturaEncabezado> builder)
    {
        builder.ToTable("FacturasEncabezado");
        builder.HasKey(f => f.IdFacturaEncabezado);

        builder.Property(f => f.NombreFacturacion).HasMaxLength(150).IsRequired();
        builder.Property(f => f.NitFacturacion).HasMaxLength(20);
        builder.Property(f => f.StripePaymentId).HasMaxLength(100);

        builder.Property(f => f.Subtotal).HasPrecision(18, 2);
        builder.Property(f => f.TasaImpuesto).HasPrecision(5, 4);
        builder.Property(f => f.MontoImpuesto).HasPrecision(18, 2);
        builder.Property(f => f.TotalPagado).HasPrecision(18, 2);

        builder.Property(f => f.EstadoPago)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(f => f.OrdenTrabajoId).IsUnique();
        builder.HasIndex(f => f.ClienteId);
        builder.HasIndex(f => f.FechaEmision);

        builder.HasOne(f => f.OrdenTrabajo)
            .WithOne()
            .HasForeignKey<FacturaEncabezado>(f => f.OrdenTrabajoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Cliente)
            .WithMany()
            .HasForeignKey(f => f.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo del encabezado de factura, una por orden de trabajo]