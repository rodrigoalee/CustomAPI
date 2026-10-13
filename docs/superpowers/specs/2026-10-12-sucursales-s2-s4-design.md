# Sucursales S2–S4: inventario por sede, sede en la operación y visibilidad por sede

**Fecha:** 2026-10-12
**Rama:** `Local/FeatureSucursales` (ya contiene S1 y todo `Local/FeatureRodriale`)
**Base de desarrollo:** `pruebas_customdb` en PostgreSQL local. Supabase no se toca en este trabajo.

## 1. Objetivo

Los Santos Customs opera en varias sedes. S1 dejó el catálogo `Sucursales`. Este trabajo completa la funcionalidad:

- **S2:** el stock de cada repuesto se lleva por sede.
- **S3:** órdenes, citas y usuarios quedan ligados a una sede; las facturas la heredan de su orden.
- **S4:** cada empleado opera y ve solo lo de su sede; quien tenga el permiso `sucursales.todas` ve todas.

### Reglas de negocio (decididas por Rodrigo)

1. El precio del catálogo es único para todas las sedes; solo el stock se separa.
2. Cada empleado pertenece a una sola sede. El Gerente no tiene sede.
3. La factura queda asociada a la sede donde se emitió, con numeración única (sin serie por establecimiento).
4. No hay traslados de repuestos entre sedes; cada sede fija su propio stock.
5. Clientes y vehículos no llevan sede: son del negocio y se atienden en cualquiera.
6. La sede de una orden o cita es la del empleado que la crea. Solo quien tiene `sucursales.todas` puede, y debe, indicarla.
7. El stock se fija por sede con un valor absoluto, igual que hoy se fija `CantidadStock`.
8. El acceso a todas las sedes lo da el permiso `sucursales.todas`, no tener la sede vacía. Si a alguien le falta la sede, el sistema falla cerrado.

### Criterios de éxito

- Una orden descuenta del inventario de su sede, y falla con `409` si esa sede no tiene suficiente, aunque otra sí tenga.
- Un empleado no puede leer, modificar ni facturar nada de otra sede: recibe `404` en registros y `403` en la ruta explícita de inventario.
- Al aplicar la migración, ningún dato se pierde: el stock total por repuesto es el mismo antes y después, y todos los registros existentes quedan en una sede.
- El Gerente conserva todo su acceso inmediatamente después de migrar.
- El controlador de sucursales deja de ser accesible sin iniciar sesión.

## 2. Modelo de datos

### Tabla nueva `InventariosSucursal` (entidad `InventarioSucursal`)

| Columna | Tipo | Notas |
|---|---|---|
| `IdInventario` | `int` PK identidad | Llave simple: `ActualizarAuditadoAsync` y `BloquearFilaAsync` exigen una sola llave `int`. |
| `SucursalId` | `int` FK → `Sucursales` | `Restrict` |
| `RepuestoId` | `int` FK → `Repuestos` | `Restrict` |
| `Cantidad` | `int` | `CHECK ("Cantidad" >= 0)` |

- Índice único (`SucursalId`, `RepuestoId`).
- Si una sede no tiene fila para un repuesto, se considera que tiene 0.

### Columnas nuevas

| Tabla | Columna | Nulabilidad | Índice |
|---|---|---|---|
| `Usuarios` | `SucursalId` FK `Restrict` | `int?` | sí |
| `OrdenesTrabajoEncabezado` | `SucursalId` FK `Restrict` | obligatoria | sí |
| `Citas` | `SucursalId` FK `Restrict` | obligatoria | sí |

### Columna eliminada

- `Repuestos.CantidadStock`.

### Facturas: sin columna nueva

La sede de una factura es `FacturaEncabezado.OrdenTrabajo.SucursalId`. Una factura pertenece a una sola orden y la sede de una orden no cambia nunca, así que copiarla sería duplicar un dato. El filtro por sede es un join por llave primaria.

## 3. Migración `InventarioPorSucursal`

