//[INICIO][28/8/2026][Rodriale][Mapeo de Repuesto a la tabla Repuestos]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class RepuestoConfiguration : IEntityTypeConfiguration<Repuesto>
{
    public void Configure(EntityTypeBuilder<Repuesto> builder)
    {
        builder.ToTable("Repuestos");
        builder.HasKey(r => r.IdRepuesto);

        builder.Property(r => r.CodigoStock).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(r => r.PrecioUnitario).HasPrecision(18, 2);
        builder.Property(r => r.Activo).HasDefaultValue(true);

        builder.HasIndex(r => r.CodigoStock).IsUnique();
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Repuesto a la tabla Repuestos]