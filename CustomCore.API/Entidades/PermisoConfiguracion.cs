//[INICIO][28/8/2026][Rodriale][Mapeo de Permiso a la tabla Permisos]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("Permisos");
        builder.HasKey(p => p.IdPermiso);

        builder.Property(p => p.NombreCodigo).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Descripcion).HasMaxLength(100);
        builder.Property(p => p.Modulo).HasMaxLength(50).IsRequired();

        builder.HasIndex(p => p.NombreCodigo).IsUnique();
        builder.HasIndex(p => p.Modulo);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Permiso a la tabla Permisos]