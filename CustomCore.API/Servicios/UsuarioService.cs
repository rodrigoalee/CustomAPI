//[INICIO][16/9/2026][jgarciad8][Servicio de consulta de usuarios]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using Microsoft.EntityFrameworkCore;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;

namespace CustomCore.API.Servicios;

//[INICIO][16/9/2026][jgarciad8][Dependencias del servicio de usuarios]
public sealed class UsuarioService(
    AppDbContext db,
    PasswordService passwordService,
    RolService rolService)
//[FIN][16/9/2026][jgarciad8][Dependencias del servicio de usuarios]

{
    //[INICIO][16/9/2026][jgarciad8][Consulta paginada de usuarios con búsqueda]
    public async Task<ResultadoPaginado<UsuarioResumenDto>> ObtenerAsync(
        ConsultaPaginada consulta,
        CancellationToken cancellationToken)
    {
        //[INICIO][26/10/2026][Rodriale][El mantenedor lista al personal; las cuentas del portal no son usuarios del sistema administrativo]
        var query = db.Usuarios.AsNoTracking().Where(usuario => usuario.ClienteId == null);
        //[FIN][26/10/2026][Rodriale][El mantenedor lista al personal]

        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
        {
            var patron = $"%{consulta.Busqueda.Trim()}%";

            query = query.Where(usuario =>
                EF.Functions.ILike(usuario.NombreCompleto, patron)
                || EF.Functions.ILike(usuario.Correo, patron));
        }

        var total = await query.CountAsync(cancellationToken);

      
        var desplazamiento =
            ((long)consulta.Pagina - 1) * consulta.Tamanio;

        if (desplazamiento >= total)
        {
            return new ResultadoPaginado<UsuarioResumenDto>(
                Array.Empty<UsuarioResumenDto>(),
                consulta.Pagina,
                consulta.Tamanio,
                total);
        }

        var items = await Proyectar(query
            .OrderBy(usuario => usuario.NombreCompleto)
            .ThenBy(usuario => usuario.IdUsuario)
            .Skip((int)desplazamiento)
            .Take(consulta.Tamanio))
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<UsuarioResumenDto>(
            items,
            consulta.Pagina,
            consulta.Tamanio,
            total);
    }
    //[FIN][16/9/2026][jgarciad8][Consulta paginada de usuarios con búsqueda]

    //[INICIO][16/9/2026][jgarciad8][Consulta individual sin exponer la contraseña]
    public Task<UsuarioResumenDto?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return Proyectar(db.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.IdUsuario == id))
            .SingleOrDefaultAsync(cancellationToken);
    }
    //[FIN][16/9/2026][jgarciad8][Consulta individual sin exponer la contraseña]

    //[INICIO][26/10/2026][Rodriale][Resumen con el rol y el nombre de la sede, en una sola consulta]
    private IQueryable<UsuarioResumenDto> Proyectar(IQueryable<Usuario> query) =>
        query.Select(usuario => new UsuarioResumenDto(
            usuario.IdUsuario,
            usuario.NombreCompleto,
            usuario.Correo,
            usuario.Activo,
            usuario.SucursalId,
            usuario.UsuarioRoles.OrderBy(ur => ur.Rol.Nombre).Select(ur => (int?)ur.RolId).FirstOrDefault(),
            usuario.UsuarioRoles.OrderBy(ur => ur.Rol.Nombre).Select(ur => ur.Rol.Nombre).FirstOrDefault(),
            db.Sucursales.Where(s => s.IdSucursal == usuario.SucursalId).Select(s => s.Nombre).FirstOrDefault()));
    //[FIN][26/10/2026][Rodriale][Resumen con el rol y el nombre de la sede]

    //[INICIO][16/9/2026][jgarciad8][Creación de usuarios con contraseña BCrypt]
    public async Task<UsuarioResumenDto> CrearAsync(
        CrearUsuarioRequest request,
        int usuarioActualId,
        CancellationToken cancellationToken)
    {
        
        var correoNormalizado = request.Correo.Trim().ToLowerInvariant();


        var correoExiste = await db.Usuarios
            .AsNoTracking()
            .AnyAsync(
                usuario => usuario.Correo.ToLower() == correoNormalizado,
                cancellationToken);

        if (correoExiste)
        {
            throw new ConflictoNegocioException(
                "Ya existe un usuario registrado con ese correo.");
        }

        //[INICIO][26/10/2026][Rodriale][Rol y sede se validan juntos: la sede solo se omite si el rol ve todas]
        await ValidarRolYSedeAsync(request.RolId, request.SucursalId, usuarioActualId, cancellationToken);
        //[FIN][26/10/2026][Rodriale][Rol y sede se validan juntos]

        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            Correo = correoNormalizado,
            PasswordHash = passwordService.CrearHash(request.Password),
            Activo = true,
            SucursalId = request.SucursalId
        };

        //[INICIO][26/10/2026][Rodriale][Usuario y rol en el mismo SaveChanges: o se guardan los dos o ninguno]
        usuario.UsuarioRoles.Add(new UsuarioRol { RolId = request.RolId });
        //[FIN][26/10/2026][Rodriale][Usuario y rol en el mismo SaveChanges]

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);

        return (await ObtenerPorIdAsync(usuario.IdUsuario, cancellationToken))!;
    }
    //[FIN][16/9/2026][jgarciad8][Creación de usuarios con contraseña BCrypt]

    //[INICIO][16/9/2026][jgarciad8][Actualización de nombre y correo del usuario]
    public async Task<bool> ActualizarAsync(
        int id,
        ActualizarUsuarioRequest request,
        int usuarioActualId,
        CancellationToken cancellationToken)
    {
        // Se conserva el seguimiento de EF Core para guardar los cambios.
        var usuario = await db.Usuarios
            .Include(usuario => usuario.UsuarioRoles)
            .SingleOrDefaultAsync(
                usuario => usuario.IdUsuario == id,
                cancellationToken);

        if (usuario is null)
        {
            return false;
        }

        var correoNormalizado = request.Correo.Trim().ToLowerInvariant();

        // El correo puede conservarse, pero no pertenecer a otro usuario.
        var correoExiste = await db.Usuarios
            .AsNoTracking()
            .AnyAsync(
                otroUsuario =>
                    otroUsuario.IdUsuario != id
                    && otroUsuario.Correo.ToLower() == correoNormalizado,
                cancellationToken);

        if (correoExiste)
        {
            throw new ConflictoNegocioException(
                "Ya existe otro usuario registrado con ese correo.");
        }

        //[INICIO][26/10/2026][Rodriale][Cambio de rol: nunca el propio, y se reemplaza el anterior en el mismo guardado]
        var cambiaRol = usuario.UsuarioRoles.Count != 1 || usuario.UsuarioRoles.First().RolId != request.RolId;
        if (cambiaRol && id == usuarioActualId)
        {
            throw new ConflictoNegocioException("No puedes cambiar tu propio rol.");
        }

        await ValidarRolYSedeAsync(
            request.RolId,
            request.SucursalId,
            cambiaRol ? usuarioActualId : null,
            cancellationToken);

        if (cambiaRol)
        {
            db.UsuarioRoles.RemoveRange(usuario.UsuarioRoles);
            db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = id, RolId = request.RolId });
        }
        //[FIN][26/10/2026][Rodriale][Cambio de rol]

        usuario.NombreCompleto = request.NombreCompleto.Trim();
        usuario.Correo = correoNormalizado;

        //[INICIO][12/10/2026][Rodriale][Sede del empleado]
        usuario.SucursalId = request.SucursalId;
        //[FIN][12/10/2026][Rodriale][Sede del empleado]

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
    //[FIN][16/9/2026][jgarciad8][Actualización de nombre y correo del usuario]

    //[INICIO][16/9/2026][jgarciad8][Activación y desactivación de cuentas]
    public async Task<bool> CambiarEstadoAsync(
        int id,
        bool activo,
        int usuarioActualId,
        CancellationToken cancellationToken)
    {
        // Evita que quien administra usuarios desactive su propia cuenta.
        if (id == usuarioActualId && !activo)
        {
            throw new ConflictoNegocioException(
                "No puedes desactivar tu propia cuenta.");
        }

        var usuario = await db.Usuarios
            .SingleOrDefaultAsync(
                usuario => usuario.IdUsuario == id,
                cancellationToken);

        if (usuario is null)
        {
            return false;
        }

        // Solicitar nuevamente el mismo estado no necesita otra escritura.
        if (usuario.Activo == activo)
        {
            return true;
        }

        usuario.Activo = activo;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
    //[FIN][16/9/2026][jgarciad8][Activación y desactivación de cuentas]

    //[INICIO][26/10/2026][Rodriale][Contraseña temporal: cambiar la versión de credenciales cierra sus sesiones abiertas]
    public async Task<bool> RestablecerPasswordAsync(
        int id,
        string password,
        CancellationToken cancellationToken)
    {
        var usuario = await db.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.IdUsuario == id, cancellationToken);

        if (usuario is null)
        {
            return false;
        }

        usuario.PasswordHash = passwordService.CrearHash(password);
        usuario.VersionCredenciales = Guid.NewGuid().ToString("N");

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
    //[FIN][26/10/2026][Rodriale][Contraseña temporal]

    //[INICIO][26/10/2026][Rodriale][Rol de empleado válido y sede obligatoria salvo para quien ve todas las sedes]
    private async Task ValidarRolYSedeAsync(
        int rolId,
        int? sucursalId,
        int? usuarioActualId,
        CancellationToken cancellationToken)
    {
        var rol = await db.Roles
            .AsNoTracking()
            .Where(r => r.IdRol == rolId)
            .Select(r => new
            {
                r.Nombre,
                VeTodas = r.RolPermisos.Any(rp => rp.Permiso.NombreCodigo == PermisosSistema.SucursalesTodas)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (rol is null || rol.Nombre == RegistroService.RolCliente)
        {
            throw new ConflictoNegocioException("Elige un rol del personal del taller.");
        }

        if (usuarioActualId is { } actual)
        {
            await rolService.ValidarRolAsignableAsync(rolId, actual, cancellationToken);
        }

        if (sucursalId is null && !rol.VeTodas)
        {
            throw new ConflictoNegocioException($"Elige la sede: el rol {rol.Nombre} trabaja en una sola sede.");
        }

        await ValidarSucursalAsync(sucursalId, cancellationToken);
    }
    //[FIN][26/10/2026][Rodriale][Rol de empleado válido y sede obligatoria]

    //[INICIO][12/10/2026][Rodriale][Solo se asigna una sede que exista y esté abierta]
    private async Task ValidarSucursalAsync(int? sucursalId, CancellationToken cancellationToken)
    {
        if (sucursalId is { } id &&
            !await db.Sucursales.AsNoTracking().AnyAsync(s => s.IdSucursal == id && s.Activo, cancellationToken))
        {
            throw new ConflictoNegocioException("La sucursal no existe o está dada de baja.");
        }
    }
    //[FIN][12/10/2026][Rodriale][Solo se asigna una sede que exista y esté abierta]

}
//[FIN][16/9/2026][jgarciad8][Servicio de consulta de usuarios]