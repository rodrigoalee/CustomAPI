//[INICIO][28/8/2026][Rodriale][Mapeo de Rol a la tabla Roles]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.IdRol);

        builder.Property(r => r.Nombre).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Descripcion).HasMaxLength(200);

        builder.HasIndex(r => r.Nombre).IsUnique();
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Rol a la tabla Roles]