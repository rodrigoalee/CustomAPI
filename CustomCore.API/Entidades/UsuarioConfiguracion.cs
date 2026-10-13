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

        //[INICIO][12/10/2026][Rodriale][Una sede cerrada no se borra; Restrict protege a sus empleados]
        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(u => u.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);
        //[FIN][12/10/2026][Rodriale][Una sede cerrada no se borra]
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Usuario, Correo único porque es el identificador de login]