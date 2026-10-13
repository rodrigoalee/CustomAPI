# Sucursales S2–S4 — Plan de implementación

> **Para agentes:** SUB-SKILL OBLIGATORIA: usar superpowers:subagent-driven-development (recomendada) o superpowers:executing-plans para ejecutar este plan tarea por tarea. Los pasos usan casillas (`- [ ]`) para el seguimiento.

**Objetivo:** que el stock se lleve por sede, que órdenes, citas, facturas y usuarios queden ligados a una sede, y que cada empleado opere y vea solo la suya, mientras quien tenga `sucursales.todas` ve todas.

**Arquitectura:** una tabla nueva `InventariosSucursal` reemplaza a `Repuestos.CantidadStock`, y `SucursalId` se agrega a `Usuarios`, `OrdenesTrabajoEncabezado` y `Citas`; la factura deriva su sede de la orden. Un servicio por petición, `SucursalActual`, resuelve una sola vez la sede y el alcance de quien llama. Los servicios lo consultan con `AlcanceAsync()` para leer y `ResolverAsync()` para crear. Todas las escrituras siguen pasando por la auditoría existente (`ActualizarAuditadoAsync` / interceptor).

**Stack:** .NET 9, ASP.NET Core Web API, EF Core con Npgsql (PostgreSQL 16 local), JWT, permisos en base de datos.

**Spec:** `docs/superpowers/specs/2026-10-12-sucursales-s2-s4-design.md`

## Restricciones globales

