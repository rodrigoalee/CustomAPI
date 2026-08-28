//[INICIO][28/8/2026][Rodriale][Mapeo del detalle de factura]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class FacturaDetalleConfiguration : IEntityTypeConfiguration<FacturaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaDetalle> builder)
    {
        builder.ToTable("FacturasDetalle", t =>
            t.HasCheckConstraint(
                "CK_FacturasDetalle_CantidadPositiva",
                @"""Cantidad"" > 0"));

        builder.HasKey(d => d.IdFacturaDetalle);

        builder.Property(d => d.DescripcionItem).HasMaxLength(150).IsRequired();
        builder.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
        builder.Property(d => d.Subtotal).HasPrecision(18, 2);

        builder.HasIndex(d => d.FacturaId);

        builder.HasOne(d => d.Factura)
            .WithMany(f => f.Detalles)
            .HasForeignKey(d => d.FacturaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo del detalle de factura]