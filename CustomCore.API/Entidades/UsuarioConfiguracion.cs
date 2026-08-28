//[INICIO][28/8/2026][Rodriale][Mapeo de Usuario, Correo único porque es el identificador de login]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.IdUsuario);

        builder.Property(u => u.NombreCompleto).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Correo).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Activo).HasDefaultValue(true);

        builder.HasIndex(u => u.Correo).IsUnique();
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Usuario, Correo único porque es el identificador de login]