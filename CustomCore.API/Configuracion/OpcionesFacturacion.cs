//[INICIO][31/8/2026][Rodriale][La tasa de impuesto no se quema en el código: cambia por ley y no queremos recompilar por eso]
namespace CustomCore.API.Configuracion
{
    public sealed class OpcionesFacturacion
    {
        public const string Seccion = "Facturacion";

        //[INICIO][31/8/2026][Rodriale][0.12 = 12% de IVA]
        public decimal TasaImpuesto { get; set; } = 0.12m;
        //[FIN][31/8/2026][Rodriale][0.12 = 12% de IVA]

        //[INICIO][31/8/2026][Rodriale][true = el precio del catálogo YA trae el IVA y solo se desglosa (Guatemala). false = el IVA se suma al final]
        public bool PreciosIncluyenImpuesto { get; set; } = true;
        //[FIN][31/8/2026][Rodriale][true = precio con IVA incluido]
    }
}
//[FIN][31/8/2026][Rodriale][La tasa de impuesto no se quema en el código]
