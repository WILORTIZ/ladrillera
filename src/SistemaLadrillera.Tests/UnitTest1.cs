using SistemaLadrillera.Domain.Entities;
using SistemaLadrillera.Infrastructure.Services;
using Xunit;

namespace SistemaLadrillera.Tests;

public class LadrilleraAcceptanceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteLadrilleraService _service;

    public LadrilleraAcceptanceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"LadrilleraTest_{Guid.NewGuid():N}.db");
        _service = new SqliteLadrilleraService(_tempDbPath);
        _service.InitializeDatabase();
    }

    [Fact]
    public void Criterio1_2_InicializaSemillaExcel_Y_FlujoProduccionSecadoHornoInventario()
    {
        var productos = _service.GetProductos();
        Assert.Equal(4, productos.Count);

        var farol = productos.First(p => p.Nombre == "Farol");
        Assert.Equal(500, farol.StockActual);
        Assert.Equal(1000, farol.StockSecado);
        Assert.Equal(2000, farol.StockHorno);

        // Ingresar 500 a Secado -> 1500
        _service.RegistrarEntradaSecado(farol.Id, 500, "OperadorTest", "Lote mañana");
        // Trasladar 400 a Horno -> Secado 1100, Horno 2400
        _service.TrasladarSecadoAHorno(farol.Id, 400, "OperadorTest", "Paso a quema");
        // Pérdida en Horno de 50 -> Horno 2350, Dañados 50
        _service.RegistrarPerdidaProduccion(farol.Id, "Horno", 50, "Rotura térmica", "OperadorTest", "");
        // Salida de Horno a Inventario de 350 -> Horno 2000, Inventario 500 + 350 = 850
        _service.FinalizarHornoAInventario(farol.Id, 350, "OperadorTest", "Cocción lista");

        var actualizado = _service.GetProductoById(farol.Id)!;
        Assert.Equal(1100, actualizado.StockSecado);
        Assert.Equal(2000, actualizado.StockHorno);
        Assert.Equal(850, actualizado.StockActual);
        Assert.Equal(50, actualizado.DanadosAcumulados);
    }

    [Fact]
    public void Criterio3_6_7_VentaDescuentaInventarioUnaSolaVez_CalculaComisionFleteYSaldo_Y_ConsecutivosUnicos()
    {
        var bloquelon = _service.GetProductos().First(p => p.Nombre == "Bloquelon");
        long stockInicial = bloquelon.StockActual; // 1000 unidades
        var juan = _service.GetVendedores().First(v => v.Codigo == "V001"); // 2%

        // Venta de 200 unidades a $1000 c/u con 10% descuento = $180,000 COP, flete $20,000 interno, abono $80,000
        var v1 = _service.CrearVenta(new Venta
        {
            ClienteNombre = "Constructora Los Andes",
            Telefono = "3151112233",
            VendedorId = juan.Id,
            PlacaTransporte = "KLM-456",
            FleteAsignadoCop = 20000,
            FleteIncluidoEnFactura = false,
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = bloquelon.Id,
                    Cantidad = 200,
                    Unidad = "Unidad",
                    PrecioPorUnidadMedidaCop = 1000,
                    DescuentoPct = 10m
                }
            }
        }, abonoInicialCop: 80000);

        var bloquelonPost = _service.GetProductoById(bloquelon.Id)!;
        Assert.Equal(stockInicial - 200, bloquelonPost.StockActual);
        Assert.Equal(180000, v1.TotalVentaCop);
        Assert.Equal(100000, v1.SaldoClienteCop);
        Assert.Equal("Parcial", v1.EstadoPago);
        Assert.Equal(3600, v1.ComisionCop); // 2% de 180,000
        Assert.Equal(156400, v1.GananciaNetaCop); // 180,000 - 3,600 - 20,000

        // Registrar pago del saldo restante
        var pago2 = _service.RegistrarPago(v1.Id, 100000, "Transferencia", "TRX-99", "Pago final");
        Assert.Equal(0, pago2.SaldoNuevoCop);
        var v1Pagada = _service.GetVentaById(v1.Id)!;
        Assert.Equal("Pagado", v1Pagada.EstadoPago);

        // Segunda venta verifica consecutivo único distinto
        var v2 = _service.CrearVenta(new Venta
        {
            ClienteNombre = "Obra Norte",
            Detalles = new List<VentaDetalle>
            {
                new() { ProductoId = bloquelon.Id, Cantidad = 50, Unidad = "Unidad", PrecioPorUnidadMedidaCop = 1000 }
            }
        });
        Assert.NotEqual(v1.FacturaId, v2.FacturaId);

        // Generar PDF de factura y verificar archivo en disco
        var factura = _service.GetFacturas(v1.FacturaId).Single();
        string pdfPath = _service.GenerarPdfFactura(factura.Id);
        Assert.True(File.Exists(pdfPath));
    }

    [Fact]
    public void Criterio4_DevolucionSoloSumaInventarioSiProductoEsApto()
    {
        var maciso = _service.GetProductos().First(p => p.Nombre == "Maciso"); // 300 unid
        var venta = _service.CrearVenta(new Venta
        {
            ClienteNombre = "Cliente Devolucion",
            Detalles = new List<VentaDetalle>
            {
                new() { ProductoId = maciso.Id, Cantidad = 100, Unidad = "Unidad", PrecioPorUnidadMedidaCop = 300 }
            }
        });

        Assert.Equal(200, _service.GetProductoById(maciso.Id)!.StockActual);

        // 1. Devolución de 30 unidades DAÑADAS -> NO debe sumar al stock disponible
        _service.RegistrarDevolucion(new Devolucion
        {
            VentaId = venta.Id,
            ProductoId = maciso.Id,
            CantidadUnidades = 30,
            EstadoProducto = "Dañado",
            AprobadaParaReingreso = true,
            Motivo = "Rotura en descargue"
        });
        Assert.Equal(200, _service.GetProductoById(maciso.Id)!.StockActual);

        // 2. Devolución de 20 unidades APTAS -> SÍ debe sumar al stock disponible (200 + 20 = 220)
        _service.RegistrarDevolucion(new Devolucion
        {
            VentaId = venta.Id,
            ProductoId = maciso.Id,
            CantidadUnidades = 20,
            EstadoProducto = "Apto",
            AprobadaParaReingreso = true,
            Motivo = "Sobrante limpio"
        });
        Assert.Equal(220, _service.GetProductoById(maciso.Id)!.StockActual);
    }

    [Fact]
    public void Criterio5_CotizacionConvertidaNoCreaVentasDuplicadas()
    {
        var bloque = _service.GetProductos().First(p => p.Nombre == "Bloque cemento"); // 1500 unid
        var cot = _service.CrearCotizacion(new Cotizacion
        {
            ClienteNombre = "Ferretería Central",
            Estado = "Aceptada",
            Detalles = new List<CotizacionDetalle>
            {
                new() { ProductoId = bloque.Id, Cantidad = 1, Unidad = "Millar", PrecioUnitarioCop = 1500000 }
            }
        });

        var venta = _service.ConvertirCotizacionEnVenta(cot.Id, "TRK-001", "Pedro", 50000, null);
        Assert.Equal(500, _service.GetProductoById(bloque.Id)!.StockActual); // 1500 - 1000 (1 Millar)

        // Intentar convertir la misma cotización por segunda vez debe lanzar excepción y no duplicar
        Assert.Throws<InvalidOperationException>(() =>
            _service.ConvertirCotizacionEnVenta(cot.Id, "TRK-001", "Pedro", 50000, null));
    }

    [Fact]
    public void Criterio8_9_BackupRestauracionYMigracionExcelIdempotente()
    {
        string backupPath = _service.CrearCopiaSeguridad(Path.GetTempPath());
        Assert.True(File.Exists(backupPath));
        Assert.True(_service.VerificarIntegridadBaseDatos(out string msg));
        Assert.Equal("ok", msg, ignoreCase: true);

        string excelPath = @"D:\antigravity\ladrillera\Sistema_Ladrillera.xlsm";
        if (File.Exists(excelPath))
        {
            var r1 = _service.ImportarDesdeExcel(excelPath);
            Assert.Empty(r1.Errores);
            int countProds1 = _service.GetProductos().Count;

            // Segunda ejecución idempotente: no duplica productos
            var r2 = _service.ImportarDesdeExcel(excelPath);
            int countProds2 = _service.GetProductos().Count;
            Assert.Equal(countProds1, countProds2);
        }
    }

    public void Dispose()
    {
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
        }
        catch
        {
            // Ignorar bloqueo temporal en limpieza
        }
    }
}
