//[INICIO][23/10/2026][Rodriale][DTOs del portal del cliente]
using System.ComponentModel.DataAnnotations;
using CustomCore.API.Entidades;

namespace CustomCore.API.Dtos
{
    public record CitaPortalDto(
        int IdCita,
        string? Servicio,
        string? Motivo,
        string Marca,
        string Modelo,
        int Anio,
        string Placa,
        DateTimeOffset FechaHora,
        EstadoCita Estado);

    public record PerfilPortalDto(
        string NombreCompleto,
        string Correo,
        string Telefono);

    public record VehiculoPortalDto(
        int IdVehiculo,
        string Marca,
        string Modelo,
        int Anio,
        string Placa);

    public record RegistrarVehiculoRequest(
        [Required, MaxLength(20)] string Placa,
        [Required, MaxLength(50)] string Marca,
        [Required, MaxLength(50)] string Modelo,
        [Range(1900, 2100)] int Anio);

    public record SucursalPortalDto(
        int IdSucursal,
        string Nombre,
        string Direccion);

    public record ServicioPortalDto(
        int IdServicio,
        string Nombre,
        decimal TarifaManoObra);

    public record ConsultaDisponibilidad
    {
        [Range(1, int.MaxValue)]
        public int SucursalId { get; init; }

        public DateOnly Desde { get; init; }
        public DateOnly Hasta { get; init; }
    }

    public record HorarioDisponibleDto(
        DateTimeOffset FechaHora,
        bool Disponible,
        bool Lleno);

    //[INICIO][27/10/2026][Rodriale][Servicio del catálogo o, si no le sirve ninguno, lo que necesita descrito por el cliente]
    public record AgendarCitaRequest(
        [Range(1, int.MaxValue)] int VehiculoId,
        [Range(1, int.MaxValue)] int SucursalId,
        [Range(1, int.MaxValue)] int? ServicioId,
        DateTimeOffset FechaHora,
        [MaxLength(500)] string? DescripcionServicio = null) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ServicioId is null == string.IsNullOrWhiteSpace(DescripcionServicio))
                yield return new ValidationResult(
                    "Elige un servicio del catálogo o describe lo que necesitas.",
                    [nameof(ServicioId), nameof(DescripcionServicio)]);
        }
    }
    //[FIN][27/10/2026][Rodriale][Servicio del catálogo o descrito por el cliente]

    public record DiaDisponibilidadDto(
        DateOnly Fecha,
        IReadOnlyList<HorarioDisponibleDto> Horarios);

    //[INICIO][24/10/2026][Rodriale][Seguimiento de una cita: los datos de la orden vienen nulos mientras el carro no haya llegado]
    public record SeguimientoCitaDto(
        int IdCita,
        string? Servicio,
        string? Motivo,
        string Marca,
        string Modelo,
        int Anio,
        string Placa,
        string Sucursal,
        DateTimeOffset FechaHora,
        EstadoCita Estado,
        EstadoOrdenTrabajo? EstadoOrden,
        DateTimeOffset? FechaIngreso,
        DateTimeOffset? FechaFinalizacion,
        bool Pagado,
        //[INICIO][29/10/2026][Rodriale][El cliente ve el diagnóstico del mecánico con sus fotos]
        string? Diagnostico,
        IReadOnlyList<FotoOrdenDto> Fotos);
        //[FIN][29/10/2026][Rodriale][El cliente ve el diagnóstico del mecánico con sus fotos]
    //[FIN][24/10/2026][Rodriale][Seguimiento de una cita]

    //[INICIO][26/10/2026][Rodriale][Facturas del cliente en su portal]
    public record FacturaPortalDto(
        int IdFactura,
        DateTimeOffset FechaEmision,
        string Placa,
        decimal Total,
        bool Pagada);

    public record EnlacePagoDto(string Token);
    //[FIN][26/10/2026][Rodriale][Facturas del cliente en su portal]
}
//[FIN][23/10/2026][Rodriale][DTOs del portal del cliente]
