//[INICIO][27/10/2026][Rodriale][Vuelca cualquier TablaReporte a Excel o PDF; los totales se calculan aquí sobre las columnas de moneda]
using ClosedXML.Excel;
using CustomCore.API.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CustomCore.API.Servicios;

public static class GeneradorReportes
{
    private const string FormatoMoneda = "\"Q\"#,##0.00";
    private const string FormatoFecha = "dd/MM/yyyy HH:mm";
    private const string ColorMarca = "#3E0E09";

    public static ArchivoReporte Generar(TablaReporte tabla, FormatoReporte formato, string nombreBase)
    {
        var sello = DateTime.Now.ToString("yyyyMMdd-HHmm");
        return formato == FormatoReporte.Excel
            ? new ArchivoReporte(
                Excel(tabla),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{nombreBase}-{sello}.xlsx")
            : new ArchivoReporte(Pdf(tabla), "application/pdf", $"{nombreBase}-{sello}.pdf");
    }

    private static decimal?[] Totales(TablaReporte tabla) =>
        tabla.Columnas
            .Select((columna, i) => columna.Tipo == TipoColumna.Moneda
                ? tabla.Filas.Sum(fila => fila[i] as decimal? ?? 0m)
                : (decimal?)null)
            .ToArray();

    private static byte[] Excel(TablaReporte tabla)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Reporte");

        hoja.Cell(1, 1).Value = tabla.Titulo;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        hoja.Cell(2, 1).Value = tabla.Subtitulo;

        const int filaEncabezado = 4;
        for (var c = 0; c < tabla.Columnas.Count; c++)
        {
            var celda = hoja.Cell(filaEncabezado, c + 1);
            celda.Value = tabla.Columnas[c].Titulo;
            celda.Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml(ColorMarca));
        }

        for (var f = 0; f < tabla.Filas.Count; f++)
            for (var c = 0; c < tabla.Columnas.Count; c++)
                Escribir(hoja.Cell(filaEncabezado + 1 + f, c + 1), tabla.Filas[f][c], tabla.Columnas[c].Tipo);

        var totales = Totales(tabla);
        if (totales.Any(t => t is not null))
        {
            var filaTotal = filaEncabezado + tabla.Filas.Count + 1;
            hoja.Cell(filaTotal, 1).Value = "Total";
            for (var c = 0; c < totales.Length; c++)
                if (totales[c] is { } total)
                    Escribir(hoja.Cell(filaTotal, c + 1), total, TipoColumna.Moneda);
            hoja.Row(filaTotal).Style.Font.SetBold();
        }

        if (tabla.Filas.Count > 0)
            hoja.Range(filaEncabezado, 1, filaEncabezado + tabla.Filas.Count, tabla.Columnas.Count).SetAutoFilter();
        hoja.SheetView.FreezeRows(filaEncabezado);
        hoja.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void Escribir(IXLCell celda, object? valor, TipoColumna tipo)
    {
        switch (valor)
        {
            case null:
                return;
            case decimal numero:
                celda.Value = numero;
                celda.Style.NumberFormat.Format = FormatoMoneda;
                return;
            case int entero:
                celda.Value = entero;
                return;
            case DateTimeOffset fecha:
                celda.Value = TimeZoneInfo.ConvertTime(fecha, HorarioTaller.Zona).DateTime;
                celda.Style.DateFormat.Format = FormatoFecha;
                return;
            default:
                celda.Value = valor.ToString();
                return;
        }
    }

    private static string Texto(object? valor) => valor switch
    {
        null => "",
        decimal numero => numero.ToString("Q#,##0.00"),
        DateTimeOffset fecha => TimeZoneInfo.ConvertTime(fecha, HorarioTaller.Zona).ToString(FormatoFecha),
        _ => valor.ToString() ?? ""
    };

    private static byte[] Pdf(TablaReporte tabla)
    {
        var totales = Totales(tabla);
        var conTotales = totales.Any(t => t is not null);

        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(tabla.Columnas.Count > 6 ? PageSizes.Letter.Landscape() : PageSizes.Letter);
            pagina.Margin(30);
            pagina.DefaultTextStyle(estilo => estilo.FontSize(9));

            pagina.Header().PaddingBottom(12).Column(columna =>
            {
                columna.Item().Text("Los Santos Customs").FontSize(10).FontColor(Colors.Grey.Darken1);
                columna.Item().Text(tabla.Titulo).FontSize(16).Bold().FontColor(ColorMarca);
                columna.Item().Text(tabla.Subtitulo).FontColor(Colors.Grey.Darken2);
            });

            pagina.Content().Table(contenido =>
            {
                contenido.ColumnsDefinition(columnas =>
                {
                    foreach (var _ in tabla.Columnas)
                        columnas.RelativeColumn();
                });

                contenido.Header(encabezado =>
                {
                    foreach (var columna in tabla.Columnas)
                        encabezado.Cell().Background(ColorMarca).Padding(4).Text(columna.Titulo).FontColor(Colors.White).Bold();
                });

                foreach (var fila in tabla.Filas)
                    for (var c = 0; c < tabla.Columnas.Count; c++)
                    {
                        var celda = contenido.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
                        var alineada = tabla.Columnas[c].Tipo is TipoColumna.Moneda or TipoColumna.Entero ? celda.AlignRight() : celda;
                        alineada.Text(Texto(fila[c]));
                    }

                if (conTotales)
                    for (var c = 0; c < totales.Length; c++)
                        contenido.Cell().Padding(4).AlignRight()
                            .Text(c == 0 ? "Total" : totales[c] is { } total ? Texto(total) : "").Bold();
            });

            pagina.Footer().AlignCenter().Text(texto =>
            {
                texto.Span("Página ");
                texto.CurrentPageNumber();
                texto.Span(" de ");
                texto.TotalPages();
            });
        })).GeneratePdf();
    }
}
//[FIN][27/10/2026][Rodriale][Vuelca cualquier TablaReporte a Excel o PDF]