Una sola migración, que EF ejecuta en una transacción. El orden importa:

1. Crear `InventariosSucursal`, y las columnas `SucursalId` en `Usuarios`, `OrdenesTrabajoEncabezado` y `Citas`, todas como opcionales.
2. Si `Sucursales` está vacía, insertar `('Central', 'Por definir', activo)`. La sede por defecto es la de menor `IdSucursal`.
3. `INSERT INTO InventariosSucursal (SucursalId, RepuestoId, Cantidad) SELECT <sede>, IdRepuesto, CantidadStock FROM Repuestos`.
4. Asignar la sede por defecto a todas las órdenes y citas, y a los usuarios que **no** tengan el rol de nombre `Gerente`.
5. Hacer obligatorios `OrdenesTrabajoEncabezado.SucursalId` y `Citas.SucursalId`; crear índices y llaves foráneas; eliminar `Repuestos.CantidadStock`.
6. Insertar los permisos de la sección 6 (solo si su código no existe) y asignarlos al rol `Gerente` (solo si el rol existe y no los tiene).

**`Down`:** vuelve a crear `Repuestos.CantidadStock` con default 0, le pone la suma de `Cantidad` de todas las sedes y después deshace el esquema. Deja los permisos insertados: borrarlos rompería asignaciones que se hayan hecho a mano.

Todo el SQL de datos usa `migrationBuilder.Sql` con identificadores entre comillas dobles en el esquema `customcore`.

## 4. `SucursalActual`

Clase en `Seguridad/SucursalActual.cs`, registrada con `AddScoped` en `Program.cs` (`AddHttpContextAccessor()` ya está registrado por la auditoría). Depende de `IHttpContextAccessor` y de `AppDbContext`.

En la primera llamada de una petición hace **una sola consulta**:

```csharp
db.Usuarios.AsNoTracking()
    .Where(u => u.IdUsuario == idUsuario)
    .Select(u => new {
        u.SucursalId,
        Todas = u.UsuarioRoles.Any(ur => ur.Rol.RolPermisos.Any(
            rp => rp.Permiso.NombreCodigo == PermisosSistema.SucursalesTodas))
    })
```

El resultado se guarda en un campo para el resto de la petición. El id de usuario se toma del claim `sub`, igual que en `PermisoHandler`.

### `Task<int?> AlcanceAsync(CancellationToken)`, para lecturas

- Con `todas`: devuelve `null`, es decir, sin filtro.
- Con sede y sin `todas`: devuelve la sede.
- Sin sede y sin `todas`: lanza `AccesoDenegadoException` (`403`).

### `Task<int> ResolverAsync(int? solicitada, CancellationToken)`, para crear

- Con `todas`: si viene `solicitada`, la usa; si no, lanza `SolicitudInvalidaException` (`400`, "Indica la sucursal").
- Sin `todas`: sin sede, `403`; si `solicitada` viene y es distinta de su sede, `403`; si no, devuelve su sede.
- En ambos casos, si la sede no existe o está dada de baja, lanza `ConflictoNegocioException` (`409`).

### Excepciones nuevas

En `Excepciones/`: `AccesoDenegadoException` y `SolicitudInvalidaException`, que heredan de `Exception` con solo un constructor que recibe el mensaje. `ManejadorGlobalExcepciones.Traducir` las convierte en `403` y `400` con su mensaje como detalle.

## 5. Cambios por módulo

**Regla general:** un registro de otra sede se comporta como uno inexistente (`404`). El filtro de alcance va en el mismo `Where` que ya busca por id, así que no agrega consultas.

### Órdenes de trabajo (`OrdenTrabajoService`)

