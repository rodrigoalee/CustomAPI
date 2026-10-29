//[INICIO][29/10/2026][Rodriale][Tabla de fotos de la orden; si se borra la orden se van sus fotos]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class FotoOrdenConfiguration : IEntityTypeConfiguration<FotoOrden>
{
    public void Configure(EntityTypeBuilder<FotoOrden> builder)
    {
        builder.ToTable("FotosOrden");
        builder.HasKey(f => f.IdFotoOrden);

        builder.Property(f => f.Url).HasMaxLength(500).IsRequired();
        builder.Property(f => f.Descripcion).HasMaxLength(200);

        builder.HasIndex(f => f.OrdenTrabajoId);

        builder.HasOne(f => f.OrdenTrabajo)
            .WithMany(o => o.Fotos)
            .HasForeignKey(f => f.OrdenTrabajoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(f => f.UsuarioSubeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//[FIN][29/10/2026][Rodriale][Tabla de fotos de la orden]
