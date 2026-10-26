//[INICIO][26/10/2026][Rodriale][Tablas de cotizaciones; los montos no pueden ser negativos ni las cantidades cero]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class CotizacionConfiguration : IEntityTypeConfiguration<Cotizacion>
{
    public void Configure(EntityTypeBuilder<Cotizacion> builder)
    {
        builder.ToTable("Cotizaciones");
        builder.HasKey(c => c.IdCotizacion);

        builder.Property(c => c.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Nota).HasMaxLength(500);
        builder.Property(c => c.Total).HasPrecision(18, 2);

        builder.HasIndex(c => c.OrdenTrabajoId);

        builder.HasOne(c => c.OrdenTrabajo)
            .WithMany()
            .HasForeignKey(c => c.OrdenTrabajoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioCreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CotizacionDetalleConfiguration : IEntityTypeConfiguration<CotizacionDetalle>
{
    public void Configure(EntityTypeBuilder<CotizacionDetalle> builder)
    {
        builder.ToTable("CotizacionesDetalle", t =>
        {
            t.HasCheckConstraint("CK_CotizacionesDetalle_CantidadPositiva", @"""Cantidad"" > 0");
            t.HasCheckConstraint("CK_CotizacionesDetalle_PrecioNoNegativo", @"""PrecioUnitario"" >= 0");
        });

        builder.HasKey(d => d.IdCotizacionDetalle);

        builder.Property(d => d.Descripcion).HasMaxLength(200).IsRequired();
        builder.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
        builder.Property(d => d.Subtotal).HasPrecision(18, 2);

        builder.HasOne<Cotizacion>()
            .WithMany(c => c.Detalles)
            .HasForeignKey(d => d.CotizacionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
//[FIN][26/10/2026][Rodriale][Tablas de cotizaciones]