- `CrearOrdenRequest` gana el parámetro opcional `int? SucursalId`. `CrearInternoAsync` asigna `SucursalId = await sucursal.ResolverAsync(request.SucursalId)`.
- `ConsultaOrdenes` gana `int? SucursalId`. El listado aplica primero el alcance; el filtro `SucursalId` del request solo se aplica cuando el alcance es `null`.
- `ObtenerPorIdAsync`, `ActualizarAsync`, `CambiarEstadoAsync`, `AgregarLineaAsync` y `QuitarLineaAsync` aplican el alcance en la consulta de existencia (`ExisteYNoEstaFacturadaAsync` recibe el alcance), así que una orden de otra sede responde `404`.
- `OrdenDetalleDto` y `OrdenListaDto` ganan `SucursalId`.
- **Mecánico:** al crear o actualizar con `MecanicoAsignadoId`, el usuario debe tener la misma `SucursalId` que la orden, o se responde `409`.
- **`DescontarStockAsync(sucursalId, …)`:** una consulta trae los `IdInventario` de los repuestos pedidos en esa sede. Si a algún repuesto le falta la fila, se responde `409` por stock insuficiente. Después, por cada repuesto en orden estable de `RepuestoId`, se ejecuta el `UPDATE` condicionado `Cantidad >= cantidad` con `ActualizarAuditadoAsync` sobre `IdInventario`. Si afecta 0 filas, `409`.
- **`QuitarLineaInternoAsync`:** devuelve la cantidad a la fila de inventario (sede de la orden, repuesto), que existe porque el descuento la exigió.
- `AgregarLineaInternoAsync` usa la `SucursalId` de la orden, nunca la del usuario.

### Citas (`CitaService`)

- `CrearCitaRequest` gana `int? SucursalId`, que se resuelve con `ResolverAsync`.
- `ConsultaCitas` gana `int? SucursalId`, con la misma lógica de alcance que las órdenes.
- Detalle, reprogramar y cambiar estado aplican el alcance (`404`).
- `CitaListaDto` y `CitaDetalleDto` ganan `SucursalId`.
- El solapamiento sigue siendo por vehículo, sin cambios.

### Facturas (`FacturaService`) y pagos (`PagoStripeService`)

- `CrearAsync`: la consulta de la orden filtra por alcance (`o.SucursalId`). Si la orden es de otra sede, devuelve `null`, que el controlador ya traduce a `404`.
- `ConsultaFacturas` gana `int? SucursalId`. Listado, detalle y `RegistrarPagoAsync` filtran por `f.OrdenTrabajo.SucursalId`.
- `FacturaListaDto` y `FacturaDetalleDto` ganan `SucursalId`.
- `PagoStripeService.ObtenerFacturaPendienteAsync` aplica el alcance, lo que cubre la sesión de Checkout y el cobro directo. **El webhook no se filtra.**

### Inventario (nuevo: `InventarioService` + `InventarioController`)

Ruta base: `api/sucursales/{sucursalId:int}/inventario`.

- **`GET`** (`inventario.leer`): página de repuestos activos con `Cantidad` en esa sede, con un `LEFT JOIN` en una sola consulta (`0` si no hay fila). Admite `Busqueda` por código o nombre. DTO: `InventarioDto(int RepuestoId, string CodigoStock, string Nombre, decimal PrecioUnitario, int Cantidad)`.
- **`PUT {repuestoId:int}`** (`inventario.ajustar`), con cuerpo `FijarInventarioRequest([Range(0, int.MaxValue)] int Cantidad)`. Dentro de `EjecutarUnidadAuditadaAsync`: si existe la fila, `ActualizarAuditadoAsync`; si no, `Add` y `SaveChangesAsync`, que audita el interceptor. Responde `204`. Si el repuesto no existe, `404`; si está dado de baja, `409`.
- **Acceso:** `AlcanceAsync()` no nulo y distinto de `sucursalId` → `403`, porque aquí la sede viene explícita en la ruta. Si la sede no existe, `404`.

### Repuestos (`RepuestoService`)

- `CantidadStock` sale de `RepuestoDto`, `CrearRepuestoRequest` y `ActualizarRepuestoRequest`. **Rompe el contrato para el frontend.**

### Usuarios (`UsuarioService`)

