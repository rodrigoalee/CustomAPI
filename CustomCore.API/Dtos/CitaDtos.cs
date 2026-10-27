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

        //[INICIO][12/10/2026][Rodriale][Filtro por sede; solo lo respeta quien ve todas]
        [Range(1, int.MaxValue)]
        public int? SucursalId { get; init; }

        //[INICIO][25/10/2026][Rodriale][Citas a las que todavía no se les abrió orden, para el tablero de recepción]
        public bool SinOrden { get; init; }
        //[FIN][25/10/2026][Rodriale][Citas a las que todavía no se les abrió orden]
        //[FIN][12/10/2026][Rodriale][Filtro por sede]
    }
    //[FIN][31/8/2026][Rodriale][Filtros de agenda: por vehículo, por rango de fechas y por estado]

    public record CitaListaDto(
        int IdCita,
        int SucursalId,
        int VehiculoId,
        string Placa,
        string NombreCliente,
        DateTimeOffset FechaHora,
        int DuracionMinutos,
        EstadoCita Estado,
        //[INICIO][25/10/2026][Rodriale][Servicio pedido y orden abierta, si la hay]
        string? Servicio,
        int? IdOrden);
        //[FIN][25/10/2026][Rodriale][Servicio pedido y orden abierta]

    public record CitaDetalleDto(
        int IdCita,
        int SucursalId,
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
        EstadoCita Estado,
        //[INICIO][25/10/2026][Rodriale][Servicio pedido y orden abierta, si la hay]
        string? Servicio,
        int? IdOrden);
        //[FIN][25/10/2026][Rodriale][Servicio pedido y orden abierta]

    public record CrearCitaRequest(
        [Range(1, int.MaxValue)] int VehiculoId,
        DateTimeOffset FechaHora,
        [Range(15, 480)] int DuracionMinutos,
        [MaxLength(500)] string? Motivo,
        //[INICIO][12/10/2026][Rodriale][Solo lo usa quien ve todas las sedes; al empleado se le asigna la suya]
        [Range(1, int.MaxValue)] int? SucursalId = null,
        //[FIN][12/10/2026][Rodriale][Solo lo usa quien ve todas las sedes]
        //[INICIO][26/10/2026][Rodriale][Servicio pedido en el mostrador, igual que en el portal]
        [Range(1, int.MaxValue)] int? ServicioId = null);
        //[FIN][26/10/2026][Rodriale][Servicio pedido en el mostrador]

    public record ReprogramarCitaRequest(
        DateTimeOffset FechaHora,
        [Range(15, 480)] int DuracionMinutos,
        [MaxLength(500)] string? Motivo);

    //[INICIO][17/9/2026][jgarciad8][Validación de estados permitidos para citas]
    public record CambiarEstadoCitaRequest(
        [EnumDataType(typeof(EstadoCita),
        ErrorMessage = "El estado de la cita no es válido.")]
    EstadoCita Estado);
    //[FIN][17/9/2026][jgarciad8][Validación de estados permitidos para citas]
}
//[FIN][31/8/2026][Rodriale][DTOs del módulo de citas]