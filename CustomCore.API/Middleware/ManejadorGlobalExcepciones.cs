//[INICIO][30/8/2026][Rodriale][Manejador único de excepciones]
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCore.API.Excepciones;
using Npgsql;

namespace CustomCore.API.Middleware
{
    //[INICIO][30/8/2026][Rodriale][Constructor de la clase ManejadorGlobalExcepciones que implementa IExceptionHandler]
    public sealed class ManejadorGlobalExcepciones(
        IProblemDetailsService problemDetailsService,
        IHostEnvironment entorno,
        ILogger<ManejadorGlobalExcepciones> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // [INICIO][30/8/2026][Rodriale][Si el cliente abortó la petición no hay a quién responder]
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                logger.LogInformation("Solicitud cancelada por el cliente en {Ruta}", httpContext.Request.Path);
                return false;
            }
            // [FIN][30/8/2026][Rodriale][Si el cliente abortó la petición no hay a quién responder]

            logger.LogError(exception, "Ocurrió una excepción no controlada en {Metodo} {Ruta}",
                httpContext.Request.Method, httpContext.Request.Path);

            var (estado, titulo, detalle) = Traducir(exception, entorno.IsDevelopment());

            httpContext.Response.StatusCode = estado;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = estado,
                    Title = titulo,
                    Detail = detalle,
                    Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
                }
            });
        }
        // [FIN][30/8/2026][Rodriale][Constructor de la clase ManejadorGlobalExcepciones que implementa IExceptionHandler]

        // [INICIO][30/8/2026][Rodriale][Método Traducir que traduce excepciones a códigos de estado HTTP y mensajes]
        private static (int Estado, string Titulo, string? Detalle) Traducir(Exception exception, bool esDesarrollo) =>
            exception switch
            {
                //[INICIO][31/8/2026][Rodriale][el cliente puede corregir y reintentar]
                ConflictoNegocioException conflicto
                    => (StatusCodes.Status409Conflict, "Conflicto de negocio", conflicto.Message),
                //[FIN][31/8/2026][Rodriale][el cliente puede corregir y reintentar]

                DbUpdateConcurrencyException
                    => (StatusCodes.Status409Conflict,
                        "Conflicto de concurrencia",
                        "El registro fue modificado por otro usuario. Vuelve a cargarlo e intenta de nuevo."),

                //[INICIO][30/8/2026][Rodriale][SaveChanges envuelve el error; ExecuteUpdate/ExecuteDelete lo lanzan directo]
                DbUpdateException { InnerException: PostgresException pg } => TraducirPostgres(pg, exception, esDesarrollo),
                PostgresException pg => TraducirPostgres(pg, exception, esDesarrollo),
                //[FIN][30/8/2026][Rodriale][SaveChanges envuelve el error; ExecuteUpdate/ExecuteDelete lo lanzan directo]

                //[INICIO][16/9/2026][Rodriale][Tarjeta rechazada: no es falla de Stripe sino de la tarjeta, el cliente puede intentar con otra]
                Stripe.StripeException { StripeError.Type: "card_error" } rechazo
                    => (StatusCodes.Status402PaymentRequired,
                        "Pago rechazado",
                        rechazo.StripeError.Message),
                //[FIN][16/9/2026][Rodriale][Tarjeta rechazada]


                //[INICIO][16/9/2026][Rodriale][Si Stripe rechaza la petición, el problema está en el proveedor o en la configuración, no en el cliente]
                Stripe.StripeException stripe
                    => (StatusCodes.Status502BadGateway,
                        "Error con el proveedor de pagos",
                        esDesarrollo ? stripe.Message : "No fue posible generar el pago en este momento."),
                //[FIN][16/9/2026][Rodriale][Si Stripe rechaza la petición, el problema está en el proveedor o en la configuración, no en el cliente]


                _ => (StatusCodes.Status500InternalServerError,
                      "Error interno del servidor",
                      esDesarrollo ? exception.ToString() : null)
            };

        private static (int Estado, string Titulo, string? Detalle) TraducirPostgres(
            PostgresException pg,
            Exception exception,
            bool esDesarrollo) =>
            pg.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation
                    => (StatusCodes.Status409Conflict,
                        "Registro duplicado",
                        $"Ya existe un registro con ese valor. Restricción: {pg.ConstraintName}."),

                PostgresErrorCodes.ForeignKeyViolation
                    => (StatusCodes.Status409Conflict,
                        "Referencia inválida",
                        $"El registro apunta a otro inexistente, o está siendo referenciado y no puede eliminarse. Restricción: {pg.ConstraintName}."),

                PostgresErrorCodes.CheckViolation
                    => (StatusCodes.Status400BadRequest,
                        "Datos inválidos",
                        $"Los datos violan una regla de negocio de la base. Restricción: {pg.ConstraintName}."),

                _ => (StatusCodes.Status500InternalServerError,
                      "Error interno del servidor",
                      esDesarrollo ? exception.ToString() : null)
            };
        // [FIN][30/8/2026][Rodriale][Método Traducir que traduce excepciones a códigos de estado HTTP y mensajes]
    }
    //[FIN][30/8/2026][Rodriale][Manejador único de excepciones]
}