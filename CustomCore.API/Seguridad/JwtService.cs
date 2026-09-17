//[INICIO][16/9/2026][jgarciad8][Servicio para emitir tokens JWT]
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CustomCore.API.Configuracion;
using CustomCore.API.Dtos;
using Microsoft.IdentityModel.Tokens;

namespace CustomCore.API.Seguridad;

public sealed class JwtService(
    OpcionesJwt opciones,
    SymmetricSecurityKey claveFirma)
{
    //[INICIO][16/9/2026][jgarciad8][Creación de un token firmado con vencimiento]
    public LoginResponse CrearToken(UsuarioSesionDto usuario)
    {
        var ahora = DateTimeOffset.UtcNow;
        var vencimiento = ahora.AddMinutes(opciones.DuracionMinutos);

        var claims = new[]
        {
            
            new Claim(
                JwtRegisteredClaimNames.Sub,
                usuario.IdUsuario.ToString(CultureInfo.InvariantCulture)),

            
            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: opciones.Emisor,
            audience: opciones.Audiencia,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: vencimiento.UtcDateTime,
            signingCredentials: new SigningCredentials(
                claveFirma,
                SecurityAlgorithms.HmacSha256));

        var tokenSerializado = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new LoginResponse(
            tokenSerializado,
            "Bearer",
            vencimiento,
            usuario);
    }
    //[FIN][16/9/2026][jgarciad8][Creación de un token firmado con vencimiento]
}
//[FIN][16/9/2026][jgarciad8][Servicio para emitir tokens JWT]