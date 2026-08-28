//[INICIO][28/8/2026][Rodriale][Mapeo de Vehiculo y su relación con Cliente]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        builder.ToTable("Vehiculos");
        builder.HasKey(v => v.IdVehiculo);

        builder.Property(v => v.Placa).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Marca).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Modelo).HasMaxLength(50).IsRequired();

        builder.HasIndex(v => v.Placa).IsUnique();

        builder.HasOne(v => v.Cliente)
            .WithMany(c => c.Vehiculos)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Vehiculo y su relación con Cliente]