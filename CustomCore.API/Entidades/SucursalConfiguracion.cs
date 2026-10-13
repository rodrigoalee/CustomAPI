//[INICIO][17/9/2026][Rodriale][Mapeo de Sucursal a la tabla Sucursales]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> builder)
    {
        builder.ToTable("Sucursales");
        builder.HasKey(s => s.IdSucursal);

        builder.Property(s => s.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Direccion).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Telefono).HasMaxLength(20);
        builder.Property(s => s.Activo).HasDefaultValue(true);

        //[INICIO][17/9/2026][Rodriale][Dos sedes con el mismo nombre confundirían a quien elige dónde agendar]
        builder.HasIndex(s => s.Nombre).IsUnique();
        //[FIN][17/9/2026][Rodriale][Dos sedes con el mismo nombre confundirían a quien elige dónde agendar]
    }
}
//[FIN][17/9/2026][Rodriale][Mapeo de Sucursal a la tabla Sucursales]