//[INICIO][12/10/2026][Rodriale][Mapeo del inventario por sede]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class InventarioSucursalConfiguration : IEntityTypeConfiguration<InventarioSucursal>
{
    public void Configure(EntityTypeBuilder<InventarioSucursal> builder)
    {
        //[INICIO][12/10/2026][Rodriale][La base misma impide stock negativo, aunque algún día se escriba sin pasar por el servicio]
        builder.ToTable("InventariosSucursal", t =>
            t.HasCheckConstraint("CK_InventariosSucursal_CantidadNoNegativa", @"""Cantidad"" >= 0"));
        //[FIN][12/10/2026][Rodriale][La base misma impide stock negativo]

        builder.HasKey(i => i.IdInventario);

        //[INICIO][12/10/2026][Rodriale][Una sola fila por sede y repuesto]
        builder.HasIndex(i => new { i.SucursalId, i.RepuestoId }).IsUnique();
        //[FIN][12/10/2026][Rodriale][Una sola fila por sede y repuesto]

        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(i => i.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Repuesto>()
            .WithMany()
            .HasForeignKey(i => i.RepuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//[FIN][12/10/2026][Rodriale][Mapeo del inventario por sede]
