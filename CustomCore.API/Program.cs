using System.Diagnostics;
using CustomCore.API.Data;
using CustomCore.API.Middleware;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using CustomCore.API.Seguridad;

var builder = WebApplication.CreateBuilder(args);

//[INICIO][27/8/2026][Rodriale][validacion al buscar la dirección de Postgres aplicando un "FailFast]
var connectionString = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Falta la cadena de conexión a la base de datos en el archivo de configuración.");
//[FIN][27/8/2026][Rodriale][validacion al buscar la dirección de Postgres aplicando un "FailFast]

//[INICIO][27/8/2026][Rodriale][si la conexión está inestable esperamos e intentamos hasta 3 veces esperando 5 segundos entre cada intento]
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));
//[FIN][27/8/2026][Rodriale][si la conexión está inestable esperamos e intentamos hasta 3 veces esperando 5 segundos entre cada intento]

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = contexto =>
        contexto.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier);

builder.Services.AddExceptionHandler<ManejadorGlobalExcepciones>();
//[FIN][27/8/2026][Rodriale][registramos el manejador global de excepciones para que capture cualquier fallo del pipeline]


//[INICIO][Rodriale][Registro de inyección de dependencias]
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<VehiculoService>();
builder.Services.AddScoped<RepuestoService>();
builder.Services.AddScoped<ServicioService>();
builder.Services.AddScoped<CitaService>();
builder.Services.AddScoped<OrdenTrabajoService>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<PagoStripeService>();
//[FIN][Rodriale][Registro de inyección de dependencias]

//[INICIO][31/8/2026][Rodriale][Enlaza la sección "Facturacion" de appsettings con las opciones de impuesto]
builder.Services.Configure<CustomCore.API.Configuracion.OpcionesFacturacion>(
    builder.Configuration.GetSection(CustomCore.API.Configuracion.OpcionesFacturacion.Seccion));
//[FIN][31/8/2026][Rodriale][Enlaza la sección "Facturacion"]


//[INICIO][16/9/2026][Rodriale][un solo cliente para toda la app. si falta la clave, la app no arranca en lugar de fallar al primer cobro]
builder.Services.Configure<CustomCore.API.Configuracion.OpcionesStripe>(
    builder.Configuration.GetSection(CustomCore.API.Configuracion.OpcionesStripe.Seccion));

var claveStripe = builder.Configuration["Stripe:ClaveSecreta"];
if (string.IsNullOrWhiteSpace(claveStripe))
    throw new InvalidOperationException("Falta la clave secreta de Stripe en la configuración.");

builder.Services.AddSingleton<Stripe.IStripeClient>(new Stripe.StripeClient(claveStripe));
//[FIN][16/9/2026][Rodriale][un solo cliente para toda la app. si falta la clave, la app no arranca en lugar de fallar al primer cobro]

//[INICIO][31/8/2026][Rodriale][CORS el navegador bloquea al frontend si el backend no lo autoriza explícitamente]
const string PoliticaCors = "FrontendCustomCore";

var origenesPermitidos = builder.Configuration
    .GetSection("Cors:OrigenesPermitidos")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy(PoliticaCors, politica => politica
    //[INICIO][31/8/2026][Rodriale][Vercel genera un subdominio distinto por rama, así que además de la lista fija se aceptan los *.vercel.app]
        .SetIsOriginAllowed(origen =>
            origenesPermitidos.Contains(origen) ||
            (Uri.TryCreate(origen, UriKind.Absolute, out var uri) && uri.Host.EndsWith(".vercel.app")))
        //[FIN][31/8/2026][Rodriale][Vercel genera un subdominio distinto por rama, así que además de la lista fija se aceptan los *.vercel.app]
        .AllowAnyHeader()
        .AllowAnyMethod()
        //[INICIO][31/8/2026][Rodriale][Necesario si el JWT termina viajando en cookie obliga a listar orígenes, nunca comodín]
        .AllowCredentials()));
//[FIN][31/8/2026][Rodriale][Necesario si el JWT termina viajando en cookie obliga a listar orígenes, nunca comodín]
//[FIN][31/8/2026][Rodriale][CORS el navegador bloquea al frontend si el backend no lo autoriza explícitamente]

//[INICIO][31/8/2026][Rodriale][Render corta el TLS antes de llegar a la app sin esto la app cree que todo llega por http]
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
//[FIN][31/8/2026][Rodriale][Render corta el TLS antes de llegar a la app sin esto la app cree que todo llega por http]

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//[INICIO][16/9/2026][jgarciad8][Registro de autenticación y servicios de seguridad]
builder.Services.AgregarSeguridad(builder.Configuration);
//[FIN][16/9/2026][jgarciad8][Registro de autenticación y servicios de seguridad]

var app = builder.Build();

app.UseForwardedHeaders();

//[INICIO][30/8/2026][Rodriale][Va de primero para que capture cualquier fallo del resto del pipeline]
app.UseExceptionHandler();
//[FIN][30/8/2026][Rodriale][Va de primero para que capture cualquier fallo del resto del pipeline]

app.UseSwagger();
app.UseSwaggerUI();


app.UseCors(PoliticaCors);


app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok( new { estado = "ok", fecha = DateTimeOffset.UtcNow }));

app.Run();
