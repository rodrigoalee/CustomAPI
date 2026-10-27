//[INICIO][25/10/2026][Rodriale][Avisos de citas por WhatsApp con la Cloud API de Meta; un aviso que no sale nunca tumba la cita]
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CustomCore.API.Configuracion;
using CustomCore.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomCore.API.Servicios;

public sealed partial class NotificacionWhatsAppService(
    AppDbContext db,
    HttpClient http,
    IOptions<OpcionesWhatsApp> opciones,
    ILogger<NotificacionWhatsAppService> logger)
{
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-GT");

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex TelefonoE164();

    private sealed record DatosAviso(
        string NombreCliente,
        string Telefono,
        string Placa,
        string Sede,
        DateTimeOffset FechaHora,
        string? Servicio,
        int? IdCita);

    public Task CitaAgendadaAsync(int idCita) =>
        EnviarAsync($"cita {idCita}", DeCita(idCita), datos =>
            $"Hola {datos.NombreCliente}, recibimos tu cita en Los Santos Customs para el {Fecha(datos)}" +
            $" en la sede {datos.Sede} con tu vehículo {datos.Placa}{Servicio(datos)}." +
            " Te avisaremos cuando el taller la confirme.");

    public Task CitaConfirmadaAsync(int idCita) =>
        EnviarAsync($"cita {idCita}", DeCita(idCita), datos =>
            $"Hola {datos.NombreCliente}, tu cita en Los Santos Customs para el {Fecha(datos)}" +
            $" en la sede {datos.Sede} fue confirmada. Te esperamos con tu vehículo {datos.Placa}.");

    //[INICIO][26/10/2026][Rodriale][Avisos del avance de la orden: los mismos pasos que ve el cliente en su seguimiento]
    public Task VehiculoRecibidoAsync(int idOrden) =>
        EnviarAsync($"orden {idOrden}", DeOrden(idOrden), datos =>
            $"Hola {datos.NombreCliente}, tu vehículo {datos.Placa} ya ingresó a Los Santos Customs en la sede {datos.Sede}." +
            " Te avisaremos cuando nuestros mecánicos empiecen a trabajar en él." + Enlace(datos));

    public Task TrabajoIniciadoAsync(int idOrden) =>
        EnviarAsync($"orden {idOrden}", DeOrden(idOrden), datos =>
            $"Hola {datos.NombreCliente}, nuestros mecánicos ya están trabajando en tu vehículo {datos.Placa}" +
            $"{Servicio(datos)}." + Enlace(datos));

    public Task TrabajoTerminadoAsync(int idOrden) =>
        EnviarAsync($"orden {idOrden}", DeOrden(idOrden), datos =>
            $"Hola {datos.NombreCliente}, terminamos el trabajo en tu vehículo {datos.Placa}." +
            $" Ya puedes pasar a recogerlo a la sede {datos.Sede}." + Enlace(datos));

    //[INICIO][26/10/2026][Rodriale][Cotización de trabajo adicional: el cliente la revisa y responde en su seguimiento]
    public Task CotizacionEnviadaAsync(int idOrden, decimal total) =>
        EnviarAsync($"orden {idOrden}", DeOrden(idOrden), datos =>
            $"Hola {datos.NombreCliente}, preparamos una cotización para tu vehículo {datos.Placa}" +
            $" por {total.ToString("C", Espanol)}. Revísala y dinos si la aceptas." + Enlace(datos));
    //[FIN][26/10/2026][Rodriale][Cotización de trabajo adicional]

    //[INICIO][26/10/2026][Rodriale][Al recoger el vehículo: total y enlace a la pasarela de pago]
    public Task FacturaListaAsync(int idOrden, decimal total, string token) =>
        EnviarAsync($"orden {idOrden}", DeOrden(idOrden), datos =>
            $"Hola {datos.NombreCliente}, tu vehículo {datos.Placa} está listo. El total es {total.ToString("C", Espanol)}." +
            $" Puedes pagar aquí: {opciones.Value.UrlPago.Replace("{token}", token)}");
    //[FIN][26/10/2026][Rodriale][Al recoger el vehículo]

    private string Enlace(DatosAviso datos) =>
        datos.IdCita is { } idCita && !string.IsNullOrWhiteSpace(opciones.Value.UrlSeguimiento)
            ? $" Sigue el avance aquí: {opciones.Value.UrlSeguimiento.Replace("{idCita}", idCita.ToString())}"
            : string.Empty;

    private IQueryable<DatosAviso> DeCita(int idCita) =>
        db.Citas
            .AsNoTracking()
            .Where(c => c.IdCita == idCita)
            .Select(c => new DatosAviso(
                c.Vehiculo.Cliente.NombreCompleto,
                c.Vehiculo.Cliente.Telefono,
                c.Vehiculo.Placa,
                db.Sucursales.Where(s => s.IdSucursal == c.SucursalId).Select(s => s.Nombre).First(),
                c.FechaHora,
                c.Servicio != null ? c.Servicio.Nombre : null,
                c.IdCita));

    private IQueryable<DatosAviso> DeOrden(int idOrden) =>
        db.OrdenesTrabajoEncabezado
            .AsNoTracking()
            .Where(o => o.IdOrdenEncabezado == idOrden)
            .Select(o => new DatosAviso(
                o.Vehiculo.Cliente.NombreCompleto,
                o.Vehiculo.Cliente.Telefono,
                o.Vehiculo.Placa,
                db.Sucursales.Where(s => s.IdSucursal == o.SucursalId).Select(s => s.Nombre).First(),
                o.FechaIngreso,
                o.Cita != null && o.Cita.Servicio != null ? o.Cita.Servicio.Nombre : null,
                o.CitaId));
    //[FIN][26/10/2026][Rodriale][Avisos del avance de la orden]

    private static string Fecha(DatosAviso datos)
    {
        var local = TimeZoneInfo.ConvertTime(datos.FechaHora, HorarioTaller.Zona);
        return local.ToString("dddd d 'de' MMMM 'a las' h:mm tt", Espanol);
    }

    private static string Servicio(DatosAviso datos) =>
        datos.Servicio is null ? string.Empty : $" para {datos.Servicio}";

    private async Task EnviarAsync(string origen, IQueryable<DatosAviso> consulta, Func<DatosAviso, string> mensaje)
    {
        logger.LogInformation("Preparando aviso de WhatsApp de la {Origen}.", origen);
        var configuracion = opciones.Value;
        if (!configuracion.Configurado)
        {
            logger.LogWarning("WhatsApp no está configurado; no se envió el aviso de la {Origen}.", origen);
            return;
        }

        try
        {
            var datos = await consulta.FirstOrDefaultAsync();

            if (datos is null)
            {
                logger.LogWarning("No se encontró la {Origen} para armar el aviso de WhatsApp.", origen);
                return;
            }

            var telefono = datos.Telefono.Replace(" ", string.Empty).Replace("-", string.Empty);
            //[INICIO][26/10/2026][Rodriale][Clientes viejos guardados con 8 dígitos sin código: se asume Guatemala]
            if (telefono.Length == 8 && telefono.All(char.IsAsciiDigit))
                telefono = "+502" + telefono;
            //[FIN][26/10/2026][Rodriale][Clientes viejos guardados con 8 dígitos sin código]
            if (!TelefonoE164().IsMatch(telefono))
            {
                logger.LogWarning("El teléfono del cliente de la {Origen} no tiene código de país; no se envió el aviso.", origen);
                return;
            }

            //[INICIO][25/10/2026][Rodriale][Meta pide el número sin el "+"]
            var cuerpo = new
            {
                messaging_product = "whatsapp",
                to = telefono.TrimStart('+'),
                type = "text",
                text = new { body = mensaje(datos) }
            };
            //[FIN][25/10/2026][Rodriale][Meta pide el número sin el "+"]

            //[INICIO][26/10/2026][Rodriale][Se registra el JSON tal cual se envía a Meta para depurar rechazos]
            var json = JsonSerializer.Serialize(cuerpo);
            logger.LogInformation("Payload de WhatsApp de la {Origen}: {Json}", origen, json);
            //[FIN][26/10/2026][Rodriale][Se registra el JSON tal cual se envía a Meta]

            using var solicitud = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/{configuracion.VersionApi}/{configuracion.PhoneNumberId}/messages")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracion.Token);

            using var respuesta = await http.SendAsync(solicitud);
            var contenido = await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                logger.LogError("Meta rechazó el aviso de WhatsApp de la {Origen} ({Estado}): {Respuesta}",
                    origen, (int)respuesta.StatusCode, contenido);
                return;
            }

            logger.LogInformation("Aviso de WhatsApp de la {Origen} enviado: {Respuesta}", origen, contenido);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el aviso de WhatsApp de la {Origen}.", origen);
        }
    }
}
//[FIN][25/10/2026][Rodriale][Avisos de citas por WhatsApp con la Cloud API de Meta]
