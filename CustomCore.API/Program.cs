using CustomCore.API.Data;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
