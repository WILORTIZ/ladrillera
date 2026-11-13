using SistemaLadrillera.Domain.Entities;

namespace SistemaLadrillera.Application.Interfaces;

public interface ILadrilleraService
{
    void InitializeDatabase();

    // 1. Dashboard
    DashboardKpis GetDashboardKpis(string? desdeIso = null, string? hastaIso = null);

    // 2. Producción (Secado, Horno, Pérdidas)
    List<ProduccionMovimiento> GetProduccionMovimientos(string? etapa = null);
    void RegistrarEntradaSecado(int productoId, long cantidadUnidades, string responsable, string observaciones);
    void TrasladarSecadoAHorno(int productoId, long cantidadUnidades, string responsable, string observaciones);
    void FinalizarHornoAInventario(int productoId, long cantidadUnidades, string responsable, string observaciones);
    void RegistrarPerdidaProduccion(int productoId, string etapa, long cantidadUnidades, string causa, string responsable, string observaciones);

    // 3. Inventario y Kardex
    List<Producto> GetProductos(bool soloActivos = false);
    Producto? GetProductoById(int id);
    void GuardarProducto(Producto producto);
    void RegistrarAjusteInventario(int productoId, string concepto, long cantidadUnidades, string referencia, string responsable, string observaciones);
    List<InventarioMovimiento> GetKardexMovimientos(int? productoId = null, string? desdeIso = null, string? hastaIso = null);

    // 4. Ventas, Pagos y Devoluciones
    Venta CrearVenta(Venta venta, long abonoInicialCop = 0, string medioPagoInicial = "Efectivo");
    List<Venta> GetVentas(string? filtro = null, string? estadoPago = null, string? desdeIso = null, string? hastaIso = null);
    Venta? GetVentaById(int ventaId);
    void AnularVenta(int ventaId, string motivo, string usuario = "Administrador");
    Pago RegistrarPago(int ventaId, long montoCop, string medioPago, string referencia, string observaciones);
    List<Pago> GetPagos(int? ventaId = null);
    Devolucion RegistrarDevolucion(Devolucion devolucion);
    List<Devolucion> GetDevoluciones(int? ventaId = null);

    // 5. Facturación y PDF
    List<Factura> GetFacturas(string? filtro = null);
    Factura? GetFacturaById(int facturaId);
    string GenerarPdfFactura(int facturaId, string? rutaDestino = null);

    // 6. Cotizaciones
    Cotizacion CrearCotizacion(Cotizacion cotizacion);
    List<Cotizacion> GetCotizaciones(string? estado = null);
    void CambiarEstadoCotizacion(int cotizacionId, string nuevoEstado);
    Venta ConvertirCotizacionEnVenta(int cotizacionId, string placaTransporte, string conductor, long fleteCop, int? vendedorId, long abonoInicialCop = 0);
    string GenerarPdfCotizacion(int cotizacionId, string? rutaDestino = null);

    // 7. Transporte
    List<Vehiculo> GetVehiculos();
    void GuardarVehiculo(Vehiculo vehiculo);
    List<Conductor> GetConductores();
    void GuardarConductor(Conductor conductor);
    List<FleteTransporte> GetFletes();
    void RegistrarFlete(FleteTransporte flete);
    void ActualizarEstadoEntregaVenta(int ventaId, string nuevoEstadoEntrega);

    // 8. Vendedores y Comisiones
    List<Vendedor> GetVendedores(bool soloActivos = false);
    void GuardarVendedor(Vendedor vendedor);
    List<ComisionRegistro> GetComisiones(int? vendedorId = null, string? estado = null);
    void MarcarComisionPagada(int comisionId);

    // 9. Informes y Exportación
    string ExportarInformeCsv(string tipoInforme, string? rutaDestino = null, string? desdeIso = null, string? hastaIso = null);
    string ExportarInformePdf(string tipoInforme, string? rutaDestino = null, string? desdeIso = null, string? hastaIso = null);

    // 10. Configuración, Consecutivos, Copias de Seguridad y Migración Excel
    List<ConfiguracionItem> GetConfiguraciones();
    string GetConfigValor(string parametro, string valorPorDefecto = "");
    void GuardarConfiguracion(string parametro, string valor, string descripcion = "");
    List<ControlConsecutivo> GetConsecutivos();
    void GuardarConsecutivo(ControlConsecutivo consecutivo);
    string PeekSiguienteConsecutivo(string tipo);
    string CrearCopiaSeguridad(string? carpetaDestino = null);
    void RestaurarCopiaSeguridad(string rutaBackup);
    bool VerificarIntegridadBaseDatos(out string mensaje);
    ResultadoMigracionExcel ImportarDesdeExcel(string rutaArchivoExcel);
}

