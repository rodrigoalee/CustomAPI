//[INICIO][27/10/2026][Rodriale][Placa guatemalteca compartida por el portal y el panel: mismo formato y misma comparación al buscar duplicados]
using System.Text.RegularExpressions;
using CustomCore.API.Data;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public static partial class PlacaVehiculo
{
    [GeneratedRegex(@"^([A-Z]{1,3})(\d{3}[A-Z]{3})$")]
    private static partial Regex FormatoGuatemala();

    [GeneratedRegex(@"[\s-]")]
    private static partial Regex Separadores();

    public static string Normalizar(string placa)
    {
        var limpia = placa.Trim().ToUpperInvariant();
        var partes = FormatoGuatemala().Match(Separadores().Replace(limpia, ""));
        return partes.Success ? $"{partes.Groups[1].Value}-{partes.Groups[2].Value}" : limpia;
    }

    public static async Task ValidarQueNoExisteAsync(
        AppDbContext db,
        string placa,
        int? vehiculoExcluido,
        string mensaje,
        CancellationToken cancellationToken)
    {
        var sinSeparadores = Separadores().Replace(placa, "");

        if (await db.Vehiculos.AsNoTracking()
                .AnyAsync(v => v.Placa.ToUpper().Replace("-", "").Replace(" ", "") == sinSeparadores
                    && (vehiculoExcluido == null || v.IdVehiculo != vehiculoExcluido), cancellationToken))
            throw new ConflictoNegocioException(mensaje);
    }
}
//[FIN][27/10/2026][Rodriale][Placa guatemalteca compartida]
