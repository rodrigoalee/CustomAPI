//[INICIO][16/9/2026][jgarciad8][Servicio para generar y verificar contraseñas con BCrypt]
using System.Text;

namespace CustomCore.API.Seguridad;

public sealed class PasswordService
{
    private const int LongitudMinima = 12;
    private const int MaximoBytes = 72;
    private const int FactorTrabajo = 12;

    //[INICIO][16/9/2026][jgarciad8][Validación de contraseñas nuevas]
    public static bool EsValida(string? password)
    {
 
        if (string.IsNullOrWhiteSpace(password)
            || password.Length < LongitudMinima
            || password.Length > MaximoBytes)
        {
            return false;
        }

        return !password.Contains('\0')
            && Encoding.UTF8.GetByteCount(password) <= MaximoBytes;
    }
    //[FIN][16/9/2026][jgarciad8][Validación de contraseñas nuevas]

    //[INICIO][16/9/2026][jgarciad8][Generación del hash de una contraseña]
    public string CrearHash(string password)
    {

        if (!EsValida(password))
        {
            throw new ArgumentException(
                "La contraseña debe tener al menos 12 caracteres, "
                + "no superar 72 bytes en UTF-8 y no contener caracteres nulos.",
                nameof(password));
        }


        return BCrypt.Net.BCrypt.HashPassword(
            password,
            workFactor: FactorTrabajo);
    }
    //[FIN][16/9/2026][jgarciad8][Generación del hash de una contraseña]

    //[INICIO][16/9/2026][jgarciad8][Verificación de una contraseña contra su hash]
    public bool Verificar(string? password, string? hash)
    {
        if (string.IsNullOrEmpty(password)
            || password.Length > MaximoBytes
            || password.Contains('\0')
            || Encoding.UTF8.GetByteCount(password) > MaximoBytes
            || string.IsNullOrWhiteSpace(hash)
            || hash.Length != 60)
        {
            return false;
        }

        try
        {

            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex) when (
            ex is BCrypt.Net.SaltParseException
            or ArgumentException
            or FormatException)
        {
            return false;
        }
    }
    //[FIN][16/9/2026][jgarciad8][Verificación de una contraseña contra su hash]
}
//[FIN][16/9/2026][jgarciad8][Servicio para generar y verificar contraseñas con BCrypt]