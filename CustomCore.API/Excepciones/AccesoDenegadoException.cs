//[INICIO][12/10/2026][Rodriale][El usuario está identificado, pero no puede operar sobre esa sucursal]
namespace CustomCore.API.Excepciones
{
    public sealed class AccesoDenegadoException(string mensaje) : Exception(mensaje);
}
//[FIN][12/10/2026][Rodriale][El usuario está identificado, pero no puede operar sobre esa sucursal]
