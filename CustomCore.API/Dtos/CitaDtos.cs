//[INICIO][30/8/2026][Rodriale][DTOs del módulo de citas]
using System.ComponentModel.DataAnnotations;
using CustomCore.API.Entidades;

namespace CustomCore.API.Dtos
{
    //[INICIO][31/8/2026][Rodriale][Filtros de agenda: por vehículo, por rango de fechas y por estado]
    public record ConsultaCitas : ConsultaPaginada
    {
        [Range(1, int.MaxValue)]
        public int? VehiculoId { get; init; }

        public DateTimeOffset? Desde { get; init; }
        public DateTimeOffset? Hasta { get; init; }
        public EstadoCita? Estado { get; init; }
    }
    //[FIN][31/8/2026][Rodriale][Filtros de agenda: por vehículo, por rango de fechas y por estado]

    public record CitaListaDto(
        int IdCita,
        int VehiculoId,
        string Placa,
        string NombreCliente,
        DateTimeOffset FechaHora,
        int DuracionMinutos,
        EstadoCita Estado);

    public record CitaDetalleDto(
        int IdCita,
        int VehiculoId,
        string Placa,
        string Marca,
        string Modelo,
        int ClienteId,
        string NombreCliente,
        string TelefonoCliente,
        DateTimeOffset FechaHora,
        int DuracionMinutos,
        string? Motivo,
        EstadoCita Estado);

    public record CrearCitaRequest(
        [Range(1, int.MaxValue)] int VehiculoId,
        DateTimeOffset FechaHora,
        [Range(15, 480)] int DuracionMinutos,
        [MaxLength(500)] string? Motivo);

    public record ReprogramarCitaRequest(
        DateTimeOffset FechaHora,
        [Range(15, 480)] int DuracionMinutos,
        [MaxLength(500)] string? Motivo);

    public record CambiarEstadoCitaRequest(EstadoCita Estado);
}
//[FIN][31/8/2026][Rodriale][DTOs del módulo de citas]