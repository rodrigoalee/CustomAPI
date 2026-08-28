//[INICIO][28/8/2026][Rodriale][DTOs del módulo de clientes]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos;

//[INICIO][28/8/2026][Rodriale][datos esenciales para hacer liviana la carga y tener la tabla de clientes rápido]
public record ClienteResumenDto(
    int IdCliente,
    string NombreCompleto,
    string Telefono,
    string Correo,
    int CantidadVehiculos);
//[FIN][28/8/2026][Rodriale][datos esenciales para hacer liviana la carga y tener la tabla de clientes rápido]

//[INICIO][28/8/2026][Rodriale][datos completos del cliente y sus vehículos para la vista de detalle]
public record ClienteDetalleDto(
    int IdCliente,
    string NombreCompleto,
    string Telefono,
    string Correo,
    string? Nit,
    IReadOnlyList<VehiculoResumenDto> Vehiculos);
//[FIN][28/8/2026][Rodriale][datos completos del cliente y sus vehículos para la vista de detalle]

//[INICIO][28/8/2026][Rodriale][datos esenciales de los vehículos para la vista de detalle del cliente]
public record VehiculoResumenDto(
    int IdVehiculo,
    string Placa,
    string Marca,
    string Modelo,
    int Anio);
//[FIN][28/8/2026][Rodriale][datos esenciales de los vehículos para la vista de detalle del cliente]

//[INICIO][28/8/2026][Rodriale][DTOs para crear y actualizar clientes]
public record CrearClienteRequest(
    [property: Required, MaxLength(150)] string NombreCompleto,
    [property: Required, MaxLength(20)] string Telefono,
    [property: Required, EmailAddress, MaxLength(100)] string Correo,
    [property: MaxLength(20)] string? Nit);

public record ActualizarClienteRequest(
    [property: Required, MaxLength(150)] string NombreCompleto,
    [property: Required, MaxLength(20)] string Telefono,
    [property: Required, EmailAddress, MaxLength(100)] string Correo,
    [property: MaxLength(20)] string? Nit);
//[FIN][28/8/2026][Rodriale][DTOs para crear y actualizar clientes]

//[FIN][28/8/2026][Rodriale][DTOs del módulo de clientes]