//[INICIO][26/10/2026][Rodriale][Enlace de pago firmado: abre la pasarela de UNA factura, sin cuenta, y vence]
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CustomCore.API.Seguridad;

/// <summary>
/// Token "{facturaId}.{vencimiento}.{firma}". La firma es un HMAC con la clave del JWT y un propósito
/// propio, así que no sirve como token de sesión ni se puede alterar el id o el vencimiento.
/// </summary>
public sealed class EnlacePago(byte[] clave)
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromHours(48);

    public string Crear(int facturaId)
    {
        var vence = DateTimeOffset.UtcNow.Add(Vigencia).ToUnixTimeSeconds();
        var datos = $"{facturaId.ToString(CultureInfo.InvariantCulture)}.{vence.ToString(CultureInfo.InvariantCulture)}";
        return $"{datos}.{Firmar(datos)}";
    }

    /// <returns>El id de la factura, o null si el token está alterado o vencido.</returns>
    public int? Validar(string token)
    {
        var partes = token.Split('.');
        if (partes.Length != 3
            || !int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out var facturaId)
            || !long.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out var vence))
            return null;

        var esperada = Encoding.ASCII.GetBytes(Firmar($"{partes[0]}.{partes[1]}"));
        var recibida = Encoding.ASCII.GetBytes(partes[2]);
        if (!CryptographicOperations.FixedTimeEquals(esperada, recibida))
            return null;

        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= vence ? facturaId : null;
    }

    private string Firmar(string datos) =>
        Base64UrlEncoder.Encode(HMACSHA256.HashData(clave, Encoding.UTF8.GetBytes("enlace-pago-v1:" + datos)));
}
//[FIN][26/10/2026][Rodriale][Enlace de pago firmado]
