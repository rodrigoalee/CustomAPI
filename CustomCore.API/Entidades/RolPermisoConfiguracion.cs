//[INICIO][28/8/2026][Rodriale][Mapeo de la tabla puente RolPermisos con llave compuesta]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class RolPermisoConfiguration : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("RolPermisos");
        builder.HasKey(rp => new { rp.RolId, rp.PermisoId });

        builder.HasOne(rp => rp.Rol)
            .WithMany(r => r.RolPermisos)
            .HasForeignKey(rp => rp.RolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permiso)
            .WithMany(p => p.RolPermisos)
            .HasForeignKey(rp => rp.PermisoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
//[FIN][28/8/2026][Rodriale][Mapeo de la tabla puente RolPermisos con llave compuesta]