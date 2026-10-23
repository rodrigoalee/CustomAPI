//[INICIO][23/10/2026][Rodriale][Login con Google del portal: valida el id_token y resuelve la cuenta, solo para clientes]
using CustomCore.API.Configuracion;
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public sealed class CuentaGoogleService(
    AppDbContext db,
    JwtService jwtService,
    OpcionesGoogle opcionesGoogle)
{
    private sealed record IdentidadGoogle(string Sub, string Correo, string Nombre);

    private sealed record CuentaEncontrada(
        int IdUsuario,
        string NombreCompleto,
        string Correo,
        bool Activo,
        int? ClienteId,
        string? GoogleId,
        string VersionCredenciales,
        bool TieneRolCliente,
        bool TieneOtroRol);

    public async Task<LoginResponse?> IniciarSesionAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var identidad = await ValidarAsync(request.IdToken);

        if (identidad is null)
            return null;

        //[INICIO][23/10/2026][Rodriale][Una sola consulta para las reglas 1 y 2: la fila con ese GoogleId va primero, porque el sub es la llave y el correo solo sirve para vincular]
        var cuenta = await db.Usuarios
            .AsNoTracking()
            .Where(u => u.GoogleId == identidad.Sub || u.Correo == identidad.Correo)
            .OrderBy(u => u.GoogleId == identidad.Sub ? 0 : 1)
            .Select(u => new CuentaEncontrada(
                u.IdUsuario,
                u.NombreCompleto,
                u.Correo,
                u.Activo,
                u.ClienteId,
                u.GoogleId,
                u.VersionCredenciales,
                u.UsuarioRoles.Any(ur => ur.Rol.Nombre == RegistroService.RolCliente),
                u.UsuarioRoles.Any(ur => ur.Rol.Nombre != RegistroService.RolCliente)))
            .FirstOrDefaultAsync(cancellationToken);
        //[FIN][23/10/2026][Rodriale][Una sola consulta para las reglas 1 y 2]

        if (cuenta is not null)
            return await IniciarSesionExistenteAsync(cuenta, identidad, cancellationToken);

        //[INICIO][23/10/2026][Rodriale][Misma regla que RegistroService: controlar el buzón no prueba ser el dueño del historial que el taller registró]
        if (await db.Clientes.AsNoTracking()
                .AnyAsync(c => c.Correo == identidad.Correo, cancellationToken))
        {
            throw new ConflictoNegocioException(
                "Ese correo ya está registrado en el taller. Comunícate con nosotros para activar tu acceso.");
        }
        //[FIN][23/10/2026][Rodriale][Misma regla que RegistroService]

        return await CrearCuentaAsync(identidad, cancellationToken);
    }

    //[INICIO][23/10/2026][Rodriale][Sin Audience, ValidateAsync comprueba la firma pero no para quién se emitió el token: aceptaría el id_token de cualquier otra app de Google y entraría a la cuenta de un cliente sin contraseña]
    private async Task<IdentidadGoogle?> ValidarAsync(string idToken)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [opcionesGoogle.ClientId]
                });
        }
        catch (InvalidJwtException)
        {
            return null;
        }

        //[INICIO][23/10/2026][Rodriale][Firma mala, aud ajeno y correo sin verificar son el mismo null: al cliente legítimo no le sirve el detalle y al atacante sí]
        if (!payload.EmailVerified
            || string.IsNullOrWhiteSpace(payload.Subject)
            || string.IsNullOrWhiteSpace(payload.Email))
        {
            return null;
        }
        //[FIN][23/10/2026][Rodriale][Firma mala, aud ajeno y correo sin verificar son el mismo null]

        var correo = payload.Email.Trim().ToLowerInvariant();

        //[INICIO][23/10/2026][Rodriale][Usuario.Correo mide 100; uno más largo no cabe en la cuenta, así que se rechaza en vez de reventar al guardar]
        if (correo.Length > 100)
            return null;
        //[FIN][23/10/2026][Rodriale][Usuario.Correo mide 100]

        var nombre = string.IsNullOrWhiteSpace(payload.Name) ? correo : payload.Name.Trim();

        return new IdentidadGoogle(
            payload.Subject,
            correo,
            nombre.Length > 150 ? nombre[..150] : nombre);
    }
    //[FIN][23/10/2026][Rodriale][Sin Audience, ValidateAsync no comprueba para quién se emitió el token]

    private async Task<LoginResponse?> IniciarSesionExistenteAsync(
        CuentaEncontrada cuenta,
        IdentidadGoogle identidad,
        CancellationToken cancellationToken)
    {
        //[INICIO][23/10/2026][Rodriale][Antes de escribir nada, igual que el login por contraseña: si no, se marcaría una cuenta inhabilitada y su token moriría en la primera petición]
        if (!cuenta.Activo)
            return null;
        //[FIN][23/10/2026][Rodriale][Antes de escribir nada]

        //[INICIO][23/10/2026][Rodriale][Solo cliente puro: UsuarioRol es many-to-many y un empleado puede cargar también el rol Cliente. Aplica también a la cuenta ya vinculada, por si después le asignaron un rol del taller]
        if (!cuenta.TieneRolCliente || cuenta.TieneOtroRol)
        {
            throw new AccesoDenegadoException(
                "Esta cuenta inicia sesión con su correo y contraseña.");
        }
        //[FIN][23/10/2026][Rodriale][Solo cliente puro]

        if (cuenta.GoogleId is null)
        {
            //[INICIO][23/10/2026][Rodriale][Vincular: la cuenta la creó la propia persona con ese correo, así que controlar el buzón sí prueba que es la misma. Auditado, y condicionado a que nadie la haya vinculado o inhabilitado mientras tanto]
            var filas = await db.Usuarios
                .Where(u =>
                    u.IdUsuario == cuenta.IdUsuario
                    && u.Activo
                    && u.GoogleId == null)
                .ActualizarAuditadoAsync(db, cuenta.IdUsuario,
                    cambios => cambios.SetProperty(u => u.GoogleId, identidad.Sub),
                    cancellationToken);

            if (filas != 1)
            {
                throw new ConflictoNegocioException(
                    "La cuenta cambió durante la operación. Vuelve a intentarlo.");
            }
            //[FIN][23/10/2026][Rodriale][Vincular]
        }
        else if (cuenta.GoogleId != identidad.Sub)
        {
            //[INICIO][23/10/2026][Rodriale][El correo coincide pero la cuenta ya está vinculada a otra cuenta de Google: no se reemplaza el vínculo]
            throw new ConflictoNegocioException(
                "Ese correo ya está vinculado a otra cuenta de Google.");
            //[FIN][23/10/2026][Rodriale][El correo coincide pero la cuenta ya está vinculada a otra cuenta de Google]
        }

        //[INICIO][23/10/2026][Rodriale][Nombre y correo salen de la base, no del token: no se sincronizan con Google, eso lo decide el taller]
        return jwtService.CrearToken(
            new UsuarioSesionDto(
                cuenta.IdUsuario,
                cuenta.NombreCompleto,
                cuenta.Correo,
                cuenta.ClienteId is not null),
            cuenta.VersionCredenciales);
        //[FIN][23/10/2026][Rodriale][Nombre y correo salen de la base]
    }

    private async Task<LoginResponse> CrearCuentaAsync(
        IdentidadGoogle identidad,
        CancellationToken cancellationToken)
    {
        var idRolCliente = await db.Roles.AsNoTracking()
            .Where(r => r.Nombre == RegistroService.RolCliente)
            .Select(r => r.IdRol)
            .SingleOrDefaultAsync(cancellationToken);

        if (idRolCliente == 0)
        {
            throw new InvalidOperationException(
                $"Falta el rol '{RegistroService.RolCliente}' en la base de datos; el login con Google no puede operar sin él.");
        }

        //[INICIO][23/10/2026][Rodriale][La estrategia de reintentos de Npgsql exige envolver así las transacciones explícitas]
        var estrategia = db.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion =
                await db.Database.BeginTransactionAsync(cancellationToken);

            //[INICIO][23/10/2026][Rodriale][Sin ficha de Cliente todavía: Cliente.Telefono es obligatorio y Google no lo da. El rol sí va desde ya, para distinguirla de una cuenta de empleado]
            var usuario = new Usuario
            {
                NombreCompleto = identidad.Nombre,
                Correo = identidad.Correo,
                PasswordHash = null,
                GoogleId = identidad.Sub,
                Activo = true,
                SucursalId = null,
                ClienteId = null
            };
            //[FIN][23/10/2026][Rodriale][Sin ficha de Cliente todavía]

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync(cancellationToken);

            db.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = usuario.IdUsuario,
                RolId = idRolCliente
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);

            return jwtService.CrearToken(
                new UsuarioSesionDto(usuario.IdUsuario, usuario.NombreCompleto, usuario.Correo, false),
                usuario.VersionCredenciales);
        });
        //[FIN][23/10/2026][Rodriale][La estrategia de reintentos de Npgsql exige envolver así las transacciones explícitas]
    }
}
//[FIN][23/10/2026][Rodriale][Login con Google del portal]
