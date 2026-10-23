//[INICIO][28/8/2026][Rodriale][Mapeo de Usuario, Correo único porque es el identificador de login]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        //[INICIO][23/10/2026][Rodriale][Con el hash ya opcional, esto es lo único que impide una cuenta sin ninguna forma de entrar]
        builder.ToTable("Usuarios", t => t.HasCheckConstraint(
            "CK_Usuarios_AlMenosUnaCredencial",
            @"""PasswordHash"" IS NOT NULL OR ""GoogleId"" IS NOT NULL"));
        //[FIN][23/10/2026][Rodriale][Lo único que impide una cuenta sin ninguna forma de entrar]

        builder.HasKey(u => u.IdUsuario);

        builder.Property(u => u.NombreCompleto).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Correo).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash);
        builder.Property(u => u.Activo).HasDefaultValue(true);

        builder.HasIndex(u => u.Correo).IsUnique();

        //[INICIO][23/10/2026][Rodriale][Mismo truco del filtro que ClienteId: en Postgres los NULL no chocan, así que esto solo exige que dos cuentas no compartan la misma cuenta de Google]
        builder.Property(u => u.GoogleId).HasMaxLength(255);
        builder.HasIndex(u => u.GoogleId)
            .IsUnique()
            .HasFilter(@"""GoogleId"" IS NOT NULL");
        //[FIN][23/10/2026][Rodriale][Mismo truco del filtro que ClienteId]

        builder.Property(u => u.VersionCredenciales).HasMaxLength(64).IsRequired();

        //[INICIO][12/10/2026][Rodriale][Una sede cerrada no se borra; Restrict protege a sus empleados]
        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(u => u.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);
        //[FIN][12/10/2026][Rodriale][Una sede cerrada no se borra]

        //[INICIO][14/10/2026][Rodriale][La ficha no se borra mientras tenga cuenta; el índice único evita que dos cuentas se adueñen del mismo cliente]
        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(u => u.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        //[INICIO][14/10/2026][Rodriale][En Postgres los NULL no chocan entre sí, así que el filtro no cambia la regla: solo deja fuera del índice a los empleados]
        builder.HasIndex(u => u.ClienteId)
            .IsUnique()
            .HasFilter(@"""ClienteId"" IS NOT NULL");
        //[FIN][14/10/2026][Rodriale][El filtro solo deja fuera del índice a los empleados]
        //[FIN][14/10/2026][Rodriale][La ficha no se borra mientras tenga cuenta]
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de Usuario, Correo único porque es el identificador de login]