//[INICIO][14/10/2026][Rodriale][Registro público del portal: crea la ficha del cliente y su cuenta de una sola vez]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public sealed class RegistroService(
    AppDbContext db,
    PasswordService passwordService,
    JwtService jwtService)
{
    //[INICIO][14/10/2026][Rodriale][El rol no trae permisos: un token de cliente no abre ningún endpoint del taller, solo el portal]
    public const string RolCliente = "Cliente";
    //[FIN][14/10/2026][Rodriale][El rol no trae permisos]

    public async Task<LoginResponse> RegistrarAsync(
        RegistroRequest request,
        CancellationToken cancellationToken)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        var nombre = request.NombreCompleto.Trim();

        if (await db.Usuarios.AsNoTracking()
                .AnyAsync(u => u.Correo == correo, cancellationToken))
        {
            throw new ConflictoNegocioException(
                "Ya existe una cuenta con ese correo. Inicia sesión o recupera tu contraseña.");
        }

        //[INICIO][14/10/2026][Rodriale][Si ya tiene ficha en el taller, NO se le vincula por correo: quien sepa el correo de un cliente se quedaría con su historial. La vinculación la hace el taller verificando identidad]
        if (await db.Clientes.AsNoTracking()
                .AnyAsync(c => c.Correo == correo, cancellationToken))
        {
            throw new ConflictoNegocioException(
                "Ese correo ya está registrado en el taller. Comunícate con nosotros para activar tu acceso.");
        }
        //[FIN][14/10/2026][Rodriale][Si ya tiene ficha en el taller, NO se le vincula por correo]

        var idRolCliente = await db.Roles.AsNoTracking()
            .Where(r => r.Nombre == RolCliente)
            .Select(r => r.IdRol)
            .SingleOrDefaultAsync(cancellationToken);

        if (idRolCliente == 0)
        {
            throw new InvalidOperationException(
                $"Falta el rol '{RolCliente}' en la base de datos; el registro del portal no puede operar sin él.");
        }

        var hash = passwordService.CrearHash(request.Password);

        //[INICIO][14/10/2026][Rodriale][La estrategia de reintentos de Npgsql exige envolver así las transacciones explícitas]
        var estrategia = db.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion =
                await db.Database.BeginTransactionAsync(cancellationToken);

            var cliente = new Cliente
            {
                NombreCompleto = nombre,
                Telefono = request.Telefono.Trim(),
                Correo = correo,
                Nit = null
            };

            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(cancellationToken);

            var usuario = new Usuario
            {
                NombreCompleto = nombre,
                Correo = correo,
                PasswordHash = hash,
                Activo = true,
                //[INICIO][14/10/2026][Rodriale][El cliente no pertenece a una sede: puede dejar su carro en cualquiera]
                SucursalId = null,
                //[FIN][14/10/2026][Rodriale][El cliente no pertenece a una sede]
                ClienteId = cliente.IdCliente
            };

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync(cancellationToken);

            db.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = usuario.IdUsuario,
                RolId = idRolCliente
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);

            //[INICIO][14/10/2026][Rodriale][Devolvemos token: registrarse y tener que iniciar sesión otra vez es un paso de más]
            return jwtService.CrearToken(
                new UsuarioSesionDto(usuario.IdUsuario, usuario.NombreCompleto, usuario.Correo, true),
                usuario.VersionCredenciales);
            //[FIN][14/10/2026][Rodriale][Devolvemos token]
        });
        //[FIN][14/10/2026][Rodriale][La estrategia de reintentos de Npgsql exige envolver así las transacciones explícitas]
    }
}
//[FIN][14/10/2026][Rodriale][Registro público del portal]
