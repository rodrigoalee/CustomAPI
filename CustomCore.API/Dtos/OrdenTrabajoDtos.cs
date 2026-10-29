//[INICIO][31/8/2026][Rodriale][DTOs de órdenes de trabajo: lo que entra y sale por la API, nunca las entidades directas]
using System.ComponentModel.DataAnnotations;
using CustomCore.API.Entidades;

namespace CustomCore.API.Dtos
{
    //[INICIO][31/8/2026][Rodriale][Filtros del tablero de órdenes: por vehículo, por estado y por rango de fechas]
    public record ConsultaOrdenes : ConsultaPaginada
    {
        [Range(1, int.MaxValue)]
        public int? VehiculoId { get; init; }

        public EstadoOrdenTrabajo? Estado { get; init; }
        public DateTimeOffset? Desde { get; init; }
        public DateTimeOffset? Hasta { get; init; }

        //[INICIO][12/10/2026][Rodriale][Filtro por sede; solo lo respeta quien ve todas, al empleado se le fija la suya]
        [Range(1, int.MaxValue)]
        public int? SucursalId { get; init; }
        //[FIN][12/10/2026][Rodriale][Filtro por sede]

        //[INICIO][25/10/2026][Rodriale][Solo las órdenes asignadas a quien consulta; al mecánico restringido se le aplica siempre]
        public bool AsignadasAMi { get; init; }
        //[FIN][25/10/2026][Rodriale][Solo las órdenes asignadas a quien consulta]
    }
    //[FIN][31/8/2026][Rodriale][Filtros del tablero de órdenes]

    //[INICIO][31/8/2026][Rodriale][Fila del listado: lo justo para pintar la tabla sin traer las líneas de cada orden]
    public record OrdenListaDto(
        int IdOrdenEncabezado,
        int SucursalId,
        int VehiculoId,
        string Placa,
        string NombreCliente,
        DateTimeOffset FechaIngreso,
        DateTimeOffset? FechaFinalizacion,
        EstadoOrdenTrabajo Estado,
        decimal TotalEstimado,
        //[INICIO][25/10/2026][Rodriale][Mecánico y servicio para el tablero de recepción y "mis órdenes"]
        string? MecanicoAsignado,
        string? Servicio);
        //[FIN][25/10/2026][Rodriale][Mecánico y servicio para el tablero]
    //[FIN][31/8/2026][Rodriale][Fila del listado]

    //[INICIO][26/10/2026][Rodriale][Contadores del filtro por estado]
    public record ConteoOrdenesPorEstadoDto(
        int EnRecepcion,
        int EnProceso,
        int Finalizado);
    //[FIN][26/10/2026][Rodriale][Contadores del filtro por estado]

    //[INICIO][31/8/2026][Rodriale][Vista completa de la orden: encabezado, responsables y el desglose de trabajo]
    public record OrdenDetalleDto(
        int IdOrdenEncabezado,
        int SucursalId,
        int VehiculoId,
        string Placa,
        string Marca,
        string Modelo,
        int ClienteId,
        string NombreCliente,
        DateTimeOffset FechaIngreso,
        DateTimeOffset? FechaFinalizacion,
        string? Diagnostico,
        EstadoOrdenTrabajo Estado,
        decimal TotalEstimado,
        string UsuarioRecepcion,
        string? MecanicoAsignado,
        IReadOnlyList<LineaOrdenDto> Lineas,
        //[INICIO][25/10/2026][Rodriale][Cita de origen, motivo de recepción e id del mecánico para reasignar]
        int? CitaId,
        string? MotivoIngreso,
        int? MecanicoAsignadoId,
        //[FIN][25/10/2026][Rodriale][Cita de origen, motivo de recepción e id del mecánico]
        //[INICIO][29/10/2026][Rodriale][Fotos del diagnóstico]
        IReadOnlyList<FotoOrdenDto> Fotos);
        //[FIN][29/10/2026][Rodriale][Fotos del diagnóstico]
    //[FIN][31/8/2026][Rodriale][Vista completa de la orden]

    //[INICIO][29/10/2026][Rodriale][Foto del diagnóstico; el front la sube a Vercel Blob y aquí registra la URL]
    public record FotoOrdenDto(
        int IdFoto,
        string Url,
        string? Descripcion,
        DateTimeOffset FechaSubida);

