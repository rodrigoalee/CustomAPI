//[INICIO][23/10/2026][Rodriale][Consultas del portal: el alcance sale del ClienteId de la cuenta, no de permisos ni de sucursal]
using CustomCore.API.Data;
using CustomCore.API.Dtos;
using CustomCore.API.Entidades;
using System.Text.RegularExpressions;
using CustomCore.API.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace CustomCore.API.Servicios;

public sealed class PortalService(AppDbContext db, NotificacionWhatsAppService whatsApp)
{
    private const int MaxDiasDisponibilidad = 62;

    public async Task<IReadOnlyList<CitaPortalDto>> ObtenerCitasAsync(
        int idUsuario,
        CancellationToken cancellationToken)
    {
        var clienteId = await ObtenerClienteIdAsync(idUsuario, cancellationToken);

        return await db.Citas
            .AsNoTracking()
            .Where(c => c.Vehiculo.ClienteId == clienteId)
            .OrderByDescending(c => c.FechaHora)
            .Select(c => new CitaPortalDto(
                c.IdCita,
                c.Servicio != null ? c.Servicio.Nombre : null,
                c.Motivo,
                c.Vehiculo.Marca,
                c.Vehiculo.Modelo,
                c.Vehiculo.Anio,
                c.Vehiculo.Placa,
                c.FechaHora,
                c.Estado))
            .ToListAsync(cancellationToken);
    }

    //[INICIO][24/10/2026][Rodriale][Una cita de otro cliente se comporta como inexistente]
    public async Task<SeguimientoCitaDto?> ObtenerSeguimientoAsync(
        int idUsuario,
        int idCita,
        CancellationToken cancellationToken)
    {
        var clienteId = await ObtenerClienteIdAsync(idUsuario, cancellationToken);

        return await db.Citas
            .AsNoTracking()
            .Where(c => c.IdCita == idCita && c.Vehiculo.ClienteId == clienteId)
            .Select(c => new
            {
                Cita = c,
                Sucursal = db.Sucursales
                    .Where(s => s.IdSucursal == c.SucursalId)
                    .Select(s => s.Nombre)
                    .FirstOrDefault(),
                Orden = db.OrdenesTrabajoEncabezado
                    .Where(o => o.CitaId == c.IdCita)
                    .Select(o => new
                    {
                        o.Estado,
                        o.FechaIngreso,
                        o.FechaFinalizacion,
                        Pagado = db.FacturasEncabezado.Any(f =>
                            f.OrdenTrabajoId == o.IdOrdenEncabezado && f.EstadoPago == EstadoPago.Pagado)
                    })
                    .FirstOrDefault()
            })
            .Select(x => new SeguimientoCitaDto(
                x.Cita.IdCita,
                x.Cita.Servicio != null ? x.Cita.Servicio.Nombre : null,
                x.Cita.Motivo,
                x.Cita.Vehiculo.Marca,
                x.Cita.Vehiculo.Modelo,
                x.Cita.Vehiculo.Anio,
                x.Cita.Vehiculo.Placa,
                x.Sucursal ?? "",
                x.Cita.FechaHora,
                x.Cita.Estado,
                x.Orden != null ? x.Orden.Estado : null,
                x.Orden != null ? x.Orden.FechaIngreso : null,
                x.Orden != null ? x.Orden.FechaFinalizacion : null,
                x.Orden != null && x.Orden.Pagado))
            .FirstOrDefaultAsync(cancellationToken);
    }
    //[FIN][24/10/2026][Rodriale][Una cita de otro cliente se comporta como inexistente]

