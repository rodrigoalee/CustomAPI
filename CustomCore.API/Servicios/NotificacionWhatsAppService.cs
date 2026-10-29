//[INICIO][25/10/2026][Rodriale][Avisos de citas por WhatsApp con la Cloud API de Meta; un aviso que no sale nunca tumba la cita]
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CustomCore.API.Configuracion;
using CustomCore.API.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CustomCore.API.Servicios;

public sealed partial class NotificacionWhatsAppService(
    AppDbContext db,
    HttpClient http,
    IOptions<OpcionesWhatsApp> opciones,
    IOptions<OpcionesCorreo> opcionesCorreo,
    ILogger<NotificacionWhatsAppService> logger)
{
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-GT");

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex TelefonoE164();

    private sealed record DatosAviso(
        string NombreCliente,
        string Telefono,
        string Correo,
        string Placa,
        string Sede,
        DateTimeOffset FechaHora,
        int? IdCita);

    //[INICIO][29/10/2026][Rodriale][Texto y enlace por separado: el SMS los une y el correo pone el enlace en un botón]
    private sealed record Aviso(string Texto, string? Enlace = null, string? Boton = null, DiagnosticoAviso? Diagnostico = null)
    {
        public string TextoCompleto => Enlace is null ? Texto : $"{Texto} {Enlace}";
    }

    private sealed record DiagnosticoAviso(string? Texto, IReadOnlyList<FotoAviso> Fotos);

    private sealed record FotoAviso(string Url, string? Descripcion);
    //[FIN][29/10/2026][Rodriale][Texto y enlace por separado]

    //[INICIO][29/10/2026][Rodriale][Textos cortos: Tigo antepone "Volcan de Agua: " (16) y el SMS debe quedar en 160 caracteres]
    public Task CitaAgendadaAsync(int idCita) =>
        EnviarAsync($"cita {idCita}", "Recibimos tu cita", DeCita(idCita), datos => new Aviso(
            $"Hola {Nombre(datos)}, Los Santos Customs recibió tu cita del {Fecha(datos)} en {datos.Sede}." +
            " Te confirmaremos pronto."));

    public Task CitaConfirmadaAsync(int idCita) =>
        EnviarAsync($"cita {idCita}", "Tu cita fue confirmada", DeCita(idCita), datos => new Aviso(
            $"Hola {Nombre(datos)}, Los Santos Customs confirmó tu cita del {Fecha(datos)} en {datos.Sede}. ¡Te esperamos!"));

    //[INICIO][27/10/2026][Rodriale][Cotización previa a la visita para un servicio fuera del catálogo]
    public Task CotizacionCitaEnviadaAsync(int idCita, decimal total) =>
        EnviarAsync($"cita {idCita}", "Cotización de tu cita", DeCita(idCita), datos => new Aviso(
            $"Hola {Nombre(datos)}, Los Santos Customs te envió la cotización de tu cita: {total.ToString("C", Espanol)}.",
            Seguimiento(datos), "Ver cotización"));
    //[FIN][27/10/2026][Rodriale][Cotización previa a la visita]

    //[INICIO][27/10/2026][Rodriale][Aviso cuando el taller cancela la cita]
    public Task CitaCanceladaAsync(int idCita) =>
        EnviarAsync($"cita {idCita}", "Tu cita fue cancelada", DeCita(idCita), datos => new Aviso(
            $"Hola {Nombre(datos)}, Los Santos Customs canceló tu cita del {Fecha(datos)}." +
            " Si necesitas otra, agenda una nueva o contáctanos."));
    //[FIN][27/10/2026][Rodriale][Aviso cuando el taller cancela la cita]

    //[INICIO][26/10/2026][Rodriale][Avisos del avance de la orden: los mismos pasos que ve el cliente en su seguimiento]
    public Task VehiculoRecibidoAsync(int idOrden) =>
        EnviarAsync($"orden {idOrden}", "Tu vehículo ingresó al taller", DeOrden(idOrden), datos => new Aviso(
            $"Hola {Nombre(datos)}, tu {datos.Placa} ingresó a Los Santos Customs. Te avisaremos al empezar.",
            Seguimiento(datos), "Ver seguimiento"));

    //[INICIO][29/10/2026][Rodriale][El correo de inicio lleva el diagnóstico y las fotos que subió el mecánico]
    public async Task TrabajoIniciadoAsync(int idOrden)
    {
        var diagnostico = await DiagnosticoAsync(idOrden);
        await EnviarAsync($"orden {idOrden}", "Empezamos a trabajar en tu vehículo", DeOrden(idOrden), datos => new Aviso(
            $"Hola {Nombre(datos)}, Los Santos Customs ya está trabajando en tu {datos.Placa}.",
            Seguimiento(datos), "Ver seguimiento", diagnostico));
    }

    private async Task<DiagnosticoAviso?> DiagnosticoAsync(int idOrden)
    {
        try
        {
            var texto = await db.OrdenesTrabajoEncabezado
                .AsNoTracking()
                .Where(o => o.IdOrdenEncabezado == idOrden)
                .Select(o => o.Diagnostico)
                .FirstOrDefaultAsync();
            var fotos = await db.FotosOrden
                .AsNoTracking()
                .Where(f => f.OrdenTrabajoId == idOrden)
                .OrderBy(f => f.FechaSubida)
                .Select(f => new FotoAviso(f.Url, f.Descripcion))
                .ToListAsync();

            return string.IsNullOrWhiteSpace(texto) && fotos.Count == 0 ? null : new DiagnosticoAviso(texto, fotos);
        }
        catch (Exception ex)
        {
            // Sin el diagnóstico el aviso sale igual, solo con el texto.
            logger.LogError(ex, "No se pudo leer el diagnóstico de la orden {IdOrden} para el aviso.", idOrden);
            return null;
        }
    }
    //[FIN][29/10/2026][Rodriale][El correo de inicio lleva el diagnóstico y las fotos]

    public Task TrabajoTerminadoAsync(int idOrden) =>
        EnviarAsync($"orden {idOrden}", "Tu vehículo está listo", DeOrden(idOrden), datos => new Aviso(
            $"Hola {Nombre(datos)}, tu {datos.Placa} está listo. Ya puedes recogerlo en Los Santos Customs.",
            Seguimiento(datos), "Ver seguimiento"));

    //[INICIO][26/10/2026][Rodriale][Cotización de trabajo adicional: el cliente la revisa y responde en su seguimiento]
    public Task CotizacionEnviadaAsync(int idOrden, decimal total) =>
        EnviarAsync($"orden {idOrden}", "Cotización de trabajo adicional", DeOrden(idOrden), datos => new Aviso(
            $"Hola {Nombre(datos)}, Los Santos Customs te envió una cotización de {total.ToString("C", Espanol)}" +
            $" para tu {datos.Placa}.",
            Seguimiento(datos), "Ver cotización"));
    //[FIN][26/10/2026][Rodriale][Cotización de trabajo adicional]

    //[INICIO][26/10/2026][Rodriale][Al recoger el vehículo: total y enlace a la pasarela de pago]
    public Task FacturaListaAsync(int idOrden, decimal total, string token) =>
        EnviarAsync($"orden {idOrden}", "Total y enlace de pago", DeOrden(idOrden), datos => new Aviso(
            $"Los Santos Customs: paga {total.ToString("C", Espanol)} aquí:",
            opciones.Value.UrlPago.Replace("{token}", token), "Pagar ahora"));
    //[FIN][26/10/2026][Rodriale][Al recoger el vehículo]

    private string? Seguimiento(DatosAviso datos) =>
        datos.IdCita is { } idCita && !string.IsNullOrWhiteSpace(opciones.Value.UrlSeguimiento)
            ? opciones.Value.UrlSeguimiento.Replace("{idCita}", idCita.ToString())
            : null;

    private static string Nombre(DatosAviso datos) =>
        datos.NombreCliente.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? datos.NombreCliente;
    //[FIN][29/10/2026][Rodriale][Textos cortos]

    private IQueryable<DatosAviso> DeCita(int idCita) =>
        db.Citas
            .AsNoTracking()
            .Where(c => c.IdCita == idCita)
            .Select(c => new DatosAviso(
                c.Vehiculo.Cliente.NombreCompleto,
                c.Vehiculo.Cliente.Telefono,
                c.Vehiculo.Cliente.Correo,
                c.Vehiculo.Placa,
                db.Sucursales.Where(s => s.IdSucursal == c.SucursalId).Select(s => s.Nombre).First(),
                c.FechaHora,
                c.IdCita));

    private IQueryable<DatosAviso> DeOrden(int idOrden) =>
        db.OrdenesTrabajoEncabezado
            .AsNoTracking()
            .Where(o => o.IdOrdenEncabezado == idOrden)
            .Select(o => new DatosAviso(
                o.Vehiculo.Cliente.NombreCompleto,
                o.Vehiculo.Cliente.Telefono,
                o.Vehiculo.Cliente.Correo,
                o.Vehiculo.Placa,
                db.Sucursales.Where(s => s.IdSucursal == o.SucursalId).Select(s => s.Nombre).First(),
                o.FechaIngreso,
                o.CitaId));
    //[FIN][26/10/2026][Rodriale][Avisos del avance de la orden]

    private static string Fecha(DatosAviso datos)
    {
        var local = TimeZoneInfo.ConvertTime(datos.FechaHora, HorarioTaller.Zona);
        return local.ToString("dddd d/M h:mm tt", Espanol);
    }

    //[INICIO][28/10/2026][Rodriale][El mismo aviso sale por WhatsApp y por correo; si un canal falla, el otro sale igual]
    private async Task EnviarAsync(string origen, string asunto, IQueryable<DatosAviso> consulta, Func<DatosAviso, Aviso> mensaje)
    {
        logger.LogInformation("Preparando aviso de la {Origen}.", origen);
        if (!opciones.Value.Configurado && !opcionesCorreo.Value.Configurado)
        {
            logger.LogWarning("Ni WhatsApp ni el correo están configurados; no se envió el aviso de la {Origen}.", origen);
            return;
        }

        DatosAviso? datos;
        try
        {
            datos = await consulta.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron leer los datos del aviso de la {Origen}.", origen);
            return;
        }

        if (datos is null)
        {
            logger.LogWarning("No se encontró la {Origen} para armar el aviso.", origen);
            return;
        }

        var aviso = mensaje(datos);
        await Task.WhenAll(
            EnviarPorWhatsAppAsync(origen, datos.Telefono, aviso.TextoCompleto),
            EnviarPorCorreoAsync(origen, datos.NombreCliente, datos.Correo, asunto, aviso));
    }

    private async Task EnviarPorCorreoAsync(string origen, string nombre, string destino, string asunto, Aviso aviso)
    {
        var configuracion = opcionesCorreo.Value;
        if (!configuracion.Configurado)
        {
            logger.LogWarning("El correo no está configurado; no se envió el aviso de la {Origen}.", origen);
            return;
        }

        try
        {
            var correo = new MimeMessage();
            correo.From.Add(new MailboxAddress(configuracion.NombreRemitente, configuracion.Usuario));
            correo.To.Add(new MailboxAddress(nombre, destino));
            correo.Subject = $"{asunto} · {configuracion.NombreRemitente}";
            //[INICIO][29/10/2026][Rodriale][Correo en HTML con su versión en texto para los clientes que no muestran HTML]
            correo.Body = new BodyBuilder
            {
                TextBody = TextoCorreo(aviso),
                HtmlBody = HtmlCorreo(asunto, aviso)
            }.ToMessageBody();
            //[FIN][29/10/2026][Rodriale][Correo en HTML]

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(configuracion.Servidor, configuracion.Puerto, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(configuracion.Usuario, configuracion.Contrasena);
            await smtp.SendAsync(correo);
            await smtp.DisconnectAsync(true);

            logger.LogInformation("Aviso por correo de la {Origen} enviado.", origen);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el aviso por correo de la {Origen}.", origen);
        }
    }
    //[FIN][28/10/2026][Rodriale][El mismo aviso sale por WhatsApp y por correo]

    //[INICIO][29/10/2026][Rodriale][Plantilla del correo: una columna con tablas y estilos en línea, que es lo que respetan Gmail y Outlook]
    private static string TextoCorreo(Aviso aviso)
    {
        var texto = new StringBuilder(aviso.TextoCompleto);
        if (aviso.Diagnostico is { } diagnostico)
        {
            if (!string.IsNullOrWhiteSpace(diagnostico.Texto))
                texto.Append("\n\nDiagnóstico:\n").Append(diagnostico.Texto.Trim());
            if (diagnostico.Fotos.Count > 0)
                texto.Append($"\n\nEl mecánico adjuntó {diagnostico.Fotos.Count} foto(s); puedes verlas en tu seguimiento.");
        }
        return texto.ToString();
    }

    private static string HtmlCorreo(string asunto, Aviso aviso)
    {
        static string Html(string texto) => WebUtility.HtmlEncode(texto.Trim()).Replace("\n", "<br>");

        var cuerpo = new StringBuilder();
        cuerpo.Append($"""<p style="margin:0 0 16px;font-size:16px;line-height:24px">{Html(aviso.Texto)}</p>""");

        if (aviso.Enlace is { } enlace)
            cuerpo.Append($"""
                <table role="presentation" cellpadding="0" cellspacing="0" style="margin:8px 0 24px"><tr>
                <td style="background:#111827;border-radius:6px">
                <a href="{WebUtility.HtmlEncode(enlace)}" style="display:inline-block;padding:12px 24px;color:#ffffff;font-weight:bold;text-decoration:none">{Html(aviso.Boton ?? "Abrir")}</a>
                </td></tr></table>
                """);

        if (aviso.Diagnostico is { } diagnostico)
        {
            cuerpo.Append("""<h2 style="margin:24px 0 8px;font-size:18px">Diagnóstico del mecánico</h2>""");
            if (!string.IsNullOrWhiteSpace(diagnostico.Texto))
                cuerpo.Append($"""<p style="margin:0 0 16px;font-size:15px;line-height:22px">{Html(diagnostico.Texto)}</p>""");

            foreach (var foto in diagnostico.Fotos)
            {
                var descripcion = string.IsNullOrWhiteSpace(foto.Descripcion) ? "Foto del diagnóstico" : foto.Descripcion;
                cuerpo.Append($"""
                    <img src="{WebUtility.HtmlEncode(foto.Url)}" alt="{WebUtility.HtmlEncode(descripcion.Trim())}" width="552"
                        style="display:block;width:100%;max-width:552px;height:auto;border-radius:6px;margin:16px 0 6px">
                    """);
                if (!string.IsNullOrWhiteSpace(foto.Descripcion))
                    cuerpo.Append($"""<p style="margin:0 0 8px;font-size:14px;line-height:20px;color:#4b5563">{Html(foto.Descripcion)}</p>""");
            }
        }

        return $"""
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{Html(asunto)}</title></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:Arial,Helvetica,sans-serif;color:#111827">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6"><tr><td align="center" style="padding:24px 12px">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:600px;background:#ffffff;border-radius:8px;overflow:hidden">
            <tr><td style="background:#111827;padding:20px 24px;color:#ffffff;font-size:20px;font-weight:bold">Los Santos Customs</td></tr>
            <tr><td style="padding:24px">
            <h1 style="margin:0 0 16px;font-size:22px">{Html(asunto)}</h1>
            {cuerpo}
            </td></tr>
            <tr><td style="padding:16px 24px;background:#f9fafb;color:#6b7280;font-size:12px">Este es un aviso automático de Los Santos Customs.</td></tr>
            </table>
            </td></tr></table>
            </body></html>
            """;
    }
    //[FIN][29/10/2026][Rodriale][Plantilla del correo]

    private async Task EnviarPorWhatsAppAsync(string origen, string telefonoCliente, string texto)
    {
        var configuracion = opciones.Value;
        if (!configuracion.Configurado)
        {
            logger.LogWarning("WhatsApp no está configurado; no se envió el aviso de la {Origen}.", origen);
            return;
        }

        try
        {
            var telefono = telefonoCliente.Replace(" ", string.Empty).Replace("-", string.Empty);
            //[INICIO][26/10/2026][Rodriale][Clientes viejos guardados con 8 dígitos sin código: se asume Guatemala]
            if (telefono.Length == 8 && telefono.All(char.IsAsciiDigit))
                telefono = "+502" + telefono;
            //[FIN][26/10/2026][Rodriale][Clientes viejos guardados con 8 dígitos sin código]
            if (!TelefonoE164().IsMatch(telefono))
            {
                logger.LogWarning("El teléfono del cliente de la {Origen} no tiene código de país; no se envió el aviso.", origen);
                return;
            }

            //[INICIO][29/10/2026][Rodriale][Con Tigo el aviso sale por SMS en lugar de WhatsApp]
            if (configuracion.UsaTigo)
            {
                await EnviarPorTigoAsync(configuracion, origen, telefono, texto);
                return;
            }
            //[FIN][29/10/2026][Rodriale][Con Tigo el aviso sale por SMS]

            //[INICIO][27/10/2026][Rodriale][Con Twilio el mensaje sale por su API y el resto del flujo queda igual]
            if (configuracion.UsaTwilio)
            {
                await EnviarPorTwilioAsync(configuracion, origen, telefono, texto);
                return;
            }
            //[FIN][27/10/2026][Rodriale][Con Twilio el mensaje sale por su API]

            //[INICIO][25/10/2026][Rodriale][Meta pide el número sin el "+"]
            var cuerpo = new
            {
                messaging_product = "whatsapp",
                to = telefono.TrimStart('+'),
                type = "text",
                text = new { body = texto }
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

    //[INICIO][27/10/2026][Rodriale][Twilio recibe formulario con From/To "whatsapp:+número" y autenticación básica SID:token]
    private async Task EnviarPorTwilioAsync(OpcionesWhatsApp configuracion, string origen, string telefono, string texto)
    {
        var desde = configuracion.TwilioNumero.StartsWith("whatsapp:") ? configuracion.TwilioNumero : $"whatsapp:{configuracion.TwilioNumero}";

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.twilio.com/2010-04-01/Accounts/{configuracion.TwilioAccountSid}/Messages.json")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = desde,
                ["To"] = $"whatsapp:{telefono}",
                ["Body"] = texto
            })
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuracion.TwilioAccountSid}:{configuracion.TwilioAuthToken}")));

        using var respuesta = await http.SendAsync(solicitud);
        var contenido = await respuesta.Content.ReadAsStringAsync();

        if (!respuesta.IsSuccessStatusCode)
        {
            logger.LogError("Twilio rechazó el aviso de WhatsApp de la {Origen} ({Estado}): {Respuesta}",
                origen, (int)respuesta.StatusCode, contenido);
            return;
        }

        logger.LogInformation("Aviso de WhatsApp de la {Origen} enviado por Twilio.", origen);
    }
    //[FIN][27/10/2026][Rodriale][Twilio]

    //[INICIO][29/10/2026][Rodriale][SMS por Tigo: token OAuth por client_credentials y luego el mensaje con APIKey/APISecret]
    // El token dura cerca de una hora; se guarda entre avisos para no pedir uno por cada SMS.
    private static (string Valor, DateTimeOffset Vence)? tokenTigo;

    private async Task EnviarPorTigoAsync(OpcionesWhatsApp configuracion, string origen, string telefono, string texto)
    {
        var token = await TokenTigoAsync(configuracion);
        var cuerpo = new
        {
            protocol = "sms",
            shortcodeId = configuracion.TigoShortcode,
            shortcodeType = configuracion.TigoTipoShortcode,
            msisdn = telefono.TrimStart('+'),
            priority = 0,
            body = texto
        };

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            $"{configuracion.TigoUrlBase}/v1/tigo/b2b/gt/comcorp/messages/organizations/{configuracion.TigoOrganizacion}")
        {
            Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json")
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        solicitud.Headers.Add("APIKey", configuracion.TigoApiKey);
        solicitud.Headers.Add("APISecret", configuracion.TigoApiSecret);

        using var respuesta = await http.SendAsync(solicitud);
        var contenido = await respuesta.Content.ReadAsStringAsync();

        if (!respuesta.IsSuccessStatusCode)
        {
            if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                tokenTigo = null;
            logger.LogError("Tigo rechazó el SMS de la {Origen} ({Estado}): {Respuesta}",
                origen, (int)respuesta.StatusCode, contenido);
            return;
        }

        logger.LogInformation("SMS de la {Origen} enviado por Tigo: {Respuesta}", origen, contenido);
    }

    private async Task<string> TokenTigoAsync(OpcionesWhatsApp configuracion)
    {
        if (tokenTigo is { } guardado && guardado.Vence > DateTimeOffset.UtcNow)
            return guardado.Valor;

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            $"{configuracion.TigoUrlBase}/oauth/client_credential/accesstoken?grant_type=client_credentials");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Basic", configuracion.TigoAutenticacion);

        using var respuesta = await http.SendAsync(solicitud);
        var contenido = await respuesta.Content.ReadAsStringAsync();
        if (!respuesta.IsSuccessStatusCode)
            throw new HttpRequestException($"Tigo no entregó el token ({(int)respuesta.StatusCode}): {contenido}");

        using var json = JsonDocument.Parse(contenido);
        var valor = json.RootElement.GetProperty("access_token").GetString()
            ?? throw new HttpRequestException("Tigo respondió sin access_token.");

        // Apigee manda expires_in como texto ("3599"); se renueva un minuto antes de que venza.
        if (json.RootElement.TryGetProperty("expires_in", out var expira)
            && int.TryParse(expira.ToString(), out var segundos) && segundos > 60)
            tokenTigo = (valor, DateTimeOffset.UtcNow.AddSeconds(segundos - 60));

        return valor;
    }
    //[FIN][29/10/2026][Rodriale][SMS por Tigo]
}
//[FIN][25/10/2026][Rodriale][Avisos de citas por WhatsApp con la Cloud API de Meta]