- Rama `Local/FeatureSucursales`. Los commits van ahí; no se hace `push`.
- Toda migración se aplica solo a la base de `ConnectionStrings:Default`, que debe apuntar a `localhost` / `pruebas_customdb`. Antes de cualquier `dotnet ef database update`, verificar el host sin imprimir la cadena. **Supabase no se toca.**
- Directorio de trabajo para `dotnet`: `C:\Users\roood\source\repos\CustomCore.API\CustomCore.API`. Raíz de git: un nivel arriba.
- Comentarios con el formato del proyecto: `//[INICIO][12/10/2026][Rodriale][motivo]` … `//[FIN][12/10/2026][Rodriale][motivo]`. No se tocan los comentarios de `jgarciad8` ni los existentes, salvo para mover código que cambia.
- Sin sobreingeniería: ni interfaces, ni repositorios, ni capas nuevas. Consultas proyectadas y en una sola ida a la base cuando se pueda.
- Un registro de otra sede responde `404`. La ruta explícita de inventario de otra sede responde `403`. Sin sede y sin `sucursales.todas`, `403`. Quien tiene `todas` y no indica sede al crear recibe `400`. Una sede inexistente o dada de baja al crear, `409`.
- El proyecto no tiene pruebas automatizadas. La verificación de cada tarea es `dotnet build` (0 errores y sin advertencias nuevas) más las comprobaciones que indique la tarea. La Tarea 8 recorre todos los escenarios contra la API local.
- Commits con el formato `Rodriale: <mensaje>.`, terminando con la línea `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Foco de revisión

1. **Repuesto creado después de la migración y sin fila de inventario en la sede:** pedirlo en una orden debe responder `409` por stock insuficiente, no `500` ni un descuento silencioso. Lo cubre la Tarea 2 (`DescontarStockAsync`) y lo ejercita la Tarea 8 (escenario "repuesto sin fila").
2. **Usuario sin sede y sin `sucursales.todas`:** cualquier listado o alta debe responder `403`, no devolver datos de todas las sedes. Lo cubre la Tarea 2 (`SucursalActual`) y lo ejercita la Tarea 8 (usuario "Sin sede").
3. **Empleado que manda `?sucursalId=` de otra sede en un listado:** se ignora, y solo ve la suya. Lo cubren las Tareas 3, 5 y 6 (`AlcanceAsync() ?? consulta.SucursalId`) y lo ejercita la Tarea 8.
4. **Quitar una línea de una orden devuelve el stock a la sede de la orden**, no a la del usuario que la quita (por ejemplo, el Gerente). Lo cubre la Tarea 2 (`DevolverStockAsync` con `orden.SucursalId`) y lo ejercita la Tarea 8.
5. **`PUT /api/usuarios/{id}` sin `sucursalId`:** deja al usuario sin sede, porque `PUT` reemplaza el recurso completo. Es el comportamiento esperado; queda documentado en la Tarea 7 y se avisa al frontend en la entrega.

---

## Mapa de archivos

| Archivo | Responsabilidad | Tarea |
|---|---|---|
| `Excepciones/AccesoDenegadoException.cs` (nuevo) | Error que se traduce en `403` | 1 |
| `Excepciones/SolicitudInvalidaException.cs` (nuevo) | Error que se traduce en `400` | 1 |
| `Middleware/ManejadorGlobalExcepciones.cs` | Traduce las dos excepciones nuevas | 1 |
| `Seguridad/PermisosSistema.cs` | 5 constantes nuevas | 1 |
| `Seguridad/ConfiguracionSeguridad.cs` | Políticas de sucursales e inventario | 1 |
| `Entidades/InventarioSucursal.cs` + `InventarioSucursalConfiguracion.cs` (nuevos) | Stock por sede | 2 |
| `Entidades/Usuario.cs`, `OrdenTrabajoEncabezado.cs`, `Cita.cs` y sus configuraciones | Columna `SucursalId` y FK | 2 |
| `Entidades/Repuesto.cs`, `Dtos/RepuestoDto.cs`, `Servicios/RepuestoService.cs` | Quitar `CantidadStock` | 2 |
| `Data/AppDbContext.cs` | `DbSet` de inventario | 2 |
| `Data/Auditoria/CambioAuditoria.cs` | Campos auditables nuevos | 2 |
| `Data/Migrations/*_InventarioPorSucursal.cs` (nuevo) | Esquema y traslado de datos | 2 |
| `Seguridad/SucursalActual.cs` (nuevo) | Sede y alcance de quien llama | 2 |
| `Servicios/OrdenTrabajoService.cs`, `Dtos/OrdenTrabajoDtos.cs` | Stock por sede (T2); alcance y mecánico (T3) | 2, 3 |
| `Servicios/InventarioService.cs`, `Controllers/InventarioController.cs`, `Dtos/InventarioDtos.cs` (nuevos) | Consultar y fijar stock por sede | 4 |
| `Servicios/CitaService.cs`, `Dtos/CitaDtos.cs` | Sede y alcance en citas | 5 |
| `Servicios/FacturaService.cs`, `Dtos/FacturaDtos.cs`, `Servicios/PagoStripeService.cs` | Alcance por la orden | 6 |
| `Servicios/UsuarioService.cs`, `Dtos/UsuarioDtos.cs` | Sede del usuario | 7 |
| `Controllers/SucursalesController.cs`, `Servicios/SucursalService.cs` | Autorización y auditoría | 7 |
| `Program.cs` | Registrar `SucursalActual` (T2) e `InventarioService` (T4) | 2, 4 |

---

### Tarea 1: Permisos y excepciones

**Archivos:**
- Crear: `Excepciones/AccesoDenegadoException.cs`, `Excepciones/SolicitudInvalidaException.cs`
- Modificar: `Middleware/ManejadorGlobalExcepciones.cs`, `Seguridad/PermisosSistema.cs`, `Seguridad/ConfiguracionSeguridad.cs`

**Interfaces:**
- Produce: `AccesoDenegadoException(string mensaje)`, `SolicitudInvalidaException(string mensaje)`, `PermisosSistema.SucursalesLeer|SucursalesGestionar|SucursalesTodas|InventarioLeer|InventarioAjustar` (todas `const string`), y las políticas registradas con esos mismos nombres (excepto `SucursalesTodas`).

- [ ] **Paso 1: Crear las excepciones**

`Excepciones/AccesoDenegadoException.cs`:

```csharp
//[INICIO][12/10/2026][Rodriale][El usuario está identificado, pero no puede operar sobre esa sucursal]
namespace CustomCore.API.Excepciones
{
    public sealed class AccesoDenegadoException(string mensaje) : Exception(mensaje);
}
//[FIN][12/10/2026][Rodriale][El usuario está identificado, pero no puede operar sobre esa sucursal]
```

`Excepciones/SolicitudInvalidaException.cs`:

```csharp
//[INICIO][12/10/2026][Rodriale][A la petición le falta un dato que solo el cliente puede dar]
namespace CustomCore.API.Excepciones
{
    public sealed class SolicitudInvalidaException(string mensaje) : Exception(mensaje);
}
//[FIN][12/10/2026][Rodriale][A la petición le falta un dato que solo el cliente puede dar]
```

- [ ] **Paso 2: Traducirlas en el manejador global**

En `Middleware/ManejadorGlobalExcepciones.cs`, dentro de `Traducir`, justo después del bloque de `ConflictoNegocioException` (antes de `DbUpdateConcurrencyException`), agregar:

```csharp
                //[INICIO][12/10/2026][Rodriale][Sucursal ajena o usuario sin sucursal: el cliente no puede corregirlo reintentando]
                AccesoDenegadoException denegado
                    => (StatusCodes.Status403Forbidden, "Acceso denegado", denegado.Message),

                SolicitudInvalidaException invalida
                    => (StatusCodes.Status400BadRequest, "Solicitud inválida", invalida.Message),
                //[FIN][12/10/2026][Rodriale][Sucursal ajena o usuario sin sucursal]
```

- [ ] **Paso 3: Agregar las constantes de permisos**

En `Seguridad/PermisosSistema.cs`, antes de la llave de cierre de la clase:

```csharp
    //[INICIO][12/10/2026][Rodriale][Permisos de sucursales e inventario por sede]
    public const string SucursalesLeer = "sucursales.leer";
    public const string SucursalesGestionar = "sucursales.gestionar";

    //[INICIO][12/10/2026][Rodriale][No protege endpoints: SucursalActual lo consulta para saber si quien llama ve todas las sedes]
    public const string SucursalesTodas = "sucursales.todas";
    //[FIN][12/10/2026][Rodriale][No protege endpoints]

    public const string InventarioLeer = "inventario.leer";
    public const string InventarioAjustar = "inventario.ajustar";
    //[FIN][12/10/2026][Rodriale][Permisos de sucursales e inventario por sede]
```

- [ ] **Paso 4: Registrar las políticas**

En `Seguridad/ConfiguracionSeguridad.cs`, dentro de `AddAuthorization`, después del bloque `//[FIN][17/9/2026][jgarciad8][Políticas de facturación y pagos]`:

```csharp
            //[INICIO][12/10/2026][Rodriale][Políticas de sucursales e inventario; sucursales.todas no es política, se consulta en SucursalActual]
            foreach (var codigo in new[]
            {
                PermisosSistema.SucursalesLeer,
                PermisosSistema.SucursalesGestionar,
                PermisosSistema.InventarioLeer,
                PermisosSistema.InventarioAjustar
            })
            {
                opciones.AddPolicy(codigo, politica =>
                {
                    politica.RequireAuthenticatedUser();
                    politica.AddRequirements(new PermisoRequirement(codigo));
                });
            }
            //[FIN][12/10/2026][Rodriale][Políticas de sucursales e inventario]
```

- [ ] **Paso 5: Compilar**

Ejecutar: `dotnet build --nologo -v q`
Esperado: `0 Errores`.

- [ ] **Paso 6: Commit**

```bash
git add CustomCore.API/Excepciones CustomCore.API/Middleware/ManejadorGlobalExcepciones.cs CustomCore.API/Seguridad/PermisosSistema.cs CustomCore.API/Seguridad/ConfiguracionSeguridad.cs
git commit -m "Rodriale: permisos de sucursales e inventario y excepciones 403/400." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 2: Modelo, migración, `SucursalActual` y stock por sede

Es la tarea grande y no se puede partir. Quitar `CantidadStock` rompe la compilación de `RepuestoService`, `CambioAuditoria` y `OrdenTrabajoService`, y la migración solo se genera cuando el proyecto compila. **Después de esta tarea, crear citas falla hasta la Tarea 5**, porque `Citas.SucursalId` ya es obligatoria. Es esperado.

**Archivos:**
- Crear: `Entidades/InventarioSucursal.cs`, `Entidades/InventarioSucursalConfiguracion.cs`, `Seguridad/SucursalActual.cs`, `Data/Migrations/<marca>_InventarioPorSucursal.cs` (generada y luego editada)
- Modificar: `Entidades/Usuario.cs`, `Entidades/UsuarioConfiguracion.cs`, `Entidades/OrdenTrabajoEncabezado.cs`, `Entidades/OrdenTrabajoEncabezadoConfiguracion.cs`, `Entidades/Cita.cs`, `Entidades/CitaConfiguracion.cs`, `Entidades/Repuesto.cs`, `Data/AppDbContext.cs`, `Data/Auditoria/CambioAuditoria.cs`, `Dtos/RepuestoDto.cs`, `Servicios/RepuestoService.cs`, `Dtos/OrdenTrabajoDtos.cs`, `Servicios/OrdenTrabajoService.cs`, `Program.cs`

**Interfaces:**
- Consume (Tarea 1): `AccesoDenegadoException`, `SolicitudInvalidaException`, `PermisosSistema.SucursalesTodas`.
- Produce:
  - `InventarioSucursal { int IdInventario; int SucursalId; int RepuestoId; int Cantidad; }`, y `AppDbContext.InventariosSucursal`.
  - `Usuario.SucursalId : int?`, `OrdenTrabajoEncabezado.SucursalId : int`, `Cita.SucursalId : int`.
  - `SucursalActual.AlcanceAsync(CancellationToken) : Task<int?>` y `SucursalActual.ResolverAsync(int? solicitada, CancellationToken) : Task<int>`.
  - `CrearOrdenRequest` con el último parámetro `int? SucursalId = null`.
  - En `OrdenTrabajoService`: `private sealed record OrdenEditable(EstadoOrdenTrabajo Estado, int SucursalId)`; `ExisteYNoEstaFacturadaAsync` y `EstaAbiertaParaEditarLineasAsync` devuelven `Task<OrdenEditable?>`.

- [ ] **Paso 1: Verificar que `Default` apunta a local**

```powershell
dotnet user-secrets list | ForEach-Object { if ($_ -match '^ConnectionStrings:Default = (.*)$') { ([regex]::Match($matches[1],'(?i)host=([^;]+)')).Groups[1].Value } }
```

Esperado: `localhost`. Si sale otra cosa, **detenerse** y avisar a Rodrigo.

- [ ] **Paso 2: Entidad y configuración de inventario**

`Entidades/InventarioSucursal.cs`:

```csharp
//[INICIO][12/10/2026][Rodriale][Existencias de un repuesto en una sede; el precio sigue siendo único en Repuestos]
namespace CustomCore.API.Entidades;

public class InventarioSucursal
{
    //[INICIO][12/10/2026][Rodriale][Llave simple: la auditoría bloquea y registra por una sola llave int]
    public int IdInventario { get; set; }
    //[FIN][12/10/2026][Rodriale][Llave simple]
    public int SucursalId { get; set; }
    public int RepuestoId { get; set; }
    public int Cantidad { get; set; }
}
//[FIN][12/10/2026][Rodriale][Existencias de un repuesto en una sede]
```

`Entidades/InventarioSucursalConfiguracion.cs`:

```csharp
//[INICIO][12/10/2026][Rodriale][Mapeo del inventario por sede]
using CustomCore.API.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomCore.API.Data.Configurations;

public sealed class InventarioSucursalConfiguration : IEntityTypeConfiguration<InventarioSucursal>
{
    public void Configure(EntityTypeBuilder<InventarioSucursal> builder)
    {
        //[INICIO][12/10/2026][Rodriale][La base misma impide stock negativo, aunque algún día se escriba sin pasar por el servicio]
        builder.ToTable("InventariosSucursal", t =>
            t.HasCheckConstraint("CK_InventariosSucursal_CantidadNoNegativa", @"""Cantidad"" >= 0"));
        //[FIN][12/10/2026][Rodriale][La base misma impide stock negativo]

        builder.HasKey(i => i.IdInventario);

        //[INICIO][12/10/2026][Rodriale][Una sola fila por sede y repuesto]
        builder.HasIndex(i => new { i.SucursalId, i.RepuestoId }).IsUnique();
        //[FIN][12/10/2026][Rodriale][Una sola fila por sede y repuesto]

        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(i => i.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Repuesto>()
            .WithMany()
            .HasForeignKey(i => i.RepuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//[FIN][12/10/2026][Rodriale][Mapeo del inventario por sede]
```

En `Data/AppDbContext.cs`, después de `public DbSet<Sucursal> Sucursales => Set<Sucursal>();`:

```csharp
        public DbSet<InventarioSucursal> InventariosSucursal => Set<InventarioSucursal>();
```

- [ ] **Paso 3: `SucursalId` en usuario, orden y cita; quitar `CantidadStock`**

`Entidades/Usuario.cs`, después de `public bool Activo { get; set; } = true;`:

```csharp
    //[INICIO][12/10/2026][Rodriale][Sede del empleado; vacía solo para quien tenga sucursales.todas]
    public int? SucursalId { get; set; }
    //[FIN][12/10/2026][Rodriale][Sede del empleado]
```

`Entidades/OrdenTrabajoEncabezado.cs`, después de `public int VehiculoId { get; set; }`:

```csharp
    //[INICIO][12/10/2026][Rodriale][Sede donde se atiende la orden; de aquí sale el stock que consume]
    public int SucursalId { get; set; }
    //[FIN][12/10/2026][Rodriale][Sede donde se atiende la orden]
```

`Entidades/Cita.cs`, después de `public int VehiculoId { get; set; }`:

```csharp
    //[INICIO][12/10/2026][Rodriale][Sede donde se agenda la cita]
    public int SucursalId { get; set; }
    //[FIN][12/10/2026][Rodriale][Sede donde se agenda la cita]
```

`Entidades/Repuesto.cs`: eliminar la línea `public int CantidadStock { get; set; }`.

En las tres configuraciones, al final de `Configure`, agregar la llave foránea sin navegación (EF crea el índice de la FK):

`Entidades/UsuarioConfiguracion.cs`:

```csharp
        //[INICIO][12/10/2026][Rodriale][Una sede cerrada no se borra; Restrict protege a sus empleados]
        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(u => u.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);
        //[FIN][12/10/2026][Rodriale][Una sede cerrada no se borra]
```

`Entidades/OrdenTrabajoEncabezadoConfiguracion.cs`:

```csharp
        //[INICIO][12/10/2026][Rodriale][Sede de la orden]
        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(o => o.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);
        //[FIN][12/10/2026][Rodriale][Sede de la orden]
```

`Entidades/CitaConfiguracion.cs`:

```csharp
        //[INICIO][12/10/2026][Rodriale][Sede de la cita]
        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(c => c.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);
        //[FIN][12/10/2026][Rodriale][Sede de la cita]
```

- [ ] **Paso 4: Campos auditables**

En `Data/Auditoria/CambioAuditoria.cs`, dentro del diccionario `Campos`:

1. En `[typeof(Usuario)]`, agregar `nameof(Usuario.SucursalId)` después de `nameof(Usuario.Activo)`.
2. En `[typeof(Repuesto)]`, quitar `nameof(Repuesto.CantidadStock), `.
3. En `[typeof(Cita)]`, agregar `nameof(Cita.SucursalId), ` después de `nameof(Cita.VehiculoId), `.
4. En `[typeof(OrdenTrabajoEncabezado)]`, agregar `nameof(OrdenTrabajoEncabezado.SucursalId), ` después de `nameof(OrdenTrabajoEncabezado.VehiculoId), `.
5. Antes de `//[FIN][5/10/2026][jgarciad8][Campos permitidos del taller y facturación; sin datos de tarjeta ni secretos Stripe]`, agregar (con una coma después del bloque de `FacturaDetalle`):

```csharp
        //[INICIO][12/10/2026][Rodriale][Sedes y existencias por sede también quedan en la bitácora]
        [typeof(Sucursal)] = [
            nameof(Sucursal.IdSucursal), nameof(Sucursal.Nombre), nameof(Sucursal.Direccion),
            nameof(Sucursal.Telefono), nameof(Sucursal.Activo)],
        [typeof(InventarioSucursal)] = [
            nameof(InventarioSucursal.IdInventario), nameof(InventarioSucursal.SucursalId),
            nameof(InventarioSucursal.RepuestoId), nameof(InventarioSucursal.Cantidad)]
        //[FIN][12/10/2026][Rodriale][Sedes y existencias por sede también quedan en la bitácora]
```

- [ ] **Paso 5: Repuestos sin stock**

`Dtos/RepuestoDto.cs`: quitar el parámetro `int CantidadStock,` de `RepuestoDto` y la línea `[Range(0, int.MaxValue)] int CantidadStock` de `CrearRepuestoRequest` (el parámetro anterior, `PrecioUnitario`, queda sin coma final). En `ActualizarRepuestoRequest`, quitar `[Range(0, int.MaxValue)] int CantidadStock,`.

`Servicios/RepuestoService.cs`:
- En las dos proyecciones `new RepuestoDto(...)`, quitar la línea `r.CantidadStock,`.
- En `CrearAsync`, quitar `CantidadStock = request.CantidadStock` (y la coma que queda sobrando en `PrecioUnitario = request.PrecioUnitario,`).
- En `ActualizarAsync`, quitar `.SetProperty(r => r.CantidadStock, request.CantidadStock)`.

- [ ] **Paso 6: Crear `SucursalActual`**

`Seguridad/SucursalActual.cs`:

```csharp
//[INICIO][12/10/2026][Rodriale][Sede y alcance de quien llama, resueltos una sola vez por petición]
using System.IdentityModel.Tokens.Jwt;
using CustomCore.API.Data;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Seguridad;

public sealed class SucursalActual(AppDbContext db, IHttpContextAccessor http)
{
    private sealed record Perfil(int? SucursalId, bool Todas);

    private Perfil? _perfil;

    //[INICIO][12/10/2026][Rodriale][Para lecturas: null significa sin filtro; sin sede y sin permiso falla cerrado]
    public async Task<int?> AlcanceAsync(CancellationToken cancellationToken)
    {
        var perfil = await ObtenerPerfilAsync(cancellationToken);

        if (perfil.Todas)
            return null;

        return perfil.SucursalId
            ?? throw new AccesoDenegadoException("Tu usuario no tiene una sucursal asignada.");
    }
    //[FIN][12/10/2026][Rodriale][Para lecturas]

    //[INICIO][12/10/2026][Rodriale][Para altas: el empleado opera en su sede; quien ve todas debe decir cuál]
    public async Task<int> ResolverAsync(int? solicitada, CancellationToken cancellationToken)
    {
        var perfil = await ObtenerPerfilAsync(cancellationToken);
        int sucursalId;

        if (perfil.Todas)
        {
            sucursalId = solicitada
                ?? throw new SolicitudInvalidaException("Indica la sucursal (sucursalId).");
        }
        else
        {
            sucursalId = perfil.SucursalId
                ?? throw new AccesoDenegadoException("Tu usuario no tiene una sucursal asignada.");

            if (solicitada is { } pedida && pedida != sucursalId)
                throw new AccesoDenegadoException("Solo puedes operar en tu sucursal.");
        }

        var activa = await db.Sucursales
            .AsNoTracking()
            .AnyAsync(s => s.IdSucursal == sucursalId && s.Activo, cancellationToken);

        if (!activa)
            throw new ConflictoNegocioException("La sucursal no existe o está dada de baja.");

        return sucursalId;
    }
    //[FIN][12/10/2026][Rodriale][Para altas]

    //[INICIO][12/10/2026][Rodriale][Una sola consulta por petición; se lee en la base y no del token para que reasignar sede surta efecto de inmediato]
    private async Task<Perfil> ObtenerPerfilAsync(CancellationToken cancellationToken)
    {
        if (_perfil is not null)
            return _perfil;

        if (!int.TryParse(http.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var idUsuario))
            throw new AccesoDenegadoException("La sesión no identifica a un usuario.");

        _perfil = await db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario)
            .Select(u => new Perfil(
                u.SucursalId,
                u.UsuarioRoles.Any(ur => ur.Rol.RolPermisos.Any(
                    rp => rp.Permiso.NombreCodigo == PermisosSistema.SucursalesTodas))))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AccesoDenegadoException("La sesión no identifica a un usuario.");

        return _perfil;
    }
    //[FIN][12/10/2026][Rodriale][Una sola consulta por petición]
}
//[FIN][12/10/2026][Rodriale][Sede y alcance de quien llama]
```

En `Program.cs`, dentro del bloque `//[INICIO][Rodriale][Registro de inyección de dependencias]`, después de `builder.Services.AddScoped<SucursalService>();`:

```csharp
builder.Services.AddScoped<SucursalActual>();
```

(`Program.cs` ya tiene `using CustomCore.API.Seguridad;` y `AddHttpContextAccessor()`.)

- [ ] **Paso 7: La orden toma su sede al crearse**

En `Dtos/OrdenTrabajoDtos.cs`, reemplazar `CrearOrdenRequest` por:

```csharp
    //[INICIO][31/8/2026][Rodriale][Alta de la orden; ojo que aquí NO se manda el precio: eso lo pone el catálogo, no el cliente]
    public record CrearOrdenRequest(
        [Range(1, int.MaxValue)] int VehiculoId,
        [Range(1, int.MaxValue)] int UsuarioRecepcionId,
        int? MecanicoAsignadoId,
        [MaxLength(2000)] string? Diagnostico,
        IReadOnlyList<CrearLineaOrdenRequest> Lineas,
        //[INICIO][12/10/2026][Rodriale][Solo lo usa quien ve todas las sedes; al empleado se le asigna la suya]
        [Range(1, int.MaxValue)] int? SucursalId = null);
        //[FIN][12/10/2026][Rodriale][Solo lo usa quien ve todas las sedes]
    //[FIN][31/8/2026][Rodriale][Alta de la orden]
```

En `Servicios/OrdenTrabajoService.cs`:

1. Cambiar la firma de la clase a:

```csharp
    public sealed class OrdenTrabajoService(AppDbContext db, SucursalActual sucursal)
```

y agregar `using CustomCore.API.Seguridad;` a los `using`.

2. Debajo de la declaración de la clase (antes de `//[INICIO][31/8/2026][Rodriale][Listado del tablero...`), agregar:

```csharp
        //[INICIO][12/10/2026][Rodriale][Lo que hace falta saber de una orden antes de tocarla]
        private sealed record OrdenEditable(EstadoOrdenTrabajo Estado, int SucursalId);
        //[FIN][12/10/2026][Rodriale][Lo que hace falta saber de una orden antes de tocarla]
```

3. En `CrearInternoAsync`, reemplazar desde la línea `//[INICIO][31/8/2026][Rodriale][Primero se valida que todo lo pedido exista...` hasta `await DescontarStockAsync(request.Lineas, repuestos, cancellationToken);` por:

```csharp
            //[INICIO][12/10/2026][Rodriale][La sede se decide antes de mover stock: de ella sale lo que se descuenta]
            var sucursalId = await sucursal.ResolverAsync(request.SucursalId, cancellationToken);
            //[FIN][12/10/2026][Rodriale][La sede se decide antes de mover stock]

            //[INICIO][31/8/2026][Rodriale][Primero se valida que todo lo pedido exista y esté activo, antes de mover una sola pieza de bodega]
            var repuestos = await CargarRepuestosAsync(request.Lineas, cancellationToken);
            var servicios = await CargarServiciosAsync(request.Lineas, cancellationToken);
            //[FIN][31/8/2026][Rodriale][Primero se valida que todo exista y esté activo]

            await DescontarStockAsync(sucursalId, request.Lineas, repuestos, cancellationToken);
```

y en el inicializador `new OrdenTrabajoEncabezado { ... }`, agregar `SucursalId = sucursalId,` después de `VehiculoId = request.VehiculoId,`.

4. Reemplazar `ExisteYNoEstaFacturadaAsync` completo por:

```csharp
        //[INICIO][31/8/2026][Rodriale][Una orden ya facturada es intocable: cambiarla alteraría un documento fiscal ya emitido]
        private async Task<OrdenEditable?> ExisteYNoEstaFacturadaAsync(
            int ordenId,
            CancellationToken cancellationToken)
        {
            var info = await db.OrdenesTrabajoEncabezado
                .AsNoTracking()
                .Where(o => o.IdOrdenEncabezado == ordenId)
                .Select(o => new
                {
                    o.Estado,
                    o.SucursalId,
                    TieneFactura = db.FacturasEncabezado.Any(f => f.OrdenTrabajoId == o.IdOrdenEncabezado)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (info is null)
                return null;

            if (info.TieneFactura)
                throw new ConflictoNegocioException(
                    "La orden ya fue facturada y no admite cambios.");

            return new OrdenEditable(info.Estado, info.SucursalId);
        }
        //[FIN][31/8/2026][Rodriale][Una orden ya facturada es intocable]
```

5. Reemplazar `EstaAbiertaParaEditarLineasAsync` completo por:

```csharp
        //[INICIO][31/8/2026][Rodriale][Además de no estar facturada, para tocar líneas la orden no puede estar finalizada: primero se reabre]
        private async Task<OrdenEditable?> EstaAbiertaParaEditarLineasAsync(
            int ordenId,
            CancellationToken cancellationToken)
        {
            var orden = await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken);

            if (orden is null)
                return null;

            if (orden.Estado == EstadoOrdenTrabajo.Finalizado)
                throw new ConflictoNegocioException(
                    "La orden está finalizada; reábrela antes de modificar sus líneas.");

            return orden;
        }
        //[FIN][31/8/2026][Rodriale][Para tocar líneas la orden no puede estar finalizada]
```

6. En `AgregarLineaInternoAsync`, reemplazar:

```csharp
            if (await EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken) is null)
                return null;
```

por:

```csharp
            var orden = await EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken);
            if (orden is null)
                return null;
```

y `await DescontarStockAsync(lineas, repuestos, cancellationToken);` por:

```csharp
            //[INICIO][12/10/2026][Rodriale][El stock sale de la sede de la orden, no de la del usuario que agrega la línea]
            await DescontarStockAsync(orden.SucursalId, lineas, repuestos, cancellationToken);
            //[FIN][12/10/2026][Rodriale][El stock sale de la sede de la orden]
```

7. En `QuitarLineaInternoAsync`, reemplazar:

```csharp
            if (await EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken) is null)
                return false;
```

por:

```csharp
            var orden = await EstaAbiertaParaEditarLineasAsync(ordenId, cancellationToken);
            if (orden is null)
                return false;
```

y el bloque interno del `if (linea.RepuestoId is { } repuestoId) { ... }` (las líneas `var cantidad = linea.Cantidad;` y el `await db.Repuestos ... ActualizarAuditadoAsync(...)` con sus comentarios de jgarciad8) por:

```csharp
            {
                //[INICIO][12/10/2026][Rodriale][Regresa a la bodega de la sede de la orden]
                await DevolverStockAsync(orden.SucursalId, repuestoId, linea.Cantidad, cancellationToken);
                //[FIN][12/10/2026][Rodriale][Regresa a la bodega de la sede de la orden]
            }
```

8. Reemplazar `DescontarStockAsync` completo (desde su `//[INICIO][31/8/2026]` hasta su `//[FIN][31/8/2026][Rodriale][Descuento de stock con UPDATE condicionado]`) por:

```csharp
        //[INICIO][31/8/2026][Rodriale][El descuento va en un UPDATE con condición: si no alcanzan las existencias afecta 0 filas y ahí mismo se corta. Así dos órdenes al mismo tiempo no pueden vender la misma pieza]
        private async Task DescontarStockAsync(
            int sucursalId,
            IReadOnlyList<CrearLineaOrdenRequest> lineas,
            Dictionary<int, (string Nombre, decimal PrecioUnitario)> repuestos,
            CancellationToken cancellationToken)
        {
            //[INICIO][31/8/2026][Rodriale][Si el mismo repuesto viene en varias líneas, se suma primero para no hacer un UPDATE por línea]
            var consumoPorRepuesto = lineas
                .Where(l => l.RepuestoId.HasValue)
                .GroupBy(l => l.RepuestoId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Cantidad));
            //[FIN][31/8/2026][Rodriale][Si el mismo repuesto viene en varias líneas, se suma primero]

            if (consumoPorRepuesto.Count == 0)
                return;

            //[INICIO][12/10/2026][Rodriale][Una sola consulta trae las filas de inventario de la sede; si un repuesto no tiene fila, en esa sede hay 0]
            var ids = consumoPorRepuesto.Keys.ToList();
            var inventario = await db.InventariosSucursal
                .AsNoTracking()
                .Where(i => i.SucursalId == sucursalId && ids.Contains(i.RepuestoId))
                .ToDictionaryAsync(i => i.RepuestoId, i => i.IdInventario, cancellationToken);
            //[FIN][12/10/2026][Rodriale][Una sola consulta trae las filas de inventario de la sede]

            //[INICIO][5/10/2026][jgarciad8][Orden estable de bloqueos de inventario]
            foreach (var (repuestoId, cantidad) in consumoPorRepuesto.OrderBy(p => p.Key))
            //[FIN][5/10/2026][jgarciad8][Orden estable de bloqueos de inventario]
            {
                var filasAfectadas = 0;

                if (inventario.TryGetValue(repuestoId, out var idInventario))
                {
                    filasAfectadas = await db.InventariosSucursal
                        .Where(i => i.IdInventario == idInventario && i.Cantidad >= cantidad)
                        //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                        .ActualizarAuditadoAsync(db, idInventario, s => s
                            .SetProperty(i => i.Cantidad, i => i.Cantidad - cantidad),
                            cancellationToken);
                    //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                }

                if (filasAfectadas == 0)
                    throw new ConflictoNegocioException(
                        $"Stock insuficiente del repuesto '{repuestos[repuestoId].Nombre}' en esta sucursal: se solicitaron {cantidad} unidades.");
            }
        }
        //[FIN][31/8/2026][Rodriale][Descuento de stock con UPDATE condicionado]

        //[INICIO][12/10/2026][Rodriale][La fila existe: el descuento que se revierte la exigió]
        private async Task DevolverStockAsync(
            int sucursalId,
            int repuestoId,
            int cantidad,
            CancellationToken cancellationToken)
        {
            var idInventario = await db.InventariosSucursal
                .AsNoTracking()
                .Where(i => i.SucursalId == sucursalId && i.RepuestoId == repuestoId)
                .Select(i => i.IdInventario)
                .FirstAsync(cancellationToken);

            await db.InventariosSucursal
                .Where(i => i.IdInventario == idInventario)
                .ActualizarAuditadoAsync(db, idInventario, s => s
                    .SetProperty(i => i.Cantidad, i => i.Cantidad + cantidad),
                    cancellationToken);
        }
        //[FIN][12/10/2026][Rodriale][La fila existe]
```

- [ ] **Paso 8: Compilar**

Ejecutar: `dotnet build --nologo -v q`
Esperado: `0 Errores`. Si queda alguna referencia a `CantidadStock`, búscala con `grep -rn CantidadStock --include=*.cs . | grep -v Migrations` y quítala.

- [ ] **Paso 9: Generar la migración**

Ejecutar: `dotnet ef migrations add InventarioPorSucursal`
Esperado: se crean `Data/Migrations/<marca>_InventarioPorSucursal.cs` y su `.Designer.cs`, y se actualiza `AppDbContextModelSnapshot.cs`.

- [ ] **Paso 10: Reemplazar `Up` y `Down` por la versión que traslada datos**

El `Up` que genera EF agrega las columnas obligatorias con `defaultValue: 0`, lo que rompería las FK, y borra `CantidadStock` sin copiarla. En el `.cs` (no en el `.Designer.cs`), reemplazar los cuerpos de `Up` y `Down` por lo siguiente. Los `using` y la clase se dejan tal como los generó EF; si falta `using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;`, se agrega.

```csharp
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //[INICIO][12/10/2026][Rodriale][1. Estructura nueva; las columnas de sede nacen opcionales para poder rellenarlas]
            migrationBuilder.CreateTable(
                name: "InventariosSucursal",
                schema: "customcore",
                columns: table => new
                {
                    IdInventario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: false),
                    RepuestoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventariosSucursal", x => x.IdInventario);
                    table.CheckConstraint("CK_InventariosSucursal_CantidadNoNegativa", "\"Cantidad\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventariosSucursal_Repuestos_RepuestoId",
                        column: x => x.RepuestoId,
                        principalSchema: "customcore",
                        principalTable: "Repuestos",
                        principalColumn: "IdRepuesto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventariosSucursal_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalSchema: "customcore",
                        principalTable: "Sucursales",
                        principalColumn: "IdSucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            foreach (var tabla in new[] { "Usuarios", "OrdenesTrabajoEncabezado", "Citas" })
            {
                migrationBuilder.AddColumn<int>(
                    name: "SucursalId",
                    schema: "customcore",
                    table: tabla,
                    type: "integer",
                    nullable: true);
            }
            //[FIN][12/10/2026][Rodriale][1. Estructura nueva]

            //[INICIO][12/10/2026][Rodriale][2-4. Sede por defecto y traslado: el stock actual pasa a esa sede, junto con órdenes, citas y empleados (no los Gerentes)]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Sucursales" ("Nombre", "Direccion", "Activo")
                SELECT 'Central', 'Por definir', TRUE
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Sucursales");

                INSERT INTO customcore."InventariosSucursal" ("SucursalId", "RepuestoId", "Cantidad")
                SELECT (SELECT MIN("IdSucursal") FROM customcore."Sucursales"), r."IdRepuesto", r."CantidadStock"
                FROM customcore."Repuestos" r;

                UPDATE customcore."OrdenesTrabajoEncabezado"
                SET "SucursalId" = (SELECT MIN("IdSucursal") FROM customcore."Sucursales");

                UPDATE customcore."Citas"
                SET "SucursalId" = (SELECT MIN("IdSucursal") FROM customcore."Sucursales");

                UPDATE customcore."Usuarios" u
                SET "SucursalId" = (SELECT MIN("IdSucursal") FROM customcore."Sucursales")
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM customcore."UsuarioRoles" ur
                    JOIN customcore."Roles" ro ON ro."IdRol" = ur."RolId"
                    WHERE ur."UsuarioId" = u."IdUsuario" AND ro."Nombre" = 'Gerente');
                """);
            //[FIN][12/10/2026][Rodriale][2-4. Sede por defecto y traslado]

            //[INICIO][12/10/2026][Rodriale][5. Con los datos ya rellenos, las restricciones y la columna vieja fuera]
            foreach (var tabla in new[] { "OrdenesTrabajoEncabezado", "Citas" })
            {
                migrationBuilder.AlterColumn<int>(
                    name: "SucursalId",
                    schema: "customcore",
                    table: tabla,
                    type: "integer",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "integer",
                    oldNullable: true);
            }

            foreach (var tabla in new[] { "Usuarios", "OrdenesTrabajoEncabezado", "Citas" })
            {
                migrationBuilder.CreateIndex(
                    name: $"IX_{tabla}_SucursalId",
                    schema: "customcore",
                    table: tabla,
                    column: "SucursalId");

                migrationBuilder.AddForeignKey(
                    name: $"FK_{tabla}_Sucursales_SucursalId",
                    schema: "customcore",
                    table: tabla,
                    column: "SucursalId",
                    principalSchema: "customcore",
                    principalTable: "Sucursales",
                    principalColumn: "IdSucursal",
                    onDelete: ReferentialAction.Restrict);
            }

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSucursal_RepuestoId",
                schema: "customcore",
                table: "InventariosSucursal",
                column: "RepuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSucursal_SucursalId_RepuestoId",
                schema: "customcore",
                table: "InventariosSucursal",
                columns: new[] { "SucursalId", "RepuestoId" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "CantidadStock",
                schema: "customcore",
                table: "Repuestos");
            //[FIN][12/10/2026][Rodriale][5. Restricciones y columna vieja fuera]

            //[INICIO][12/10/2026][Rodriale][6. Permisos nuevos, sin duplicar, y asignados al Gerente para que nadie pierda acceso al migrar]
            migrationBuilder.Sql("""
                INSERT INTO customcore."Permisos" ("NombreCodigo", "Descripcion", "Modulo")
                SELECT p.codigo, p.descripcion, p.modulo
                FROM (VALUES
                    ('sucursales.leer', 'Ver el catálogo de sucursales', 'Sucursales'),
                    ('sucursales.gestionar', 'Crear, editar y cerrar sucursales', 'Sucursales'),
                    ('sucursales.todas', 'Operar y ver datos de todas las sucursales', 'Sucursales'),
                    ('inventario.leer', 'Ver existencias por sucursal', 'Inventario'),
                    ('inventario.ajustar', 'Fijar existencias de un repuesto en una sucursal', 'Inventario')
                ) AS p(codigo, descripcion, modulo)
                WHERE NOT EXISTS (SELECT 1 FROM customcore."Permisos" x WHERE x."NombreCodigo" = p.codigo);

                INSERT INTO customcore."RolPermisos" ("RolId", "PermisoId")
                SELECT r."IdRol", p."IdPermiso"
                FROM customcore."Roles" r
                JOIN customcore."Permisos" p ON p."NombreCodigo" IN
                    ('sucursales.leer', 'sucursales.gestionar', 'sucursales.todas', 'inventario.leer', 'inventario.ajustar')
                WHERE r."Nombre" = 'Gerente'
                  AND NOT EXISTS (
                      SELECT 1 FROM customcore."RolPermisos" rp
                      WHERE rp."RolId" = r."IdRol" AND rp."PermisoId" = p."IdPermiso");
                """);
            //[FIN][12/10/2026][Rodriale][6. Permisos nuevos]
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //[INICIO][12/10/2026][Rodriale][Vuelve al stock único sumando todas las sedes, para revertir sin perder existencias. Los permisos se quedan: pudieron asignarse a mano]
            migrationBuilder.AddColumn<int>(
                name: "CantidadStock",
                schema: "customcore",
                table: "Repuestos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE customcore."Repuestos" r
                SET "CantidadStock" = COALESCE(
                    (SELECT SUM(i."Cantidad") FROM customcore."InventariosSucursal" i WHERE i."RepuestoId" = r."IdRepuesto"), 0);
                """);

            foreach (var tabla in new[] { "Usuarios", "OrdenesTrabajoEncabezado", "Citas" })
            {
                migrationBuilder.DropForeignKey(
                    name: $"FK_{tabla}_Sucursales_SucursalId",
                    schema: "customcore",
                    table: tabla);

                migrationBuilder.DropIndex(
                    name: $"IX_{tabla}_SucursalId",
                    schema: "customcore",
                    table: tabla);

                migrationBuilder.DropColumn(
                    name: "SucursalId",
                    schema: "customcore",
                    table: tabla);
            }

            migrationBuilder.DropTable(
                name: "InventariosSucursal",
                schema: "customcore");
            //[FIN][12/10/2026][Rodriale][Vuelve al stock único sumando todas las sedes]
        }
```

- [ ] **Paso 11: Confirmar que el modelo y la migración cuadran**

Ejecutar: `dotnet build --nologo -v q` y luego `dotnet ef migrations has-pending-model-changes --no-build`
Esperado: `0 Errores` y `No changes have been made to the model since the last migration.`

- [ ] **Paso 12: Guardar una foto del stock y aplicar**

Definir el ayudante de `psql` para esta sesión de PowerShell. Lee la cadena de User Secrets sin imprimirla:

```powershell
$linea = dotnet user-secrets list | Where-Object { $_ -like 'ConnectionStrings:Default = *' }
$cs = @{}; $linea.Substring('ConnectionStrings:Default = '.Length).Split(';') | Where-Object { $_ } | ForEach-Object { $k, $v = $_.Split('=', 2); $cs[$k.Trim().ToLower()] = $v.Trim() }
if ($cs['host'] -ne 'localhost') { throw 'Default no apunta a localhost' }
$env:PGPASSWORD = $cs['password']
$puerto = if ($cs['port']) { $cs['port'] } else { '5432' }
function Sql([string]$q) { & 'C:\Program Files\PostgreSQL\16\bin\psql.exe' -h localhost -p $puerto -U $cs['username'] -d $cs['database'] -v ON_ERROR_STOP=1 -qAt -c $q }
```

```powershell
Sql 'SELECT SUM("CantidadStock") FROM customcore."Repuestos"'
dotnet ef database update
```

Anotar la suma. Esperado: la migración se aplica sin errores.

- [ ] **Paso 13: Comprobar el traslado**

```powershell
Sql 'SELECT COALESCE(SUM("Cantidad"),0) FROM customcore."InventariosSucursal"'
Sql 'SELECT COUNT(*) FROM customcore."OrdenesTrabajoEncabezado" WHERE "SucursalId" IS NULL'
Sql 'SELECT COUNT(*) FROM customcore."Citas" WHERE "SucursalId" IS NULL'
Sql 'SELECT COUNT(*) FROM customcore."RolPermisos" rp JOIN customcore."Roles" r ON r."IdRol" = rp."RolId" JOIN customcore."Permisos" p ON p."IdPermiso" = rp."PermisoId" WHERE r."Nombre" = ''Gerente'' AND (p."NombreCodigo" LIKE ''sucursales.%'' OR p."NombreCodigo" LIKE ''inventario.%'')'
```

Esperado: la primera suma es igual a la del paso 12; luego `0`, `0`, y `5` (o `0` si en esta base no existe el rol `Gerente`, en cuyo caso hay que avisarle a Rodrigo antes de la Tarea 8).

- [ ] **Paso 14: Probar la reversión ida y vuelta**

```powershell
dotnet ef database update AgregarSucursales
Sql 'SELECT SUM("CantidadStock") FROM customcore."Repuestos"'
dotnet ef database update
Sql 'SELECT COALESCE(SUM("Cantidad"),0) FROM customcore."InventariosSucursal"'
```

Esperado: las dos sumas son iguales a la del paso 12, y las dos actualizaciones terminan sin errores.

- [ ] **Paso 15: Commit**

```bash
git add -A CustomCore.API
git commit -m "Rodriale: inventario por sucursal, sede en usuarios, órdenes y citas, y SucursalActual." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 3: Órdenes — alcance por sede y mecánico de la misma sede

**Archivos:**
- Modificar: `Dtos/OrdenTrabajoDtos.cs`, `Servicios/OrdenTrabajoService.cs`

**Interfaces:**
- Consume (Tarea 2): `SucursalActual.AlcanceAsync`, `OrdenEditable`, `OrdenTrabajoEncabezado.SucursalId`, `Usuario.SucursalId`.
- Produce: `ConsultaOrdenes.SucursalId : int?`; `OrdenListaDto` y `OrdenDetalleDto` con `int SucursalId` como segundo parámetro.

- [ ] **Paso 1: DTOs**

En `Dtos/OrdenTrabajoDtos.cs`:

En `ConsultaOrdenes`, después de `public DateTimeOffset? Hasta { get; init; }`:

```csharp

        //[INICIO][12/10/2026][Rodriale][Filtro por sede; solo lo respeta quien ve todas, al empleado se le fija la suya]
        [Range(1, int.MaxValue)]
        public int? SucursalId { get; init; }
        //[FIN][12/10/2026][Rodriale][Filtro por sede]
```

En `OrdenListaDto` y en `OrdenDetalleDto`, agregar `int SucursalId,` justo después de `int IdOrdenEncabezado,`.

- [ ] **Paso 2: Listado y detalle con alcance**

En `Servicios/OrdenTrabajoService.cs`, en `ObtenerAsync`, justo después de `var query = db.OrdenesTrabajoEncabezado.AsNoTracking();`:

```csharp

            //[INICIO][12/10/2026][Rodriale][El empleado solo ve su sede aunque pida otra; quien ve todas puede filtrar]
            var filtroSucursal = await sucursal.AlcanceAsync(cancellationToken) ?? consulta.SucursalId;
            if (filtroSucursal is { } sucursalId)
                query = query.Where(o => o.SucursalId == sucursalId);
            //[FIN][12/10/2026][Rodriale][El empleado solo ve su sede]
```

En la proyección `new OrdenListaDto(`, agregar `o.SucursalId,` después de `o.IdOrdenEncabezado,`.

Reemplazar `ObtenerPorIdAsync` (que hoy tiene cuerpo de expresión) por:

```csharp
        //[INICIO][31/8/2026][Rodriale][Detalle completo en una sola consulta: orden, vehículo, dueño, responsables y líneas, sin caer en N+1]
        public async Task<OrdenDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
        {
            //[INICIO][12/10/2026][Rodriale][Una orden de otra sede se comporta como inexistente]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Una orden de otra sede se comporta como inexistente]

            return await db.OrdenesTrabajoEncabezado
                .AsNoTracking()
                .Where(o => o.IdOrdenEncabezado == id && (alcance == null || o.SucursalId == alcance))
                .Select(o => new OrdenDetalleDto(
                    o.IdOrdenEncabezado,
                    o.SucursalId,
                    o.VehiculoId,
                    o.Vehiculo.Placa,
                    o.Vehiculo.Marca,
                    o.Vehiculo.Modelo,
                    o.Vehiculo.ClienteId,
                    o.Vehiculo.Cliente.NombreCompleto,
                    o.FechaIngreso,
                    o.FechaFinalizacion,
                    o.Diagnostico,
                    o.Estado,
                    o.TotalEstimado,
                    o.UsuarioRecepcion.NombreCompleto,
                    o.MecanicoAsignado != null ? o.MecanicoAsignado.NombreCompleto : null,
                    o.Detalles
                        .OrderBy(d => d.IdOrdenDetalle)
                        .Select(d => new LineaOrdenDto(
                            d.IdOrdenDetalle,
                            d.RepuestoId != null ? "Repuesto" : "Servicio",
                            d.RepuestoId,
                            d.ServicioId,
                            d.RepuestoId != null ? d.Repuesto!.Nombre : d.Servicio!.Nombre,
                            d.Cantidad,
                            d.PrecioUnitario,
                            d.SubtotalEstimado))
                        .ToList()))
                .FirstOrDefaultAsync(cancellationToken);
        }
        //[FIN][31/8/2026][Rodriale][Detalle completo en una sola consulta]
```

- [ ] **Paso 3: Alcance en toda edición**

En `ExisteYNoEstaFacturadaAsync` (el de la Tarea 2), al inicio del método:

```csharp
            //[INICIO][12/10/2026][Rodriale][Editar una orden de otra sede responde 404, igual que si no existiera]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Editar una orden de otra sede responde 404]

```

y cambiar su `Where` a:

```csharp
                .Where(o => o.IdOrdenEncabezado == ordenId && (alcance == null || o.SucursalId == alcance))
```

Esto cubre actualizar, cambiar de estado, agregar línea y quitar línea, porque todos pasan por aquí.

- [ ] **Paso 4: El mecánico debe ser de la sede de la orden**

Agregar este método privado antes de `CargarRepuestosAsync`:

```csharp
        //[INICIO][12/10/2026][Rodriale][El mecánico asignado tiene que trabajar en la sede de la orden]
        private async Task ValidarMecanicoAsync(
            int? mecanicoId,
            int sucursalId,
            CancellationToken cancellationToken)
        {
            if (mecanicoId is not { } id)
                return;

            var esDeLaSede = await db.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.IdUsuario == id && u.Activo && u.SucursalId == sucursalId, cancellationToken);

            if (!esDeLaSede)
                throw new ConflictoNegocioException(
                    "El mecánico asignado no existe, está inactivo o no pertenece a la sucursal de la orden.");
        }
        //[FIN][12/10/2026][Rodriale][El mecánico asignado tiene que trabajar en la sede de la orden]
```

En `CrearInternoAsync`, justo después de `var sucursalId = await sucursal.ResolverAsync(...)` y su `//[FIN]`:

```csharp

            await ValidarMecanicoAsync(request.MecanicoAsignadoId, sucursalId, cancellationToken);
```

En `ActualizarAsync`, reemplazar:

```csharp
            if (await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken) is null)
                return false;
```

por:

```csharp
            var orden = await ExisteYNoEstaFacturadaAsync(ordenId, cancellationToken);
            if (orden is null)
                return false;

            await ValidarMecanicoAsync(request.MecanicoAsignadoId, orden.SucursalId, cancellationToken);
```

- [ ] **Paso 5: Compilar**

Ejecutar: `dotnet build --nologo -v q`
Esperado: `0 Errores`.

- [ ] **Paso 6: Commit**

```bash
git add CustomCore.API/Dtos/OrdenTrabajoDtos.cs CustomCore.API/Servicios/OrdenTrabajoService.cs
git commit -m "Rodriale: órdenes filtradas por sucursal y mecánico de la misma sede." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 4: Inventario por sede (consultar y fijar)

**Archivos:**
- Crear: `Dtos/InventarioDtos.cs`, `Servicios/InventarioService.cs`, `Controllers/InventarioController.cs`
- Modificar: `Program.cs`

**Interfaces:**
- Consume: `SucursalActual.AlcanceAsync`, `AppDbContext.InventariosSucursal`, `ActualizarAuditadoAsync`, `PermisosSistema.InventarioLeer|InventarioAjustar`, `AccesoDenegadoException`.
- Produce: `GET api/sucursales/{sucursalId}/inventario` → `ResultadoPaginado<InventarioDto>`; `PUT api/sucursales/{sucursalId}/inventario/{repuestoId}` con `FijarInventarioRequest` → `204`.

- [ ] **Paso 1: DTOs**

`Dtos/InventarioDtos.cs`:

```csharp
//[INICIO][12/10/2026][Rodriale][DTOs del inventario por sucursal]
using System.ComponentModel.DataAnnotations;

namespace CustomCore.API.Dtos
{
    //[INICIO][12/10/2026][Rodriale][Un repuesto del catálogo con sus existencias en la sede consultada; 0 si la sede nunca lo ha tenido]
    public record InventarioDto(
        int RepuestoId,
        string CodigoStock,
        string Nombre,
        decimal PrecioUnitario,
        int Cantidad);
    //[FIN][12/10/2026][Rodriale][Un repuesto del catálogo con sus existencias en la sede consultada]

    //[INICIO][12/10/2026][Rodriale][Valor absoluto: lo que se contó en bodega, no una suma o resta]
    public record FijarInventarioRequest(
        [Range(0, int.MaxValue)] int Cantidad);
    //[FIN][12/10/2026][Rodriale][Valor absoluto]
}
//[FIN][12/10/2026][Rodriale][DTOs del inventario por sucursal]
```

- [ ] **Paso 2: Servicio**

`Servicios/InventarioService.cs`:

```csharp
//[INICIO][12/10/2026][Rodriale][Servicio de inventario por sucursal: consultar y fijar existencias]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using CustomCore.API.Excepciones;
using CustomCore.API.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios
{
    public sealed class InventarioService(AppDbContext db, SucursalActual sucursal)
    {
        //[INICIO][12/10/2026][Rodriale][Catálogo activo con la cantidad de la sede en una sola consulta; sin fila cuenta como 0]
        public async Task<ResultadoPaginado<InventarioDto>?> ObtenerAsync(
            int sucursalId,
            ConsultaPaginada consulta,
            CancellationToken cancellationToken)
        {
            await ValidarAccesoAsync(sucursalId, cancellationToken);

            if (!await db.Sucursales.AsNoTracking().AnyAsync(s => s.IdSucursal == sucursalId, cancellationToken))
                return null;

            var query = db.Repuestos.AsNoTracking().Where(r => r.Activo);

            if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            {
                var patron = $"%{consulta.Busqueda.Trim()}%";
                query = query.Where(r =>
                    EF.Functions.ILike(r.CodigoStock, patron) ||
                    EF.Functions.ILike(r.Nombre, patron));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(r => r.Nombre)
                .Skip((consulta.Pagina - 1) * consulta.Tamanio)
                .Take(consulta.Tamanio)
                .Select(r => new InventarioDto(
                    r.IdRepuesto,
                    r.CodigoStock,
                    r.Nombre,
                    r.PrecioUnitario,
                    db.InventariosSucursal
                        .Where(i => i.SucursalId == sucursalId && i.RepuestoId == r.IdRepuesto)
                        .Select(i => i.Cantidad)
                        .FirstOrDefault()))
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<InventarioDto>(items, consulta.Pagina, consulta.Tamanio, total);
        }
        //[FIN][12/10/2026][Rodriale][Catálogo activo con la cantidad de la sede]

        //[INICIO][12/10/2026][Rodriale][Fija la existencia: actualiza con auditoría si hay fila, la crea si es la primera vez. false = sede o repuesto inexistente]
        public async Task<bool> FijarAsync(
            int sucursalId,
            int repuestoId,
            int cantidad,
            CancellationToken cancellationToken)
        {
            await ValidarAccesoAsync(sucursalId, cancellationToken);

            var repuestoActivo = await db.Repuestos
                .AsNoTracking()
                .Where(r => r.IdRepuesto == repuestoId)
                .Select(r => (bool?)r.Activo)
                .FirstOrDefaultAsync(cancellationToken);

            if (repuestoActivo is null ||
                !await db.Sucursales.AsNoTracking().AnyAsync(s => s.IdSucursal == sucursalId, cancellationToken))
                return false;

            if (repuestoActivo == false)
                throw new ConflictoNegocioException("El repuesto está dado de baja; no se le asignan existencias.");

            var idInventario = await db.InventariosSucursal
                .AsNoTracking()
                .Where(i => i.SucursalId == sucursalId && i.RepuestoId == repuestoId)
                .Select(i => (int?)i.IdInventario)
                .FirstOrDefaultAsync(cancellationToken);

            if (idInventario is { } id)
            {
                await db.InventariosSucursal
                    .Where(i => i.IdInventario == id)
                    .ActualizarAuditadoAsync(db, id, s => s.SetProperty(i => i.Cantidad, cantidad), cancellationToken);

                return true;
            }

            //[INICIO][12/10/2026][Rodriale][Si dos ajustes crean la misma fila a la vez, el índice único rechaza el segundo con 409]
            db.InventariosSucursal.Add(new InventarioSucursal
            {
                SucursalId = sucursalId,
                RepuestoId = repuestoId,
                Cantidad = cantidad
            });
            await db.SaveChangesAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Si dos ajustes crean la misma fila a la vez]

            return true;
        }
        //[FIN][12/10/2026][Rodriale][Fija la existencia]

        //[INICIO][12/10/2026][Rodriale][La sede viene explícita en la ruta: pedir otra es un 403 claro, no un 404]
        private async Task ValidarAccesoAsync(int sucursalId, CancellationToken cancellationToken)
        {
            if (await sucursal.AlcanceAsync(cancellationToken) is { } propia && propia != sucursalId)
                throw new AccesoDenegadoException("Solo puedes ver o ajustar el inventario de tu sucursal.");
        }
        //[FIN][12/10/2026][Rodriale][La sede viene explícita en la ruta]
    }
}
//[FIN][12/10/2026][Rodriale][Servicio de inventario por sucursal]
```

- [ ] **Paso 3: Controlador**

`Controllers/InventarioController.cs`:

```csharp
//[INICIO][12/10/2026][Rodriale][Controlador delgado del inventario por sucursal]
using CustomCore.API.Dtos;
using CustomCore.API.Seguridad;
using CustomCore.API.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomCore.API.Controllers
{
    [ApiController]
    [Route("api/sucursales/{sucursalId:int}/inventario")]
    [Authorize]
    public sealed class InventarioController(InventarioService servicio) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = PermisosSistema.InventarioLeer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultadoPaginado<InventarioDto>>> Obtener(
            int sucursalId,
            [FromQuery] ConsultaPaginada consulta,
            CancellationToken cancellationToken)
        {
            var inventario = await servicio.ObtenerAsync(sucursalId, consulta, cancellationToken);
            return inventario is null ? NotFound() : Ok(inventario);
        }

        [HttpPut("{repuestoId:int}")]
        [Authorize(Policy = PermisosSistema.InventarioAjustar)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Fijar(
            int sucursalId,
            int repuestoId,
            FijarInventarioRequest request,
            CancellationToken cancellationToken)
        {
            var fijado = await servicio.FijarAsync(sucursalId, repuestoId, request.Cantidad, cancellationToken);
            return fijado ? NoContent() : NotFound();
        }
    }
}
//[FIN][12/10/2026][Rodriale][Controlador delgado del inventario por sucursal]
```

En `Program.cs`, después de `builder.Services.AddScoped<SucursalActual>();`:

```csharp
builder.Services.AddScoped<InventarioService>();
```

- [ ] **Paso 4: Compilar**

Ejecutar: `dotnet build --nologo -v q`
Esperado: `0 Errores`.

- [ ] **Paso 5: Commit**

```bash
git add CustomCore.API/Dtos/InventarioDtos.cs CustomCore.API/Servicios/InventarioService.cs CustomCore.API/Controllers/InventarioController.cs CustomCore.API/Program.cs
git commit -m "Rodriale: endpoints de inventario por sucursal." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 5: Citas por sede

**Archivos:**
- Modificar: `Dtos/CitaDtos.cs`, `Servicios/CitaService.cs`

**Interfaces:**
- Consume: `SucursalActual.AlcanceAsync|ResolverAsync`, `Cita.SucursalId`.
- Produce: `ConsultaCitas.SucursalId : int?`; `CitaListaDto` y `CitaDetalleDto` con `int SucursalId` como segundo parámetro; `CrearCitaRequest` con el último parámetro `int? SucursalId = null`.

- [ ] **Paso 1: DTOs**

En `Dtos/CitaDtos.cs`:

En `ConsultaCitas`, después de `public EstadoCita? Estado { get; init; }`:

```csharp

        //[INICIO][12/10/2026][Rodriale][Filtro por sede; solo lo respeta quien ve todas]
        [Range(1, int.MaxValue)]
        public int? SucursalId { get; init; }
        //[FIN][12/10/2026][Rodriale][Filtro por sede]
```

En `CitaListaDto` y `CitaDetalleDto`, agregar `int SucursalId,` después de `int IdCita,`.

Reemplazar `CrearCitaRequest` por:

```csharp
    public record CrearCitaRequest(
        [Range(1, int.MaxValue)] int VehiculoId,
        DateTimeOffset FechaHora,
        [Range(15, 480)] int DuracionMinutos,
        [MaxLength(500)] string? Motivo,
        //[INICIO][12/10/2026][Rodriale][Solo lo usa quien ve todas las sedes; al empleado se le asigna la suya]
        [Range(1, int.MaxValue)] int? SucursalId = null);
        //[FIN][12/10/2026][Rodriale][Solo lo usa quien ve todas las sedes]
```

- [ ] **Paso 2: Servicio**

En `Servicios/CitaService.cs`:

1. Firma de la clase: `public sealed class CitaService(AppDbContext db, SucursalActual sucursal)`, y agregar `using CustomCore.API.Seguridad;`.

2. En `ObtenerAsync`, después de `var query = db.Citas.AsNoTracking();`:

```csharp

            //[INICIO][12/10/2026][Rodriale][El empleado solo ve la agenda de su sede; quien ve todas puede filtrar]
            var filtroSucursal = await sucursal.AlcanceAsync(cancellationToken) ?? consulta.SucursalId;
            if (filtroSucursal is { } sucursalId)
                query = query.Where(c => c.SucursalId == sucursalId);
            //[FIN][12/10/2026][Rodriale][El empleado solo ve la agenda de su sede]
```

y en `new CitaListaDto(`, agregar `c.SucursalId,` después de `c.IdCita,`.

3. Reemplazar `ObtenerPorIdAsync` por:

```csharp
        public async Task<CitaDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
        {
            //[INICIO][12/10/2026][Rodriale][Una cita de otra sede se comporta como inexistente]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Una cita de otra sede se comporta como inexistente]

            return await db.Citas
                .AsNoTracking()
                .Where(c => c.IdCita == id && (alcance == null || c.SucursalId == alcance))
                .Select(c => new CitaDetalleDto(
                    c.IdCita,
                    c.SucursalId,
                    c.VehiculoId,
                    c.Vehiculo.Placa,
                    c.Vehiculo.Marca,
                    c.Vehiculo.Modelo,
                    c.Vehiculo.ClienteId,
                    c.Vehiculo.Cliente.NombreCompleto,
                    c.Vehiculo.Cliente.Telefono,
                    c.FechaHora,
                    c.DuracionMinutos,
                    c.Motivo,
                    c.Estado))
                .FirstOrDefaultAsync(cancellationToken);
        }
```

4. En `CrearAsync`, justo antes de `await ValidarSolapamientoAsync(`:

```csharp
            //[INICIO][12/10/2026][Rodriale][La cita queda en la sede de quien la agenda]
            var sucursalId = await sucursal.ResolverAsync(request.SucursalId, cancellationToken);
            //[FIN][12/10/2026][Rodriale][La cita queda en la sede de quien la agenda]

```

y en `new Cita { ... }`, agregar `SucursalId = sucursalId,` después de `VehiculoId = request.VehiculoId,`.

5. En `ReprogramarAsync`, después del bloque de jgarciad8 con `BloquearFilaAsync<Cita>`:

```csharp

            //[INICIO][12/10/2026][Rodriale][Reprogramar una cita de otra sede responde 404]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Reprogramar una cita de otra sede responde 404]
```

y cambiar el `.Where(c => c.IdCita == id)` de la consulta `var cita = await db.Citas` a:

```csharp
                .Where(c => c.IdCita == id && (alcance == null || c.SucursalId == alcance))
```

6. Reemplazar `CambiarEstadoAsync` completo por:

```csharp
        public async Task<bool> CambiarEstadoAsync(
            int id,
            CambiarEstadoCitaRequest request,
            CancellationToken cancellationToken)
        {
            //[INICIO][12/10/2026][Rodriale][Con la sede en el WHERE, una cita ajena afecta 0 filas y responde 404]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Con la sede en el WHERE]

            var filasAfectadas = await db.Citas
                .Where(c => c.IdCita == id && (alcance == null || c.SucursalId == alcance))
                //[INICIO][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]
                .ActualizarAuditadoAsync(db, id, s => s.SetProperty(c => c.Estado, request.Estado), cancellationToken);
            //[FIN][5/10/2026][jgarciad8][Operación directa con bloqueo y log atómico]

            return filasAfectadas > 0;
        }
```

- [ ] **Paso 3: Compilar**

Ejecutar: `dotnet build --nologo -v q`
Esperado: `0 Errores`.

- [ ] **Paso 4: Commit**

```bash
git add CustomCore.API/Dtos/CitaDtos.cs CustomCore.API/Servicios/CitaService.cs
git commit -m "Rodriale: citas con sucursal y agenda filtrada por sede." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 6: Facturas y pagos por la sede de la orden

**Archivos:**
- Modificar: `Dtos/FacturaDtos.cs`, `Servicios/FacturaService.cs`, `Servicios/PagoStripeService.cs`

**Interfaces:**
- Consume: `SucursalActual.AlcanceAsync`, `OrdenTrabajoEncabezado.SucursalId`.
- Produce: `ConsultaFacturas.SucursalId : int?`; `FacturaListaDto` y `FacturaDetalleDto` con `int SucursalId` como segundo parámetro.

- [ ] **Paso 1: DTOs**

En `Dtos/FacturaDtos.cs`:

En `ConsultaFacturas`, después de `public DateTimeOffset? Hasta { get; init; }`:

```csharp

        //[INICIO][12/10/2026][Rodriale][Filtro por sede de la orden; solo lo respeta quien ve todas]
        [Range(1, int.MaxValue)]
        public int? SucursalId { get; init; }
        //[FIN][12/10/2026][Rodriale][Filtro por sede de la orden]
```

En `FacturaListaDto` y `FacturaDetalleDto`, agregar `int SucursalId,` después de `int IdFacturaEncabezado,`.

- [ ] **Paso 2: `FacturaService`**

1. Firma: `public sealed class FacturaService(AppDbContext db, IOptions<OpcionesFacturacion> opciones, SucursalActual sucursal)`, y agregar `using CustomCore.API.Seguridad;`.

2. En `ObtenerAsync`, después de `var query = db.FacturasEncabezado.AsNoTracking();`:

```csharp

            //[INICIO][12/10/2026][Rodriale][La sede de la factura es la de su orden; no se duplica en la factura]
            var filtroSucursal = await sucursal.AlcanceAsync(cancellationToken) ?? consulta.SucursalId;
            if (filtroSucursal is { } sucursalId)
                query = query.Where(f => f.OrdenTrabajo.SucursalId == sucursalId);
            //[FIN][12/10/2026][Rodriale][La sede de la factura es la de su orden]
```

y en `new FacturaListaDto(`, agregar `f.OrdenTrabajo.SucursalId,` después de `f.IdFacturaEncabezado,`.

3. Convertir `ObtenerPorIdAsync` en `async`: al inicio, `var alcance = await sucursal.AlcanceAsync(cancellationToken);` (con su comentario `//[INICIO][12/10/2026][Rodriale][Una factura de otra sede se comporta como inexistente]` … `//[FIN]`), cambiar `=> db.FacturasEncabezado` por un cuerpo `{ return await db.FacturasEncabezado ... ; }`, el `Where` a `.Where(f => f.IdFacturaEncabezado == id && (alcance == null || f.OrdenTrabajo.SucursalId == alcance))`, y en `new FacturaDetalleDto(`, agregar `f.OrdenTrabajo.SucursalId,` después de `f.IdFacturaEncabezado,`. La firma queda `public async Task<FacturaDetalleDto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)`.

4. En `CrearAsync`, después del bloque de jgarciad8 con `BloquearFilaAsync<OrdenTrabajoEncabezado>`:

```csharp

            //[INICIO][12/10/2026][Rodriale][Solo se factura una orden de la propia sede; la ajena responde 404]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Solo se factura una orden de la propia sede]
```

y cambiar el `Where` de la consulta de la orden a:

```csharp
                .Where(o => o.IdOrdenEncabezado == request.OrdenTrabajoId && (alcance == null || o.SucursalId == alcance))
```

5. En `RegistrarPagoAsync`, después del bloque de jgarciad8 con `BloquearFilaAsync<FacturaEncabezado>`, agregar el mismo `var alcance = ...` con el comentario `[Registrar el pago de una factura de otra sede responde 404]`, y cambiar el `Where` de la consulta `var estado = ...` a:

```csharp
                .Where(f => f.IdFacturaEncabezado == id && (alcance == null || f.OrdenTrabajo.SucursalId == alcance))
```

- [ ] **Paso 3: `PagoStripeService`**

1. Agregar `SucursalActual sucursal,` a los parámetros del constructor, después de `IOptions<OpcionesStripe> opciones,`, y `using CustomCore.API.Seguridad;`.

2. En `ObtenerFacturaPendienteAsync`, al inicio del método:

```csharp
            //[INICIO][12/10/2026][Rodriale][Cobrar una factura de otra sede responde 404; el webhook no pasa por aquí]
            var alcance = await sucursal.AlcanceAsync(cancellationToken);
            //[FIN][12/10/2026][Rodriale][Cobrar una factura de otra sede responde 404]

```

y cambiar su `Where` a:

```csharp
                .Where(f => f.IdFacturaEncabezado == facturaId && (alcance == null || f.OrdenTrabajo.SucursalId == alcance))
```

`ProcesarWebhookAsync` y `MarcarPagadaAsync` **no** se tocan.

- [ ] **Paso 4: Compilar**

Ejecutar: `dotnet build --nologo -v q`
Esperado: `0 Errores`.

- [ ] **Paso 5: Commit**

```bash
git add CustomCore.API/Dtos/FacturaDtos.cs CustomCore.API/Servicios/FacturaService.cs CustomCore.API/Servicios/PagoStripeService.cs
git commit -m "Rodriale: facturas y cobros filtrados por la sucursal de la orden." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 7: Usuarios con sede, sucursales protegidas y auditadas

**Archivos:**
- Modificar: `Dtos/UsuarioDtos.cs`, `Servicios/UsuarioService.cs`, `Controllers/SucursalesController.cs`, `Servicios/SucursalService.cs`

**Interfaces:**
- Consume: `Usuario.SucursalId`, `PermisosSistema.SucursalesLeer|SucursalesGestionar`, `ActualizarAuditadoAsync`.
- Produce: `UsuarioResumenDto` con el último parámetro `int? SucursalId`; `CrearUsuarioRequest` y `ActualizarUsuarioRequest` con el último parámetro `int? SucursalId = null`.

- [ ] **Paso 1: DTOs de usuario**

En `Dtos/UsuarioDtos.cs`:

- `UsuarioResumenDto`: agregar `int? SucursalId` como último parámetro (después de `bool Activo`, con la coma correspondiente).
- `CrearUsuarioRequest`: después del parámetro `string Password`, agregar:

```csharp
,

    //[INICIO][12/10/2026][Rodriale][Sede del empleado; se omite solo para quien tendrá sucursales.todas]
    [Range(1, int.MaxValue, ErrorMessage = "La sucursal no es válida.")]
    int? SucursalId = null
    //[FIN][12/10/2026][Rodriale][Sede del empleado]
```

(la coma va pegada a `string Password`).

- `ActualizarUsuarioRequest`: después de `string Correo`, agregar el mismo parámetro con su comentario. **`PUT` reemplaza el recurso completo: omitir `sucursalId` deja al usuario sin sede.**

- [ ] **Paso 2: `UsuarioService`**

1. Agregar el método privado al final de la clase:

```csharp
    //[INICIO][12/10/2026][Rodriale][Solo se asigna una sede que exista y esté abierta]
    private async Task ValidarSucursalAsync(int? sucursalId, CancellationToken cancellationToken)
    {
        if (sucursalId is { } id &&
            !await db.Sucursales.AsNoTracking().AnyAsync(s => s.IdSucursal == id && s.Activo, cancellationToken))
        {
            throw new ConflictoNegocioException("La sucursal no existe o está dada de baja.");
        }
    }
    //[FIN][12/10/2026][Rodriale][Solo se asigna una sede que exista y esté abierta]
```

2. En las tres construcciones de `new UsuarioResumenDto(` (`ObtenerAsync`, `ObtenerPorIdAsync`, `CrearAsync`), agregar `usuario.SucursalId` como último argumento.

3. En `CrearAsync`, justo antes de `var usuario = new Usuario`, agregar `await ValidarSucursalAsync(request.SucursalId, cancellationToken);`, y en el inicializador `SucursalId = request.SucursalId,` después de `Activo = true` (agregando la coma).

4. En `ActualizarAsync`, justo antes de `usuario.NombreCompleto = request.NombreCompleto.Trim();`, agregar `await ValidarSucursalAsync(request.SucursalId, cancellationToken);`, y después de `usuario.Correo = correoNormalizado;`, agregar `usuario.SucursalId = request.SucursalId;`. Cada línea nueva va con su `//[INICIO][12/10/2026][Rodriale][Sede del empleado]` … `//[FIN]`.

- [ ] **Paso 3: Proteger `SucursalesController`**

En `Controllers/SucursalesController.cs`:

- `using`: agregar `using CustomCore.API.Seguridad;` y `using Microsoft.AspNetCore.Authorization;`.
- Sobre la clase, después de `[Route("api/sucursales")]`:

```csharp
    //[INICIO][12/10/2026][Rodriale][Antes cualquiera podía crear o cerrar sedes sin iniciar sesión]
    [Authorize]
    //[FIN][12/10/2026][Rodriale][Antes cualquiera podía crear o cerrar sedes sin iniciar sesión]
```

- `Obtener` y `ObtenerPorId`: `[Authorize(Policy = PermisosSistema.SucursalesLeer)]`.
- `Crear`, `Actualizar` y `DarDeBaja`: `[Authorize(Policy = PermisosSistema.SucursalesGestionar)]`.

- [ ] **Paso 4: Auditar `SucursalService`**

En `Servicios/SucursalService.cs`, en `ActualizarAsync` y `DarDeBajaAsync`, reemplazar `.ExecuteUpdateAsync(p => p` por `.ActualizarAuditadoAsync(db, id, p => p` (el resto de cada cadena queda igual), envolviendo cada cambio en:

```csharp
                //[INICIO][12/10/2026][Rodriale][Operación directa con bloqueo y log atómico, como el resto de catálogos]
                ...
                //[FIN][12/10/2026][Rodriale][Operación directa con bloqueo y log atómico]
```

(`using CustomCore.API.Data;` ya está.)

- [ ] **Paso 5: Compilar**

Ejecutar: `dotnet build --nologo -v q`
Esperado: `0 Errores`.

- [ ] **Paso 6: Commit**

```bash
git add CustomCore.API/Dtos/UsuarioDtos.cs CustomCore.API/Servicios/UsuarioService.cs CustomCore.API/Controllers/SucursalesController.cs CustomCore.API/Servicios/SucursalService.cs
git commit -m "Rodriale: sede en usuarios, sucursales con permisos y auditoría." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 8: Verificación de punta a punta

**Archivos:**
- Crear (sin versionar): `C:\Users\roood\AppData\Local\Temp\claude\C--Users-roood-source-repos-CustomCore-API-CustomCore-API\1e40a9c9-05d1-40b9-9655-1c344a820871\scratchpad\probar-sucursales.ps1`

Los datos de prueba llevan un sufijo aleatorio por ejecución y se quedan en `pruebas_customdb`; no se borran, porque la bitácora los referencia. La contraseña de prueba se genera en memoria en cada ejecución y no se guarda.

- [ ] **Paso 1: Comprobar el estado del modelo**

Ejecutar: `dotnet build --nologo -v q` y `dotnet ef migrations has-pending-model-changes --no-build`
Esperado: `0 Errores` y sin cambios pendientes.

- [ ] **Paso 2: Escribir el script**

```powershell
# Verificación de sucursales S2–S4 contra la API local. No se versiona.
param([string]$Api = 'http://localhost:5239')
$ErrorActionPreference = 'Stop'
$proyecto = 'C:\Users\roood\source\repos\CustomCore.API\CustomCore.API'

# --- Conexión local leída de User Secrets, sin imprimirla ---
$linea = dotnet user-secrets list --project $proyecto | Where-Object { $_ -like 'ConnectionStrings:Default = *' }
$cs = @{}
$linea.Substring('ConnectionStrings:Default = '.Length).Split(';') | Where-Object { $_ } |
    ForEach-Object { $k, $v = $_.Split('=', 2); $cs[$k.Trim().ToLower()] = $v.Trim() }
if ($cs['host'] -ne 'localhost') { throw 'Default no apunta a localhost; no se ejecuta.' }
$env:PGPASSWORD = $cs['password']
$puerto = if ($cs['port']) { $cs['port'] } else { '5432' }
function Sql([string]$q) {
    $r = & 'C:\Program Files\PostgreSQL\16\bin\psql.exe' -h localhost -p $puerto -U $cs['username'] -d $cs['database'] -v ON_ERROR_STOP=1 -qAt -c $q
    if ($LASTEXITCODE -ne 0) { throw "psql falló: $q" }
    $r
}

# --- Ayudantes HTTP ---
function Llamar([string]$metodo, [string]$ruta, [string]$token, $cuerpo) {
    $p = @{ Method = $metodo; Uri = "$Api$ruta"; UseBasicParsing = $true; ContentType = 'application/json' }
    if ($token) { $p.Headers = @{ Authorization = "Bearer $token" } }
    if ($null -ne $cuerpo) { $p.Body = [Text.Encoding]::UTF8.GetBytes(($cuerpo | ConvertTo-Json -Depth 5)) }
    try {
        $r = Invoke-WebRequest @p
        return [pscustomobject]@{ Codigo = [int]$r.StatusCode; Cuerpo = $r.Content; Location = $r.Headers['Location'] }
    } catch {
        $resp = $_.Exception.Response
        if ($null -eq $resp) { throw }
        return [pscustomobject]@{ Codigo = [int]$resp.StatusCode; Cuerpo = $null; Location = $null }
    }
}
$script:fallos = 0
function Esperar([string]$nombre, [int]$esperado, $resultado) {
    if ($resultado.Codigo -eq $esperado) { Write-Host "OK    $nombre ($esperado)" }
    else { Write-Host "FALLA $nombre - esperado $esperado, llegó $($resultado.Codigo)" -ForegroundColor Red; $script:fallos++ }
}
function Verificar([string]$nombre, [bool]$condicion) {
    if ($condicion) { Write-Host "OK    $nombre" } else { Write-Host "FALLA $nombre" -ForegroundColor Red; $script:fallos++ }
}
function IdDe($resultado) { [int](($resultado.Location -split '/')[-1]) }

# --- Datos de prueba ---
$s = Get-Random -Minimum 10000 -Maximum 99999
$bytes = New-Object byte[] 18; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$pw = [Convert]::ToBase64String($bytes)

if ((Sql 'SELECT COUNT(*) FROM customcore."Roles" WHERE "Nombre" = ''Gerente''') -ne '1') { throw 'No existe el rol Gerente en esta base.' }
Sql 'CREATE EXTENSION IF NOT EXISTS pgcrypto' | Out-Null

$A = Sql "INSERT INTO customcore.""Sucursales"" (""Nombre"",""Direccion"") VALUES ('Prueba A $s','x') RETURNING ""IdSucursal"""
$B = Sql "INSERT INTO customcore.""Sucursales"" (""Nombre"",""Direccion"") VALUES ('Prueba B $s','x') RETURNING ""IdSucursal"""

$rolEmpleado = Sql "INSERT INTO customcore.""Roles"" (""Nombre"") VALUES ('Empleado prueba $s') RETURNING ""IdRol"""
Sql @"
INSERT INTO customcore."RolPermisos" ("RolId","PermisoId")
SELECT $rolEmpleado, "IdPermiso" FROM customcore."Permisos"
WHERE "NombreCodigo" LIKE 'ordenes.%' OR "NombreCodigo" LIKE 'citas.%'
   OR "NombreCodigo" IN ('facturas.leer','facturas.crear','inventario.leer','inventario.ajustar');
"@ | Out-Null

function NuevoUsuario([string]$nombre, [string]$correo, [string]$sede, [string]$rolSql) {
    $id = Sql "INSERT INTO customcore.""Usuarios"" (""NombreCompleto"",""Correo"",""PasswordHash"",""Activo"",""SucursalId"") VALUES ('$nombre','$correo', crypt('$pw', gen_salt('bf', 11)), TRUE, $sede) RETURNING ""IdUsuario"""
    Sql "INSERT INTO customcore.""UsuarioRoles"" (""UsuarioId"",""RolId"") $rolSql".Replace('{id}', $id) | Out-Null
    $id
}
$rolGerente = 'SELECT {id}, "IdRol" FROM customcore."Roles" WHERE "Nombre" = ''Gerente'''
$rolEmp = "SELECT {id}, $rolEmpleado"
$idGerente = NuevoUsuario 'Gerente prueba' "gerente.$s@prueba.test" 'NULL' $rolGerente
$idA = NuevoUsuario 'Empleado A' "a.$s@prueba.test" $A $rolEmp
$idB = NuevoUsuario 'Empleado B' "b.$s@prueba.test" $B $rolEmp
$idSinSede = NuevoUsuario 'Sin sede' "sin.$s@prueba.test" 'NULL' $rolEmp

$cliente = Sql "INSERT INTO customcore.""Clientes"" (""NombreCompleto"",""Telefono"",""Correo"") VALUES ('Cliente $s','5555','c.$s@prueba.test') RETURNING ""IdCliente"""
$vehiculo = Sql "INSERT INTO customcore.""Vehiculos"" (""ClienteId"",""Placa"",""Marca"",""Modelo"",""Anio"") VALUES ($cliente,'P$s','Marca','Modelo',2020) RETURNING ""IdVehiculo"""
$rep = Sql "INSERT INTO customcore.""Repuestos"" (""CodigoStock"",""Nombre"",""PrecioUnitario"") VALUES ('R$s','Repuesto $s',100) RETURNING ""IdRepuesto"""
$repSinFila = Sql "INSERT INTO customcore.""Repuestos"" (""CodigoStock"",""Nombre"",""PrecioUnitario"") VALUES ('S$s','Sin fila $s',50) RETURNING ""IdRepuesto"""
function Stock([string]$sede) { [int](Sql "SELECT COALESCE((SELECT ""Cantidad"" FROM customcore.""InventariosSucursal"" WHERE ""SucursalId""=$sede AND ""RepuestoId""=$rep),-1)") }

function Token([string]$correo) {
    $r = Llamar 'POST' '/api/auth/login' $null @{ correo = $correo; password = $pw }
    if ($r.Codigo -ne 200) { throw "Login falló para $correo ($($r.Codigo))" }
    ($r.Cuerpo | ConvertFrom-Json).accessToken
}
$tG = Token "gerente.$s@prueba.test"; $tA = Token "a.$s@prueba.test"; $tB = Token "b.$s@prueba.test"; $tX = Token "sin.$s@prueba.test"
$linea1 = @(@{ repuestoId = [int]$rep; cantidad = 1 })

# --- Escenarios ---
Esperar 'Sucursales sin token' 401 (Llamar 'GET' '/api/sucursales' $null $null)

Esperar 'Gerente fija stock A=0' 204 (Llamar 'PUT' "/api/sucursales/$A/inventario/$rep" $tG @{ cantidad = 0 })
Esperar 'Gerente fija stock B=5' 204 (Llamar 'PUT' "/api/sucursales/$B/inventario/$rep" $tG @{ cantidad = 5 })
Esperar 'Empleado A ajusta inventario de B' 403 (Llamar 'PUT' "/api/sucursales/$B/inventario/$rep" $tA @{ cantidad = 9 })
Esperar 'Empleado A lee inventario de B' 403 (Llamar 'GET' "/api/sucursales/$B/inventario" $tA $null)
Esperar 'Empleado A lee su inventario' 200 (Llamar 'GET' "/api/sucursales/$A/inventario" $tA $null)

Esperar 'Gerente crea orden sin sucursal' 400 (Llamar 'POST' '/api/ordenes' $tG @{ vehiculoId = [int]$vehiculo; usuarioRecepcionId = 1; lineas = $linea1 })
Esperar 'Sin sede crea orden' 403 (Llamar 'POST' '/api/ordenes' $tX @{ vehiculoId = [int]$vehiculo; usuarioRecepcionId = 1; lineas = $linea1 })
Esperar 'Sin sede lista órdenes' 403 (Llamar 'GET' '/api/ordenes' $tX $null)
Esperar 'Empleado A sin stock en su sede' 409 (Llamar 'POST' '/api/ordenes' $tA @{ vehiculoId = [int]$vehiculo; usuarioRecepcionId = 1; lineas = $linea1 })
Esperar 'Empleado A crea orden en B' 403 (Llamar 'POST' '/api/ordenes' $tA @{ vehiculoId = [int]$vehiculo; usuarioRecepcionId = 1; lineas = $linea1; sucursalId = [int]$B })
Esperar 'Repuesto sin fila en la sede' 409 (Llamar 'POST' '/api/ordenes' $tB @{ vehiculoId = [int]$vehiculo; usuarioRecepcionId = 1; lineas = @(@{ repuestoId = [int]$repSinFila; cantidad = 1 }) })
Esperar 'Mecánico de otra sede' 409 (Llamar 'POST' '/api/ordenes' $tB @{ vehiculoId = [int]$vehiculo; usuarioRecepcionId = 1; mecanicoAsignadoId = [int]$idA; lineas = $linea1 })

$r = Llamar 'POST' '/api/ordenes' $tB @{ vehiculoId = [int]$vehiculo; usuarioRecepcionId = 1; lineas = @(@{ repuestoId = [int]$rep; cantidad = 2 }) }
Esperar 'Empleado B crea orden' 201 $r
$ordenB = IdDe $r
Verificar 'Stock de B bajó a 3' ((Stock $B) -eq 3)
Verificar 'Orden quedó en la sede B' ((Sql "SELECT ""SucursalId"" FROM customcore.""OrdenesTrabajoEncabezado"" WHERE ""IdOrdenEncabezado""=$ordenB") -eq $B)

Esperar 'Empleado A lee orden de B' 404 (Llamar 'GET' "/api/ordenes/$ordenB" $tA $null)
Esperar 'Empleado B lee su orden' 200 (Llamar 'GET' "/api/ordenes/$ordenB" $tB $null)
Esperar 'Empleado A edita orden de B' 404 (Llamar 'PUT' "/api/ordenes/$ordenB" $tA @{ diagnostico = 'x' })
Esperar 'Asignar mecánico de otra sede' 409 (Llamar 'PUT' "/api/ordenes/$ordenB" $tB @{ mecanicoAsignadoId = [int]$idA; diagnostico = 'x' })
Esperar 'Asignar mecánico de la sede' 204 (Llamar 'PUT' "/api/ordenes/$ordenB" $tB @{ mecanicoAsignadoId = [int]$idB; diagnostico = 'x' })

$lineaId = Sql "SELECT ""IdOrdenDetalle"" FROM customcore.""OrdenesTrabajoDetalle"" WHERE ""OrdenTrabajoId""=$ordenB"
Esperar 'Gerente quita la línea' 204 (Llamar 'DELETE' "/api/ordenes/$ordenB/lineas/$lineaId" $tG $null)
Verificar 'Stock volvió a la sede de la orden (B=5)' ((Stock $B) -eq 5)
Verificar 'Stock de A intacto (A=0)' ((Stock $A) -eq 0)

$listaA = (Llamar 'GET' "/api/ordenes?sucursalId=$B&tamanio=100" $tA $null).Cuerpo | ConvertFrom-Json
Verificar 'Empleado A con ?sucursalId=B solo ve A' (@($listaA.items | Where-Object { $_.sucursalId -ne [int]$A }).Count -eq 0)
$listaG = (Llamar 'GET' "/api/ordenes?sucursalId=$B&tamanio=100" $tG $null).Cuerpo | ConvertFrom-Json
Verificar 'Gerente filtra por B' ((@($listaG.items).Count -ge 1) -and (@($listaG.items | Where-Object { $_.sucursalId -ne [int]$B }).Count -eq 0))

$r = Llamar 'POST' '/api/citas' $tA @{ vehiculoId = [int]$vehiculo; fechaHora = (Get-Date).ToUniversalTime().AddDays(3).ToString('o'); duracionMinutos = 60 }
Esperar 'Empleado A agenda cita' 201 $r
$citaA = IdDe $r
Esperar 'Empleado B lee cita de A' 404 (Llamar 'GET' "/api/citas/$citaA" $tB $null)
Esperar 'Empleado B cancela cita de A' 404 (Llamar 'PATCH' "/api/citas/$citaA/estado" $tB @{ estado = 'Cancelada' })

Esperar 'Empleado B agrega línea' 201 (Llamar 'POST' "/api/ordenes/$ordenB/lineas" $tB @{ repuestoId = [int]$rep; cantidad = 1 })
Esperar 'Empleado B finaliza orden' 204 (Llamar 'PATCH' "/api/ordenes/$ordenB/estado" $tB @{ estado = 'Finalizado' })
Esperar 'Empleado A factura orden de B' 404 (Llamar 'POST' '/api/facturas' $tA @{ ordenTrabajoId = $ordenB; usuarioCajeroId = 1 })
$r = Llamar 'POST' '/api/facturas' $tB @{ ordenTrabajoId = $ordenB; usuarioCajeroId = 1 }
Esperar 'Empleado B factura su orden' 201 $r
$facturaB = IdDe $r
Esperar 'Empleado A lee factura de B' 404 (Llamar 'GET' "/api/facturas/$facturaB" $tA $null)
Esperar 'Gerente lee factura de B' 200 (Llamar 'GET' "/api/facturas/$facturaB" $tG $null)

Write-Host ''
if ($script:fallos -eq 0) { Write-Host 'TODOS LOS ESCENARIOS PASARON' -ForegroundColor Green } else { Write-Host "$($script:fallos) ESCENARIO(S) FALLARON" -ForegroundColor Red; exit 1 }
```

- [ ] **Paso 3: Levantar la API local**

Con la herramienta de procesos en segundo plano (no en primer plano), desde el directorio del proyecto:

```powershell
dotnet run --launch-profile http
```

Esperar a que `GET http://localhost:5239/health` responda `200`.

- [ ] **Paso 4: Ejecutar el script**

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\roood\AppData\Local\Temp\claude\C--Users-roood-source-repos-CustomCore-API-CustomCore-API\1e40a9c9-05d1-40b9-9655-1c344a820871\scratchpad\probar-sucursales.ps1"
```

Esperado: todas las líneas en `OK` y `TODOS LOS ESCENARIOS PASARON`. Por cada `FALLA`, corregir el servicio correspondiente, volver a compilar, reiniciar la API, volver a ejecutar el script y hacer un commit aparte con la corrección.

- [ ] **Paso 5: Detener la API y revisar el estado de git**

Detener el proceso de `dotnet run`. `git status` debe estar limpio (el script vive fuera del repositorio).