    public record RegistrarFotoOrdenRequest(
        [Required, MaxLength(500)] string Url,
        [MaxLength(200)] string? Descripcion);
    //[FIN][29/10/2026][Rodriale][Foto del diagnóstico]

    //[INICIO][31/8/2026][Rodriale][Cada línea del trabajo; TipoLinea le ahorra al front tener que adivinar si es repuesto o mano de obra]
    public record LineaOrdenDto(
        int IdOrdenDetalle,
        string TipoLinea,
        int? RepuestoId,
        int? ServicioId,
        string Descripcion,
        int Cantidad,
        decimal PrecioUnitario,
        decimal SubtotalEstimado);
    //[FIN][31/8/2026][Rodriale][Cada línea del trabajo]

    //[INICIO][31/8/2026][Rodriale][Una línea es repuesto O servicio, nunca los dos ni ninguno; se rechaza aquí para que ni llegue a la base]
    public record CrearLineaOrdenRequest(
        int? RepuestoId,
        int? ServicioId,
        [Range(1, int.MaxValue)] int Cantidad) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            //[INICIO][31/8/2026][Rodriale][Si ambos vienen llenos o ambos vacíos, el HasValue es igual y la línea no sirve]
            if (RepuestoId.HasValue == ServicioId.HasValue)
                yield return new ValidationResult(
                    "Cada línea debe indicar un repuesto o un servicio, pero no ambos.",
                    [nameof(RepuestoId), nameof(ServicioId)]);
            //[FIN][31/8/2026][Rodriale][Si ambos vienen llenos o ambos vacíos, la línea no sirve]
        }
    }
    //[FIN][31/8/2026][Rodriale][Una línea es repuesto O servicio]

    //[INICIO][31/8/2026][Rodriale][Alta de la orden; ojo que aquí NO se manda el precio: eso lo pone el catálogo, no el cliente]
    //[INICIO][25/10/2026][Rodriale][Toda orden nace de una cita: vehículo y sede salen de ella. UsuarioRecepcionId lo pone el controlador desde el JWT]
    public record CrearOrdenRequest(
        [Range(1, int.MaxValue)] int CitaId,
        int? MecanicoAsignadoId,
        [MaxLength(1000)] string? MotivoIngreso,
        IReadOnlyList<CrearLineaOrdenRequest>? Lineas = null,
        int UsuarioRecepcionId = 0);
    //[FIN][25/10/2026][Rodriale][Toda orden nace de una cita]
    //[FIN][31/8/2026][Rodriale][Alta de la orden]

    //[INICIO][25/10/2026][Rodriale][El mecánico solo escribe su diagnóstico; no puede tocar la asignación]
    public record ActualizarDiagnosticoRequest(
        [MaxLength(2000)] string? Diagnostico);
    //[FIN][25/10/2026][Rodriale][El mecánico solo escribe su diagnóstico]

    //[INICIO][25/10/2026][Rodriale][Mecánico de la sede con su carga: disponible si no tiene órdenes activas]
    public record MecanicoDisponibleDto(
        int IdUsuario,
        string NombreCompleto,
        int OrdenesActivas);
    //[FIN][25/10/2026][Rodriale][Mecánico de la sede con su carga]

    //[INICIO][31/8/2026][Rodriale][Lo único editable del encabezado: el diagnóstico del mecánico y a quién se le asigna el trabajo]
    public record ActualizarOrdenRequest(
        int? MecanicoAsignadoId,
        [MaxLength(2000)] string? Diagnostico);
    //[FIN][31/8/2026][Rodriale][Lo único editable del encabezado]

    //[INICIO][31/8/2026][Rodriale][Mover la orden entre "En Recepción", "En Proceso" y "Finalizado"]
    //[INICIO][17/9/2026][jgarciad8][Validación de estados permitidos para órdenes]
    public record CambiarEstadoOrdenRequest(
        [EnumDataType(typeof(EstadoOrdenTrabajo),
        ErrorMessage = "El estado de la orden no es válido.")]
    EstadoOrdenTrabajo Estado);
    //[FIN][17/9/2026][jgarciad8][Validación de estados permitidos para órdenes]

    //[FIN][31/8/2026][Rodriale][Mover la orden entre estados]
}
//[FIN][31/8/2026][Rodriale][DTOs de órdenes de trabajo]
