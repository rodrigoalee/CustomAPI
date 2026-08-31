using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    //[INICIO][30/8/2026][Rodriale][Filtro adicional por cliente sobre la paginación estándar]
    public record class ConsultaVehiculos : ConsultaPaginada
    {
        [Range(1, int.MaxValue)]
        public int? ClienteId { get; init; }
    }
    //[FIN][30/8/2026][Rodriale][Filtro adicional por cliente sobre la paginación estándar]

    //[INICIO][30/8/2026][Consulta ligera que trae la lista completa de vehículos]
    public record VehiculoListaDto(
        int IdVehiculo,
        string Placa,
        string Marca,
        string Modelo,
        int Anio,
        int ClienteId,
        string NombreCliente
    );
    //[FIN][30/8/2026][Consulta ligera que trae la lista completa de vehículos]

    //[INICIO][30/8/2026][Consulta detallada que trae la información completa de un vehículo]
    public record VehiculoDetalleDto(
        int IdVehiculo,
        string Placa,
        string Marca,
        string Modelo,
        int Anio,
        int IdCliente,
        string NombreCliente,
        string TelefonoCliente,
        int CantidadCitas,
        int CantidadOrdenes
    );
    //[FIN][30/8/2026][Consulta detallada que trae la información completa de un vehículo]

    //[INICIO][30/8/2026][Rodriale][DTOs para crear y actualizar vehículos]
    public record CrearVehiculosRequest(
        [Range(1, int.MaxValue)] int ClienteId,
    [Required, MaxLength(20)] string Placa,
    [Required, MaxLength(50)] string Marca,
    [Required, MaxLength(50)] string Modelo,
    [Range(1900, 2100)] int Anio
    );

    public record ActualizarVehiculoRequest(
    [Range(1, int.MaxValue)] int ClienteId,
    [Required, MaxLength(20)] string Placa,
    [Required, MaxLength(50)] string Marca,
    [Required, MaxLength(50)] string Modelo,
    [Range(1900, 2100)] int Anio
    );
    //[FIN][30/8/2026][Rodriale][DTOs para crear y actualizar vehículos]
}
