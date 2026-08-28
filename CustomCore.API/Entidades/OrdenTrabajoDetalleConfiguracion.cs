//[INICIO][28/8/2026][Rodriale][Mapeo del detalle de orden, con CHECK que exige repuesto o servicio]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class OrdenTrabajoDetalleConfiguration : IEntityTypeConfiguration<OrdenTrabajoDetalle>
{
    public void Configure(EntityTypeBuilder<OrdenTrabajoDetalle> builder)
    {
        builder.ToTable("OrdenesTrabajoDetalle", t =>
        {
            t.HasCheckConstraint(
                "CK_OrdenesTrabajoDetalle_RepuestoOServicio",
                @"(""RepuestoId"" IS NOT NULL) <> (""ServicioId"" IS NOT NULL)");
            t.HasCheckConstraint(
                "CK_OrdenesTrabajoDetalle_CantidadPositiva",
                @"""Cantidad"" > 0");
        });

        builder.HasKey(d => d.IdOrdenDetalle);

        builder.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
        builder.Property(d => d.SubtotalEstimado).HasPrecision(18, 2);

        builder.HasIndex(d => d.OrdenTrabajoId);

        builder.HasOne(d => d.OrdenTrabajo)
            .WithMany(o => o.Detalles)
            .HasForeignKey(d => d.OrdenTrabajoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Repuesto)
            .WithMany()
            .HasForeignKey(d => d.RepuestoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Servicio)
            .WithMany()
            .HasForeignKey(d => d.ServicioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo del detalle de orden, con CHECK que exige repuesto o servicio]