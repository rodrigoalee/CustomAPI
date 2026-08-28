//[INICIO][28/8/2026][Rodriale][Mapeo de Cliente a la tabla Clientes]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.IdCliente);

        builder.Property(c => c.NombreCompleto).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Telefono).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Correo).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Nit).HasMaxLength(20);

        builder.HasIndex(c => c.Correo);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Cliente a la tabla Clientes]