- `CrearUsuarioRequest` y `ActualizarUsuarioRequest` ganan `int? SucursalId`. Si la sede indicada no existe o está inactiva, `409`.
- `UsuarioResumenDto` gana `int? SucursalId`.

### Sucursales (`SucursalesController`)

- `[Authorize]` en la clase. `GET` usa `sucursales.leer`; `POST`, `PUT` y `DELETE` usan `sucursales.gestionar`.

### Auditoría (`CambioAuditoria.Campos`)

- Se agregan `Sucursal` (`IdSucursal`, `Nombre`, `Direccion`, `Telefono`, `Activo`) e `InventarioSucursal` (`IdInventario`, `SucursalId`, `RepuestoId`, `Cantidad`).
- Se agrega `SucursalId` a `Usuario`, `Cita` y `OrdenTrabajoEncabezado`.
- Se quita `CantidadStock` de `Repuesto`.
- `SucursalService.ActualizarAsync` y `DarDeBajaAsync` pasan a `ActualizarAuditadoAsync`, como el resto de catálogos.

## 6. Permisos

En `PermisosSistema`, como bloque propio:

| Constante | Código | Módulo |
|---|---|---|
| `SucursalesLeer` | `sucursales.leer` | Sucursales |
| `SucursalesGestionar` | `sucursales.gestionar` | Sucursales |
| `SucursalesTodas` | `sucursales.todas` | Sucursales |
| `InventarioLeer` | `inventario.leer` | Inventario |
| `InventarioAjustar` | `inventario.ajustar` | Inventario |

- Las políticas se registran en `ConfiguracionSeguridad` con un `foreach` propio, igual que los bloques existentes.
- `sucursales.todas` no protege ningún endpoint; solo lo consulta `SucursalActual`.
- La migración los asigna únicamente al rol `Gerente`. Al resto de roles se les asignan con `roles.asignar_permisos`.

## 7. Fuera de alcance

- Traslados entre sedes, movimientos de entrada o salida, compras a proveedor.
- Serie fiscal por sede.
- Proyecto de pruebas automatizadas.
- Aplicar la migración en Supabase o desplegar.
- Cambios en el frontend.

## 8. Verificación

1. `dotnet build` sin advertencias nuevas, y `dotnet ef migrations has-pending-model-changes` sin cambios.
2. `dotnet ef database update` contra `pruebas_customdb`. Con SQL se comprueba que `SUM(Cantidad)` por repuesto es igual al `CantidadStock` anterior, que no quedan órdenes ni citas sin sede y que el Gerente tiene los 5 permisos nuevos.
3. `dotnet ef database update AgregarSucursales` y de nuevo a la última, para comprobar que el `Down` conserva el stock total.
4. Con la API local, un script de PowerShell recorre estos escenarios con dos sedes, un Gerente y un empleado de cada sede:
   - Gerente crea una orden sin `SucursalId` → `400`; con `SucursalId` → `201`.
   - Empleado A crea una orden → queda en la sede A. Si pide la sede B → `403`.
   - Empleado A lee, edita o factura una orden de la sede B → `404`.
   - Stock 0 en A y 5 en B; el empleado A pide 1 → `409`.
   - Quitar una línea devuelve el stock a la sede de la orden.
   - Empleado A ajusta el inventario de B → `403`.
   - Listados: el empleado A solo ve A; el Gerente ve todo y filtra con `?sucursalId=`.
   - `GET /api/sucursales` sin token → `401`.

## 9. Entrega

- Commits por bloque en `Local/FeatureSucursales`: modelo y migración; `SucursalActual` y excepciones; inventario y repuestos; órdenes; citas; facturas y pagos; usuarios y sucursales.
- Antes de llevarlo a `Produccion` hay que coordinar con el frontend la salida de `CantidadStock` y el nuevo endpoint de inventario, y avisar a `jgarciad8` de los cambios en `CambioAuditoria` y de `SucursalActual`.
