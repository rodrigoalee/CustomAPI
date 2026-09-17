//[INICIO][16/9/2026][jgarciad8][Requisito de autorización basado en un permiso]
using Microsoft.AspNetCore.Authorization;

namespace CustomCore.API.Seguridad;

public sealed class PermisoRequirement(string codigo)
    : IAuthorizationRequirement
{
    public string Codigo { get; } = codigo;
}
//[FIN][16/9/2026][jgarciad8][Requisito de autorización basado en un permiso]