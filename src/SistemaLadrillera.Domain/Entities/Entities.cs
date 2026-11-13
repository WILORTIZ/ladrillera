namespace SistemaLadrillera.Domain.Entities;

public class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public long StockActual { get; set; }
    public long StockSecado { get; set; }
    public long StockHorno { get; set; }
    public long EntradasAcumuladas { get; set; }
    public long VentasAcumuladas { get; set; }
    public long DanadosAcumulados { get; set; }
    public long CostoUnitarioCop { get; set; }
    public long PrecioUnitarioCop { get; set; }
    public long PrecioMillarCop { get; set; }
    public bool Activo { get; set; } = true;
    public string FechaActualizacionIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}

public class Cliente
{
    public int Id { get; set; }
    public string Documento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public long SaldoPendienteCop { get; set; }
    public bool Activo { get; set; } = true;
}

public class Vendedor
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal ComisionPct { get; set; }
    public string Estado { get; set; } = "Activo";
    public string FechaIngresoIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string? FechaSalidaIso { get; set; }
}

public class ProduccionMovimiento
{
    public int Id { get; set; }
    public string FechaIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    /// <summary>
    /// EntradaSecado | TrasladoAHorno | SalidaHornoAInventario | PerdidaSecado | PerdidaHorno
    /// </summary>
    public string TipoMovimiento { get; set; } = string.Empty;
    /// <summary>
    /// Secado | Horno | Inventario
    /// </summary>
    public string Etapa { get; set; } = string.Empty;
    public long CantidadUnidades { get; set; }
    public string CausaPerdida { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public bool Anulado { get; set; }
}

public class InventarioMovimiento
{
    public int Id { get; set; }
    public string FechaIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    /// <summary>
    /// SaldoInicial | EntradaProduccion | EntradaManual | AjustePositivo | AjusteNegativo | SalidaVenta | AnulacionVenta | PerdidaRotura | ReingresoDevolucion
    /// </summary>
    public string Concepto { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public long CantidadEntrada { get; set; }
    public long CantidadSalida { get; set; }
    public long SaldoPosterior { get; set; }
    public long CostoUnitarioCop { get; set; }
    public string Responsable { get; set; } = "Sistema";
    public string Observaciones { get; set; } = string.Empty;
}

public class Venta
{
    public int Id { get; set; }
    public string FechaIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string Hora { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string FacturaId { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    /// <summary>
    /// Resumen textual para compatibilidad directa con la hoja Ventas de 26 columnas
    /// </summary>
    public string TipoLadrilloResumen { get; set; } = string.Empty;
    public long CantidadResumen { get; set; }
    public string UnidadResumen { get; set; } = "Unidad";
    public long PrecioUnitarioResumenCop { get; set; }
    public decimal DescuentoPctResumen { get; set; }
    public long SubtotalCop { get; set; }
    public long DescuentoCop { get; set; }
    public long TotalVentaCop { get; set; }
    public string PlacaTransporte { get; set; } = string.Empty;
    public string ConductorNombre { get; set; } = string.Empty;
    public long FleteAsignadoCop { get; set; }
    public bool FleteIncluidoEnFactura { get; set; }
    public string EstadoPago { get; set; } = "Pendiente";
    public long MontoPagadoCop { get; set; }
    public long SaldoClienteCop { get; set; }
    public string EstadoEntrega { get; set; } = "Pendiente";
    public int? VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public decimal ComisionPct { get; set; }
    public long ComisionCop { get; set; }
    public long GananciaNetaCop { get; set; }
    public string MotivoDevolucion { get; set; } = string.Empty;
    public string RetornadoABodega { get; set; } = "No";
    public string FechaRetornoIso { get; set; } = string.Empty;
    public string RecibidoPor { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public int? CotizacionOrigenId { get; set; }
    public bool Anulada { get; set; }
    public string MotivoAnulacion { get; set; } = string.Empty;
    public List<VentaDetalle> Detalles { get; set; } = new();
}

public class VentaDetalle
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public long Cantidad { get; set; }
    /// <summary>
    /// Unidad | Millar
    /// </summary>
    public string Unidad { get; set; } = "Unidad";
    public long CantidadUnidadesFisicas { get; set; }
    public long PrecioPorUnidadMedidaCop { get; set; }
    public decimal DescuentoPct { get; set; }
    public long TotalLineaCop { get; set; }
    public long CostoUnitarioCop { get; set; }
}

public class Factura
{
    public int Id { get; set; }
    public string FacturaCodigo { get; set; } = string.Empty;
    public int VentaId { get; set; }
    public string FechaIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string ClienteNombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string VendedorNombre { get; set; } = string.Empty;
    public long SubtotalCop { get; set; }
    public long DescuentoCop { get; set; }
    public long FleteCop { get; set; }
    public long TotalCop { get; set; }
    public string Estado { get; set; } = "Emitida";
    public string RutaPdf { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public List<FacturaDetalle> Detalles { get; set; } = new();
}

public class FacturaDetalle
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public long Cantidad { get; set; }
    public string Unidad { get; set; } = "Unidad";
    public long PrecioUnitarioCop { get; set; }
    public decimal DescuentoPct { get; set; }
    public long TotalLineaCop { get; set; }
}

public class Cotizacion
{
    public int Id { get; set; }
    public string CotizacionCodigo { get; set; } = string.Empty;
    public string FechaIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string ClienteNombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string TipoLadrilloResumen { get; set; } = string.Empty;
    public long CantidadResumen { get; set; }
    public string UnidadResumen { get; set; } = "Unidad";
    public long PrecioUnitarioResumenCop { get; set; }
    public decimal DescuentoPctResumen { get; set; }
    public long TotalCop { get; set; }
    /// <summary>
    /// Borrador | Enviada | Aceptada | Rechazada | Vencida | Convertida
    /// </summary>
    public string Estado { get; set; } = "Enviada";
    public string VigenteHastaIso { get; set; } = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
    public string ConvertidaEnVenta { get; set; } = "No";
    public int? VentaGeneradaId { get; set; }
    public string RutaPdf { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public List<CotizacionDetalle> Detalles { get; set; } = new();
}

public class CotizacionDetalle
{
    public int Id { get; set; }
    public int CotizacionId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public long Cantidad { get; set; }
    public string Unidad { get; set; } = "Unidad";
    public long CantidadUnidadesFisicas { get; set; }
    public long PrecioUnitarioCop { get; set; }
    public decimal DescuentoPct { get; set; }
    public long TotalLineaCop { get; set; }
}

public class Vehiculo
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public long CapacidadUnidades { get; set; }
    public bool Activo { get; set; } = true;
}

public class Conductor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string PlacaHabitual { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class FleteTransporte
{
    public int Id { get; set; }
    public string FechaSalidaIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string Placa { get; set; } = string.Empty;
    public string Conductor { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int? VentaId { get; set; }
    public string ClientePedido { get; set; } = string.Empty;
    public long CostoFleteCop { get; set; }
    public string EstadoEntrega { get; set; } = "En Ruta";
    public string Observaciones { get; set; } = string.Empty;
}

public class Devolucion
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public string FacturaId { get; set; } = string.Empty;
    public string FechaRetornoIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string ClienteNombre { get; set; } = string.Empty;
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public long CantidadUnidades { get; set; }
    public string Motivo { get; set; } = string.Empty;
    /// <summary>
    /// Apto | Dañado
    /// </summary>
    public string EstadoProducto { get; set; } = "Apto";
    public bool AprobadaParaReingreso { get; set; } = true;
    public bool RetornadoABodega { get; set; }
    public string RecibidoPor { get; set; } = string.Empty;
    public long ValorDescontadoSaldoCop { get; set; }
    public string Observaciones { get; set; } = string.Empty;
}

public class Pago
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public string FacturaId { get; set; } = string.Empty;
    public string FechaIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public string ClienteNombre { get; set; } = string.Empty;
    public long MontoCop { get; set; }
    public long SaldoAnteriorCop { get; set; }
    public long SaldoNuevoCop { get; set; }
    public string MedioPago { get; set; } = "Efectivo";
    public string Referencia { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public class ComisionRegistro
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public string FacturaId { get; set; } = string.Empty;
    public int? VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public string FechaIso { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public long BaseVentaCop { get; set; }
    public decimal ComisionPct { get; set; }
    public long ComisionCop { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? FechaPagoIso { get; set; }
}

public class ConfiguracionItem
{
    public string Parametro { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public class ControlConsecutivo
{
    public string Tipo { get; set; } = string.Empty; // FAC o COT
    public string Prefijo { get; set; } = string.Empty;
    public string IntervaloReinicio { get; set; } = "Mensual";
    public int DiasReinicio { get; set; } = 30;
    public string LastKey { get; set; } = string.Empty;
    public long LastCount { get; set; }
}

public class UsuarioLocal
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = "Administrador";
    public string Pin { get; set; } = "1234";
    public bool Activo { get; set; } = true;
}

public class DashboardKpis
{
    public long VentasHoyCop { get; set; }
    public int CantidadVentasHoy { get; set; }
    public long VentasMesCop { get; set; }
    public int CantidadVentasMes { get; set; }
    public long SaldoPendienteClientesCop { get; set; }
    public long TotalUnidadesInventario { get; set; }
    public long TotalUnidadesSecado { get; set; }
    public long TotalUnidadesHorno { get; set; }
    public long TotalUnidadesPerdidas { get; set; }
    public int EntregasPendientes { get; set; }
    public long ComisionesPendientesCop { get; set; }
    public long GananciaNetaMesCop { get; set; }
    public List<Producto> ResumenProductos { get; set; } = new();
}

public class ResultadoMigracionExcel
{
    public string ArchivoOrigen { get; set; } = string.Empty;
    public int ProductosImportados { get; set; }
    public int VendedoresImportados { get; set; }
    public int VentasImportadas { get; set; }
    public int CotizacionesImportadas { get; set; }
    public int TransportesImportados { get; set; }
    public int PerdidasImportadas { get; set; }
    public int ConfiguracionesImportadas { get; set; }
    public int FilasOmitidasDuplicadas { get; set; }
    public List<string> Advertencias { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

