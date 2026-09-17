//[INICIO][16/9/2026][jgarciad8][Registro de servicios y configuración de autenticación JWT]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Configuracion;
using CustomCore.API.Data;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using CustomCore.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace CustomCore.API.Seguridad;

public static class ConfiguracionSeguridad
{
    public static IServiceCollection AgregarSeguridad(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        //[INICIO][16/9/2026][jgarciad8][Lectura y validación de la configuración JWT]
        var opcionesJwt = configuracion
            .GetSection(OpcionesJwt.Seccion)
            .Get<OpcionesJwt>()
            ?? throw new InvalidOperationException(
                "Falta configurar la sección Jwt.");

        if (string.IsNullOrWhiteSpace(opcionesJwt.Clave)
            || string.IsNullOrWhiteSpace(opcionesJwt.Emisor)
            || string.IsNullOrWhiteSpace(opcionesJwt.Audiencia))
        {
            throw new InvalidOperationException(
                "Debes configurar Jwt:Clave, Jwt:Emisor y Jwt:Audiencia.");
        }

        if (opcionesJwt.DuracionMinutos is < 1 or > 60)
        {
            throw new InvalidOperationException(
                "Jwt:DuracionMinutos debe estar entre 1 y 60.");
        }

        byte[] bytesClave;

        try
        {
            bytesClave = Convert.FromBase64String(opcionesJwt.Clave);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Jwt:Clave debe ser una clave codificada en Base64.");
        }

        if (bytesClave.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Clave debe contener al menos 32 bytes.");
        }

        var claveFirma = new SymmetricSecurityKey(bytesClave);
        //[FIN][16/9/2026][jgarciad8][Lectura y validación de la configuración JWT]

        //[INICIO][16/9/2026][jgarciad8][Registro de servicios de seguridad]
        servicios.AddSingleton<PasswordService>();
        servicios.AddSingleton(new JwtService(opcionesJwt, claveFirma));
        servicios.AddScoped<AuthService>();
        //[FIN][16/9/2026][jgarciad8][Registro de servicios de seguridad]

        //[INICIO][16/9/2026][jgarciad8][Registro del servicio de usuarios y verificador de permisos]
        servicios.AddScoped<UsuarioService>();
        servicios.AddScoped<IAuthorizationHandler, PermisoHandler>();
        //[FIN][16/9/2026][jgarciad8][Registro del servicio de usuarios y verificador de permisos]

        //[INICIO][16/9/2026][jgarciad8][Registro del servicio de roles]
        servicios.AddScoped<RolService>();
        //[FIN][16/9/2026][jgarciad8][Registro del servicio de roles]

        //[INICIO][16/9/2026][jgarciad8][Validación de tokens recibidos por la API]
        servicios
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {

                opciones.MapInboundClaims = false;
                opciones.IncludeErrorDetails = false;

                opciones.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = opcionesJwt.Emisor,

                        ValidateAudience = true,
                        ValidAudience = opcionesJwt.Audiencia,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = claveFirma,

                        RequireSignedTokens = true,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,

                        ValidAlgorithms = new[]
                        {
                            SecurityAlgorithms.HmacSha256
                        },


                        ClockSkew = TimeSpan.Zero,

                        NameClaimType = JwtRegisteredClaimNames.Sub
                    };

                opciones.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async contexto =>
                    {
                        var identificador = contexto.Principal?
                            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                        if (!int.TryParse(identificador, out var idUsuario)
                            || idUsuario <= 0)
                        {
                            contexto.Fail("Token no válido.");
                            return;
                        }

                        var db = contexto.HttpContext.RequestServices
                            .GetRequiredService<AppDbContext>();


                        var usuarioActivo = await db.Usuarios
                            .AsNoTracking()
                            .AnyAsync(
                                u => u.IdUsuario == idUsuario && u.Activo,
                                contexto.HttpContext.RequestAborted);

                        if (!usuarioActivo)
                        {
                            contexto.Fail("Usuario no habilitado.");
                        }
                    }
                };
            });

        //[INICIO][16/9/2026][jgarciad8][Políticas de autorización por permisos]
        servicios.AddAuthorization(opciones =>
        {
            opciones.AddPolicy(
                PermisosSistema.UsuariosLeer,
                politica =>
                {
                    politica.RequireAuthenticatedUser();

                    politica.AddRequirements(
                        new PermisoRequirement(
                            PermisosSistema.UsuariosLeer));
                });
          
            //[INICIO][16/9/2026][jgarciad8][Política para crear usuarios]
            opciones.AddPolicy(
                PermisosSistema.UsuariosCrear,
                politica =>
                {
                    politica.RequireAuthenticatedUser();

                    politica.AddRequirements(
                        new PermisoRequirement(PermisosSistema.UsuariosCrear));
                });
            //[FIN][16/9/2026][jgarciad8][Política para crear usuarios]

            //[INICIO][16/9/2026][jgarciad8][Políticas para editar usuarios y administrar su estado]
            opciones.AddPolicy(
                PermisosSistema.UsuariosEditar,
                politica =>
                {
                    politica.RequireAuthenticatedUser();

                    politica.AddRequirements(
                        new PermisoRequirement(PermisosSistema.UsuariosEditar));
                });

            opciones.AddPolicy(
                PermisosSistema.UsuariosCambiarEstado,
                politica =>
                {
                    politica.RequireAuthenticatedUser();

                    politica.AddRequirements(
                        new PermisoRequirement(PermisosSistema.UsuariosCambiarEstado));
                });
            //[FIN][16/9/2026][jgarciad8][Políticas para editar usuarios y administrar su estado]

            //[INICIO][16/9/2026][jgarciad8][Políticas de roles y asignaciones]
            foreach (var codigo in new[]
            {
                    PermisosSistema.RolesLeer,
                    PermisosSistema.RolesCrear,
                    PermisosSistema.RolesEditar,
                    PermisosSistema.RolesEliminar,
                    PermisosSistema.UsuariosAsignarRoles
})
            {
                opciones.AddPolicy(codigo, politica =>
                {
                    politica.RequireAuthenticatedUser();
                    politica.AddRequirements(new PermisoRequirement(codigo));
                });
            }
            //[FIN][16/9/2026][jgarciad8][Políticas de roles y asignaciones]

        });
        //[FIN][16/9/2026][jgarciad8][Políticas de autorización por permisos]


        //[FIN][16/9/2026][jgarciad8][Validación de tokens recibidos por la API]

        //[INICIO][16/9/2026][jgarciad8][Habilitación de autenticación Bearer en Swagger]
        servicios.Configure<SwaggerGenOptions>(opciones =>
        {
            opciones.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Pega solamente el token, sin escribir Bearer."
                });

            opciones.OperationFilter<AutorizacionSwaggerFilter>();
        });
        //[FIN][16/9/2026][jgarciad8][Habilitación de autenticación Bearer en Swagger]

        //[INICIO][16/9/2026][jgarciad8][Título para errores de validación de autenticación]
        servicios.PostConfigure<ApiBehaviorOptions>(opciones =>
        {
            
            var crearRespuestaOriginal = opciones.InvalidModelStateResponseFactory;

            opciones.InvalidModelStateResponseFactory = contexto =>
            {
                var respuesta = crearRespuestaOriginal(contexto);

                
                if (contexto.ActionDescriptor is ControllerActionDescriptor accion
                    && accion.ControllerTypeInfo.AsType() == typeof(AuthController)
                    && respuesta is ObjectResult
                    {
                        Value: ValidationProblemDetails problema
                    })
                {
                    problema.Title = "Datos de entrada inválidos.";
                }

                return respuesta;
            };
        });
        //[FIN][16/9/2026][jgarciad8][Título para errores de validación de autenticación]

        return servicios;
    }
}
//[FIN][16/9/2026][jgarciad8][Registro de servicios y configuración de autenticación JWT]