    public async Task<PerfilPortalDto> ObtenerPerfilAsync(
        int idUsuario,
        CancellationToken cancellationToken)
    {
        var clienteId = await ObtenerClienteIdAsync(idUsuario, cancellationToken);

        return await db.Clientes
            .AsNoTracking()
            .Where(c => c.IdCliente == clienteId)
            .Select(c => new PerfilPortalDto(c.NombreCompleto, c.Correo, c.Telefono))
            .SingleAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VehiculoPortalDto>> ObtenerVehiculosAsync(
        int idUsuario,
        CancellationToken cancellationToken)
    {
        var clienteId = await ObtenerClienteIdAsync(idUsuario, cancellationToken);

        return await db.Vehiculos
            .AsNoTracking()
            .Where(v => v.ClienteId == clienteId)
            .OrderBy(v => v.Marca)
            .ThenBy(v => v.Modelo)
            .Select(v => new VehiculoPortalDto(v.IdVehiculo, v.Marca, v.Modelo, v.Anio, v.Placa))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RegistrarVehiculoAsync(
        int idUsuario,
        RegistrarVehiculoRequest request,
        CancellationToken cancellationToken)
    {
        var clienteId = await ObtenerClienteIdAsync(idUsuario, cancellationToken);
        var placa = NormalizarPlaca(request.Placa);
        var placaSinSeparadores = placa.Replace("-", "");

        //[INICIO][23/10/2026][Rodriale][Placa ajena: no se revela de quién es ni se le pasa al que la escribió; la vinculación la hace el taller]
        //[INICIO][24/10/2026][Rodriale][Se compara sin guiones ni espacios: P678KDD y P-678KDD son la misma placa]
        if (await db.Vehiculos.AsNoTracking()
                .AnyAsync(v => v.Placa.ToUpper().Replace("-", "").Replace(" ", "") == placaSinSeparadores, cancellationToken))
        //[FIN][24/10/2026][Rodriale][Se compara sin guiones ni espacios]
        {
            throw new ConflictoNegocioException(
                "Esa placa ya está registrada en el taller. Comunícate con nosotros.");
        }
        //[FIN][23/10/2026][Rodriale][Placa ajena]

        var vehiculo = new Vehiculo
        {
            ClienteId = clienteId,
            Placa = placa,
            Marca = request.Marca.Trim(),
            Modelo = request.Modelo.Trim(),
            Anio = request.Anio
        };

        db.Vehiculos.Add(vehiculo);
        await db.SaveChangesAsync(cancellationToken);

        return vehiculo.IdVehiculo;
    }

    public async Task<IReadOnlyList<SucursalPortalDto>> ObtenerSucursalesAsync(
        CancellationToken cancellationToken) =>
        await db.Sucursales
            .AsNoTracking()
            .Where(s => s.Activo)
            .OrderBy(s => s.Nombre)
            .Select(s => new SucursalPortalDto(s.IdSucursal, s.Nombre, s.Direccion))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServicioPortalDto>> ObtenerServiciosAsync(
        CancellationToken cancellationToken) =>
        await db.Servicios
            .AsNoTracking()
            .Where(s => s.Activo)
            .OrderBy(s => s.Nombre)
            .Select(s => new ServicioPortalDto(s.IdServicio, s.Nombre, s.TarifaManoObra))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DiaDisponibilidadDto>> ObtenerDisponibilidadAsync(
        ConsultaDisponibilidad consulta,
        CancellationToken cancellationToken)
    {
        var dias = consulta.Hasta.DayNumber - consulta.Desde.DayNumber + 1;
        if (dias < 1 || dias > MaxDiasDisponibilidad)
            throw new SolicitudInvalidaException(
                $"El rango de fechas debe ser de 1 a {MaxDiasDisponibilidad} días.");

        if (!await db.Sucursales.AsNoTracking()
                .AnyAsync(s => s.IdSucursal == consulta.SucursalId && s.Activo, cancellationToken))
            throw new SolicitudInvalidaException("La sede no existe o no está disponible.");

        var citas = await CitasActivasDeSedeAsync(
            consulta.SucursalId,
            HorarioTaller.InicioDelDia(consulta.Desde),
            HorarioTaller.InicioDelDia(consulta.Hasta.AddDays(1)),
            cancellationToken);

        var ahora = DateTimeOffset.UtcNow;

        return Enumerable.Range(0, dias)
            .Select(i => consulta.Desde.AddDays(i))
            .Select(fecha => new DiaDisponibilidadDto(
                fecha,
                HorarioTaller.BloquesDelDia(fecha)
                    .Select(bloque =>
                    {
                        var lleno = ContarCruces(citas, bloque) >= HorarioTaller.CapacidadPorBloque;
                        return new HorarioDisponibleDto(
                            bloque,
                            !lleno && HorarioTaller.DentroDeVentana(bloque, ahora),
                            lleno);
                    })
                    .ToList()))
            .ToList();
    }

    public async Task<int?> AgendarCitaAsync(
        int idUsuario,
        AgendarCitaRequest request,
        CancellationToken cancellationToken)
    {
        var clienteId = await ObtenerClienteIdAsync(idUsuario, cancellationToken);

        if (!await db.Vehiculos.AsNoTracking()
                .AnyAsync(v => v.IdVehiculo == request.VehiculoId && v.ClienteId == clienteId, cancellationToken))
            return null;

        if (!await db.Servicios.AsNoTracking()
                .AnyAsync(s => s.IdServicio == request.ServicioId && s.Activo, cancellationToken))
            throw new ConflictoNegocioException("El servicio no existe o ya no está disponible.");

        if (!HorarioTaller.EsInicioDeBloque(request.FechaHora)
            || !HorarioTaller.DentroDeVentana(request.FechaHora, DateTimeOffset.UtcNow))
            throw new ConflictoNegocioException("Elige un horario disponible del calendario.");

        var inicio = request.FechaHora.ToUniversalTime();
        var fin = inicio.AddMinutes(HorarioTaller.DuracionMinutos);

        //[INICIO][23/10/2026][Rodriale][La fila de la sede bloqueada serializa las reservas de esa sede: dos clientes no pueden tomar el último lugar a la vez]
        var idCita = await db.EjecutarUnidadAuditadaAsync<int?>(async () =>
        {
            var sede = await db.BloquearFilaAsync<Sucursal>(request.SucursalId, cancellationToken);
            if (sede is not { Activo: true })
                throw new ConflictoNegocioException("La sede no existe o no está disponible.");

            var cruceDelVehiculo = await db.Citas.AsNoTracking()
                .Where(c => c.VehiculoId == request.VehiculoId
                    && c.Estado != EstadoCita.Cancelada
                    && c.FechaHora >= inicio.AddDays(-1)
                    && c.FechaHora < fin)
                .Select(c => new { c.FechaHora, c.DuracionMinutos })
                .ToListAsync(cancellationToken);

            if (cruceDelVehiculo.Any(c => inicio < c.FechaHora.AddMinutes(c.DuracionMinutos)))
                throw new ConflictoNegocioException("Tu vehículo ya tiene una cita en ese horario.");

            var citasDeSede = await CitasActivasDeSedeAsync(request.SucursalId, inicio, fin, cancellationToken);
            if (ContarCruces(citasDeSede, inicio) >= HorarioTaller.CapacidadPorBloque)
                throw new ConflictoNegocioException("Ese horario se acaba de llenar. Elige otro.");

            var cita = new Cita
            {
                VehiculoId = request.VehiculoId,
                SucursalId = request.SucursalId,
                ServicioId = request.ServicioId,
                FechaHora = inicio,
                DuracionMinutos = HorarioTaller.DuracionMinutos,
                Motivo = null,
                Estado = EstadoCita.Pendiente
            };

            db.Citas.Add(cita);
            await db.SaveChangesAsync(cancellationToken);

            return cita.IdCita;
        }, cancellationToken);
        //[FIN][23/10/2026][Rodriale][La fila de la sede bloqueada serializa las reservas de esa sede]

        //[INICIO][25/10/2026][Rodriale][El aviso sale después de la transacción: solo se avisa de citas que sí quedaron guardadas]
        if (idCita is { } id)
            await whatsApp.CitaAgendadaAsync(id);

        return idCita;
        //[FIN][25/10/2026][Rodriale][El aviso sale después de la transacción]
    }

    //[INICIO][23/10/2026][Rodriale][Se trae un día de margen a cada lado: una cita larga que empezó antes del rango también ocupa bloques dentro de él]
    private async Task<List<(DateTimeOffset Inicio, int Minutos)>> CitasActivasDeSedeAsync(
        int sucursalId,
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken cancellationToken)
    {
        var desdeUtc = desde.AddDays(-1).ToUniversalTime();
        var hastaUtc = hasta.AddDays(1).ToUniversalTime();

        var citas = await db.Citas
            .AsNoTracking()
            .Where(c => c.SucursalId == sucursalId
                && c.Estado != EstadoCita.Cancelada
                && c.FechaHora >= desdeUtc
                && c.FechaHora <= hastaUtc)
            .Select(c => new { c.FechaHora, c.DuracionMinutos })
            .ToListAsync(cancellationToken);

        return citas.Select(c => (c.FechaHora, c.DuracionMinutos)).ToList();
    }
    //[FIN][23/10/2026][Rodriale][Se trae un día de margen a cada lado]

    //[INICIO][24/10/2026][Rodriale][Placa guatemalteca con guion tras el prefijo (P678KDD → P-678KDD); si no sigue el formato se guarda como la escribió]
    private static string NormalizarPlaca(string placa)
    {
        var limpia = placa.Trim().ToUpperInvariant();
        var partes = Regex.Match(Regex.Replace(limpia, @"[\s-]", ""), @"^([A-Z]{1,3})(\d{3}[A-Z]{3})$");
        return partes.Success ? $"{partes.Groups[1].Value}-{partes.Groups[2].Value}" : limpia;
    }
    //[FIN][24/10/2026][Rodriale][Placa guatemalteca con guion tras el prefijo]

    private static int ContarCruces(List<(DateTimeOffset Inicio, int Minutos)> citas, DateTimeOffset bloque)
    {
        var fin = bloque.AddMinutes(HorarioTaller.DuracionMinutos);
        return citas.Count(c => c.Inicio < fin && bloque < c.Inicio.AddMinutes(c.Minutos));
    }

    //[INICIO][23/10/2026][Rodriale][Una cuenta de empleado no tiene ficha de cliente: el portal no le muestra nada en lugar de mostrarle todo]
    private async Task<int> ObtenerClienteIdAsync(int idUsuario, CancellationToken cancellationToken)
    {
        var clienteId = await db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario)
            .Select(u => u.ClienteId)
            .SingleOrDefaultAsync(cancellationToken);

        return clienteId
            ?? throw new AccesoDenegadoException("Esta cuenta no pertenece a un cliente del taller.");
    }
    //[FIN][23/10/2026][Rodriale][Una cuenta de empleado no tiene ficha de cliente]
}
//[FIN][23/10/2026][Rodriale][Consultas del portal]
