using System.Globalization;
using System.Text;
using SistemaLadrillera.Domain.Entities;

namespace SistemaLadrillera.Infrastructure.Services;

public static class PdfDocumentGenerator
{
    public static string GenerarFacturaPdf(Factura factura, Venta? venta, string nombreEmpresa, string nitEmpresa, string direccionEmpresa, string telefonoEmpresa, string rutaArchivo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaArchivo)!);
        var lines = new List<(string Text, int Size, bool Bold)>();

        lines.Add((nombreEmpresa.ToUpperInvariant(), 15, true));
        lines.Add(($"NIT: {nitEmpresa}  |  Tel: {telefonoEmpresa}  |  Dir: {direccionEmpresa}", 9, false));
        lines.Add(("================================================================================", 9, false));
        lines.Add(($"COMPROBANTE / FACTURA INTERNA NRO: {factura.FacturaCodigo}", 13, true));
        lines.Add(($"Fecha Emision: {factura.FechaIso}      Estado: {factura.Estado}", 10, false));
        lines.Add(("--------------------------------------------------------------------------------", 9, false));
        lines.Add(($"Cliente:   {factura.ClienteNombre}", 10, true));
        lines.Add(($"Telefono:  {factura.Telefono}      Direccion: {factura.Direccion}", 10, false));
        lines.Add(($"Vendedor:  {factura.VendedorNombre}", 10, false));
        if (venta != null && !string.IsNullOrWhiteSpace(venta.PlacaTransporte))
        {
            lines.Add(($"Transporte Placa: {venta.PlacaTransporte}   |   Conductor: {venta.ConductorNombre}", 10, false));
        }
        lines.Add(("--------------------------------------------------------------------------------", 9, false));
        lines.Add(("PRODUCTO               CANTIDAD     UNIDAD      PRECIO UNIT     DESC%        TOTAL COP", 9, true));
        lines.Add(("--------------------------------------------------------------------------------", 9, false));

        foreach (var d in factura.Detalles)
        {
            string prod = PadRightSafe(d.ProductoNombre, 21);
            string cant = d.Cantidad.ToString("N0", CultureInfo.InvariantCulture).PadLeft(9);
            string unid = PadRightSafe(d.Unidad, 10);
            string prec = ("$" + d.PrecioUnitarioCop.ToString("N0", CultureInfo.InvariantCulture)).PadLeft(12);
            string desc = (d.DescuentoPct.ToString("0.##") + "%").PadLeft(7);
            string tot = ("$" + d.TotalLineaCop.ToString("N0", CultureInfo.InvariantCulture)).PadLeft(14);
            lines.Add(($"{prod} {cant}   {unid} {prec}  {desc} {tot}", 9, false));
        }

        lines.Add(("--------------------------------------------------------------------------------", 9, false));
        lines.Add(($"SUBTOTAL PRODUCTOS:   ${factura.SubtotalCop:N0} COP", 10, false));
        lines.Add(($"DESCUENTO APLICADO:   ${factura.DescuentoCop:N0} COP", 10, false));
        lines.Add(($"FLETE EN FACTURA:     ${factura.FleteCop:N0} COP", 10, false));
        lines.Add(($"TOTAL A PAGAR:        ${factura.TotalCop:N0} COP", 12, true));

        if (venta != null)
        {
            lines.Add(($" Estado de Pago: {venta.EstadoPago}  |  Abonado: ${venta.MontoPagadoCop:N0} COP  |  Saldo Pendiente: ${venta.SaldoClienteCop:N0} COP", 10, false));
        }

        if (!string.IsNullOrWhiteSpace(factura.Observaciones))
        {
            lines.Add(($"Observaciones: {factura.Observaciones}", 9, false));
        }

        lines.Add(("================================================================================", 9, false));
        lines.Add(("NOTA LEGAL IMPORTANTE: Este documento es un comprobante interno de control", 8, true));
        lines.Add(("operativo de Sistema Ladrillera (100% Offline). No constituye ni sustituye", 8, false));
        lines.Add(("los requisitos legales de Facturacion Electronica de Venta ante la DIAN en Colombia.", 8, false));

        WriteSimplePdf(rutaArchivo, lines);
        return rutaArchivo;
    }

    public static string GenerarCotizacionPdf(Cotizacion cot, string nombreEmpresa, string nitEmpresa, string telefonoEmpresa, string rutaArchivo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaArchivo)!);
        var lines = new List<(string Text, int Size, bool Bold)>();

        lines.Add((nombreEmpresa.ToUpperInvariant(), 15, true));
        lines.Add(($"NIT: {nitEmpresa}  |  Tel: {telefonoEmpresa}", 9, false));
        lines.Add(("================================================================================", 9, false));
        lines.Add(($"COTIZACION COMERCIAL NRO: {cot.CotizacionCodigo}", 13, true));
        lines.Add(($"Fecha: {cot.FechaIso}   |   Vigente Hasta: {cot.VigenteHastaIso}   |   Estado: {cot.Estado}", 10, false));
        lines.Add(("--------------------------------------------------------------------------------", 9, false));
        lines.Add(($"Cliente:  {cot.ClienteNombre}   |   Telefono: {cot.Telefono}", 10, true));
        lines.Add(("--------------------------------------------------------------------------------", 9, false));
        lines.Add(("PRODUCTO               CANTIDAD     UNIDAD      PRECIO UNIT     DESC%        TOTAL COP", 9, true));
        lines.Add(("--------------------------------------------------------------------------------", 9, false));

        foreach (var d in cot.Detalles)
        {
            string prod = PadRightSafe(d.ProductoNombre, 21);
            string cant = d.Cantidad.ToString("N0", CultureInfo.InvariantCulture).PadLeft(9);
            string unid = PadRightSafe(d.Unidad, 10);
            string prec = ("$" + d.PrecioUnitarioCop.ToString("N0", CultureInfo.InvariantCulture)).PadLeft(12);
            string desc = (d.DescuentoPct.ToString("0.##") + "%").PadLeft(7);
            string tot = ("$" + d.TotalLineaCop.ToString("N0", CultureInfo.InvariantCulture)).PadLeft(14);
            lines.Add(($"{prod} {cant}   {unid} {prec}  {desc} {tot}", 9, false));
        }

        lines.Add(("--------------------------------------------------------------------------------", 9, false));
        lines.Add(($"TOTAL COTIZADO:       ${cot.TotalCop:N0} COP", 12, true));
        if (!string.IsNullOrWhiteSpace(cot.Observaciones))
        {
            lines.Add(($"Observaciones: {cot.Observaciones}", 9, false));
        }
        lines.Add(("================================================================================", 9, false));
        lines.Add(("Precios sujetos a vigencia indicada y disponibilidad en planta.", 8, false));

        WriteSimplePdf(rutaArchivo, lines);
        return rutaArchivo;
    }

    public static string GenerarInformePdf(string titulo, List<string> encabezadosYLineas, string rutaArchivo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaArchivo)!);
        var lines = new List<(string Text, int Size, bool Bold)>
        {
            ("SISTEMA LADRILLERA - INFORME OFFLINE", 14, true),
            (titulo, 11, true),
            ($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", 9, false),
            ("================================================================================", 9, false)
        };

        foreach (var l in encabezadosYLineas)
        {
            lines.Add((l, 9, false));
        }

        WriteSimplePdf(rutaArchivo, lines);
        return rutaArchivo;
    }

    private static string PadRightSafe(string s, int len)
    {
        s ??= string.Empty;
        return s.Length > len ? s.Substring(0, len) : s.PadRight(len);
    }

    private static string SanitizePdfText(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            char mapped = c switch
            {
                'á' => 'a', 'é' => 'e', 'í' => 'i', 'ó' => 'o', 'ú' => 'u',
                'Á' => 'A', 'É' => 'E', 'Í' => 'I', 'Ó' => 'O', 'Ú' => 'U',
                'ñ' => 'n', 'Ñ' => 'N', 'ü' => 'u', 'Ü' => 'U',
                _ => c
            };
            if (mapped == '(' || mapped == ')' || mapped == '\\')
            {
                sb.Append('\\');
            }
            if (mapped >= 32 && mapped <= 126)
            {
                sb.Append(mapped);
            }
            else
            {
                sb.Append(' ');
            }
        }
        return sb.ToString();
    }

    private static void WriteSimplePdf(string path, List<(string Text, int Size, bool Bold)> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        int y = 790;
        foreach (var (text, size, bold) in lines)
        {
            string font = bold ? "/F2" : "/F1";
            string safe = SanitizePdfText(text);
            content.AppendLine($"{font} {size} Tf");
            content.AppendLine($"1 0 0 1 40 {y} Tm");
            content.AppendLine($"({safe}) Tj");
            y -= (size + 6);
            if (y < 40) break;
        }
        content.AppendLine("ET");

        byte[] streamBytes = Encoding.ASCII.GetBytes(content.ToString());

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs, Encoding.ASCII);

        void WriteStr(string s) => bw.Write(Encoding.ASCII.GetBytes(s));

        WriteStr("%PDF-1.4\n");
        var offsets = new List<long>();

        // 1: Catalog
        offsets.Add(fs.Position);
        WriteStr("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        // 2: Pages
        offsets.Add(fs.Position);
        WriteStr("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        // 3: Page
        offsets.Add(fs.Position);
        WriteStr("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>\nendobj\n");

        // 4: Font Courier
        offsets.Add(fs.Position);
        WriteStr("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>\nendobj\n");

        // 5: Font Courier-Bold
        offsets.Add(fs.Position);
        WriteStr("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold >>\nendobj\n");

        // 6: Contents
        offsets.Add(fs.Position);
        WriteStr($"6 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
        bw.Write(streamBytes);
        WriteStr("\nendstream\nendobj\n");

        long xrefPos = fs.Position;
        WriteStr($"xref\n0 {offsets.Count + 1}\n");
        WriteStr("0000000000 65535 f \n");
        foreach (var off in offsets)
        {
            WriteStr($"{off:D10} 00000 n \n");
        }
        WriteStr($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF\n");
    }
}

