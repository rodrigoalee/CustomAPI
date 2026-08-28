//[INICIO][28/8/2026][Rodriale][Mapeo de Servicio a la tabla Servicios]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class ServicioConfiguration : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        builder.ToTable("Servicios");
        builder.HasKey(s => s.IdServicio);

        builder.Property(s => s.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(s => s.TarifaManoObra).HasPrecision(18, 2);
        builder.Property(s => s.Activo).HasDefaultValue(true);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Servicio a la tabla Servicios]