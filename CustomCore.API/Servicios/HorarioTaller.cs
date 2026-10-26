//[INICIO][23/10/2026][Rodriale][Reglas de horario del portal: viven solo aquí, el front pinta lo que devuelve la disponibilidad]
namespace CustomCore.API.Servicios;

public static class HorarioTaller
{
    public const int CapacidadPorBloque = 3;
    public const int DuracionMinutos = 60;
    public const int DiasVentana = 60;
    public const int HoraApertura = 8;

    public static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");

    //[INICIO][23/10/2026][Rodriale][L–V 8:00–18:00 y sábado 8:00–13:00: el último bloque empieza una hora antes del cierre]
    public static IReadOnlyList<DateTimeOffset> BloquesDelDia(DateOnly fecha)
    {
        var cierre = fecha.DayOfWeek switch
        {
            DayOfWeek.Sunday => HoraApertura,
            DayOfWeek.Saturday => 13,
            _ => 18
        };

        var bloques = new List<DateTimeOffset>();
        for (var hora = HoraApertura; hora < cierre; hora++)
        {
            var local = fecha.ToDateTime(new TimeOnly(hora, 0));
            bloques.Add(new DateTimeOffset(local, Zona.GetUtcOffset(local)));
        }

        return bloques;
    }
    //[FIN][23/10/2026][Rodriale][L–V 8:00–18:00 y sábado 8:00–13:00]

    public static bool EsInicioDeBloque(DateTimeOffset fechaHora)
    {
        var local = TimeZoneInfo.ConvertTime(fechaHora, Zona);
        return BloquesDelDia(DateOnly.FromDateTime(local.DateTime)).Contains(fechaHora);
    }

    //[INICIO][23/10/2026][Rodriale][Desde la siguiente hora en punto: reservar la hora en curso deja al taller sin margen para prepararse]
    public static bool DentroDeVentana(DateTimeOffset inicio, DateTimeOffset ahora)
    {
        var local = TimeZoneInfo.ConvertTime(ahora, Zona);
        var siguienteHora = new DateTimeOffset(
            local.Year, local.Month, local.Day, local.Hour, 0, 0, local.Offset).AddHours(1);

        return inicio >= siguienteHora && inicio <= ahora.AddDays(DiasVentana);
    }
    //[FIN][23/10/2026][Rodriale][Desde la siguiente hora en punto]

    public static DateTimeOffset InicioDelDia(DateOnly fecha)
    {
        var local = fecha.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Zona.GetUtcOffset(local));
    }
}
//[FIN][23/10/2026][Rodriale][Reglas de horario del portal]
