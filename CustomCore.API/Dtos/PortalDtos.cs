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

    public record AgendarCitaRequest(
        [Range(1, int.MaxValue)] int VehiculoId,
        [Range(1, int.MaxValue)] int SucursalId,
        [Range(1, int.MaxValue)] int ServicioId,
        DateTimeOffset FechaHora);

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
        bool Pagado);
    //[FIN][24/10/2026][Rodriale][Seguimiento de una cita]
}
//[FIN][23/10/2026][Rodriale][DTOs del portal del cliente]
