//[INICIO][17/9/2026][jgarciad8][Emisión de JWT con versión de credenciales]
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CustomCore.API.Configuracion;
using CustomCore.API.Dtos;
using Microsoft.IdentityModel.Tokens;

namespace CustomCore.API.Seguridad;

public sealed class JwtService(
    OpcionesJwt opciones,
    SymmetricSecurityKey claveFirma)
{
    public const string ClaimVersionCredenciales = "cred_ver";

    public LoginResponse CrearToken(
        UsuarioSesionDto usuario,
        string passwordHash)
    {
        var ahora = DateTimeOffset.UtcNow;
        var expiracion = ahora.AddMinutes(opciones.DuracionMinutos);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                usuario.IdUsuario.ToString(CultureInfo.InvariantCulture)),
            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N")),
            new Claim(
                ClaimVersionCredenciales,
                CrearVersionCredenciales(usuario.IdUsuario, passwordHash))
        };

        var token = new JwtSecurityToken(
            issuer: opciones.Emisor,
            audience: opciones.Audiencia,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: expiracion.UtcDateTime,
            signingCredentials: new SigningCredentials(
                claveFirma,
                SecurityAlgorithms.HmacSha256));

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiracion,
            usuario);
    }

    private string CrearVersionCredenciales(
        int idUsuario,
        string passwordHash)
    {
        var datos = Encoding.UTF8.GetBytes(
            "credenciales-v1:" +
            idUsuario.ToString(CultureInfo.InvariantCulture) +
            ":" + passwordHash);

        var marca = HMACSHA256.HashData(claveFirma.Key, datos);

        return Base64UrlEncoder.Encode(marca);
    }

    public bool CoincideVersionCredenciales(
        string? version,
        int idUsuario,
        string passwordHash)
    {
        if (string.IsNullOrEmpty(version))
            return false;

        var esperada = CrearVersionCredenciales(idUsuario, passwordHash);

        if (version.Length != esperada.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(version),
            Encoding.UTF8.GetBytes(esperada));
    }
}
//[FIN][17/9/2026][jgarciad8][Emisión de JWT con versión de credenciales]