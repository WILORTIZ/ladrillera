using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SistemaLadrillera.Domain.Entities;
using SistemaLadrillera.Infrastructure.Services;

namespace SistemaLadrillera.Wpf;

public partial class MainWindow : Window
{
    private readonly SqliteLadrilleraService _service;
    private readonly ObservableCollection<VentaDetalle> _lineasVentaActual = new();
    private int _productoEditandoId = 0;
    private bool _sidebarExpanded = true;

    public MainWindow()
    {
        InitializeComponent();

        _service = new SqliteLadrilleraService();
        _service.InitializeDatabase();

        GridVentaLineasActuales.ItemsSource = _lineasVentaActual;
        TxtFechaActual.Text = $"Fecha Local: {DateTime.Now:yyyy-MM-dd} • 100% Offline";
        TxtDbRutaCorta.Text = _service.DatabasePath;
        TxtCotVigencia.Text = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
        TxtInfDesde.Text = DateTime.Now.ToString("yyyy-MM-01");
        TxtInfHasta.Text = DateTime.Now.ToString("yyyy-MM-dd");

        RecargarTodo();
    }

    private void RecargarTodo()
    {
        try
        {
            var productos = _service.GetProductos(false);
            var productosActivos = productos.Where(p => p.Activo).ToList();
            var vendedores = _service.GetVendedores(false);
            var vendedoresActivos = vendedores.Where(v => v.Estado == "Activo").ToList();

            // 1. Dashboard
            var kpis = _service.GetDashboardKpis();
            KpiVentasHoy.Text = $"${kpis.VentasHoyCop:N0}";
            KpiCantVentasHoy.Text = $"{kpis.CantidadVentasHoy} venta(s) hoy";
            KpiVentasMes.Text = $"${kpis.VentasMesCop:N0}";
            KpiGananciaMes.Text = $"Ganancia neta: ${kpis.GananciaNetaMesCop:N0}";
            KpiSaldoClientes.Text = $"${kpis.SaldoPendienteClientesCop:N0}";
            KpiStockDisponible.Text = $"{kpis.TotalUnidadesInventario:N0} Unid.";
            KpiStockSecado.Text = $"{kpis.TotalUnidadesSecado:N0} Unid.";
            KpiStockHorno.Text = $"{kpis.TotalUnidadesHorno:N0} Unid.";
            KpiPerdidas.Text = $"{kpis.TotalUnidadesPerdidas:N0} Unid.";
            KpiEntregasYComisiones.Text = $"{kpis.EntregasPendientes} despachos | ${kpis.ComisionesPendientesCop:N0}";

            GridDashboardProductos.ItemsSource = productos;
            var todasLasVentas = _service.GetVentas();
            GridDashboardVentas.ItemsSource = todasLasVentas.Take(15).ToList();

            // 2. Producción
            CmbProdProducto.ItemsSource = productosActivos;
            if (CmbProdProducto.SelectedIndex < 0 && productosActivos.Count > 0) CmbProdProducto.SelectedIndex = 0;
            GridProdResumen.ItemsSource = productos;
            GridProdHistorial.ItemsSource = _service.GetProduccionMovimientos();

            // 3. Inventario y Kardex
            CmbKardexProducto.ItemsSource = productosActivos;
            if (CmbKardexProducto.SelectedIndex < 0 && productosActivos.Count > 0) CmbKardexProducto.SelectedIndex = 0;
            GridInventarioProductos.ItemsSource = productos;
            GridKardex.ItemsSource = _service.GetKardexMovimientos();

            // 4. Ventas
            TxtVentaConsecutivoPreview.Text = _service.PeekSiguienteConsecutivo("FAC");
            CmbVentaVendedor.ItemsSource = vendedoresActivos;
            if (CmbVentaVendedor.SelectedIndex < 0 && vendedoresActivos.Count > 0) CmbVentaVendedor.SelectedIndex = 0;
            CmbVentaLineaProducto.ItemsSource = productosActivos;
            if (CmbVentaLineaProducto.SelectedIndex < 0 && productosActivos.Count > 0) CmbVentaLineaProducto.SelectedIndex = 0;
            CmbDevProducto.ItemsSource = productosActivos;
            if (CmbDevProducto.SelectedIndex < 0 && productosActivos.Count > 0) CmbDevProducto.SelectedIndex = 0;
            GridHistorialVentas.ItemsSource = todasLasVentas;

            // 5. Facturación
            GridFacturas.ItemsSource = _service.GetFacturas();

            // 6. Cotizaciones
            CmbCotProducto.ItemsSource = productosActivos;
            if (CmbCotProducto.SelectedIndex < 0 && productosActivos.Count > 0) CmbCotProducto.SelectedIndex = 0;
            GridCotizaciones.ItemsSource = _service.GetCotizaciones();

            // 7. Transporte
            GridVehiculos.ItemsSource = _service.GetVehiculos();
            GridFletes.ItemsSource = _service.GetFletes();

            // 8. Vendedores y Comisiones
            GridVendedores.ItemsSource = vendedores;
            GridComisiones.ItemsSource = _service.GetComisiones();

            // 10. Configuración
            GridConsecutivos.ItemsSource = _service.GetConsecutivos();
            GridConfiguraciones.ItemsSource = _service.GetConfiguraciones();
            TxtSubtituloEmpresa.Text = $"{_service.GetConfigValor("Nombre_Ladrillera", "LADRILLERA")} • NIT: {_service.GetConfigValor("NIT_Ladrillera", "900.123.456-7")} • Operación 100% Offline";

            ActualizarPrecioLineaVenta();
            ActualizarPrecioCotizacion();
        }
        catch (Exception ex)
        {
            MostrarEstado($"Error al recargar datos: {ex.Message}");
        }
    }

    private void MostrarEstado(string mensaje)
    {
        TxtBarraEstado.Text = $"[{DateTime.Now:HH:mm:ss}] {mensaje}";
    }

    // ==================== NAVEGACIÓN LATERAL ====================
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int tabIndex))
        {
            MainTabs.SelectedIndex = tabIndex;
            TxtTituloModulo.Text = btn.Content?.ToString()?.Trim() ?? "Sistema Ladrillera";
            RecargarTodo();
        }
    }

    private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        ColSidebar.Width = _sidebarExpanded ? new GridLength(250) : new GridLength(0);
    }

    private void BtnAccesoNuevaVenta_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 3;
        TxtTituloModulo.Text = "🛒  4. Ventas, Pagos y Devoluciones";
        RecargarTodo();
    }

    private void BtnRefrescarTodo_Click(object sender, RoutedEventArgs e)
    {
        RecargarTodo();
        MostrarEstado("Datos sincronizados desde SQLite correctamente.");
    }

    // ==================== MÓDULO 2: PRODUCCIÓN ====================
    private void CmbProdOperacion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PanelProdPerdida == null) return;
        PanelProdPerdida.Visibility = CmbProdOperacion.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnRegistrarProduccion_Click(object sender, RoutedEventArgs e)
    {
        if (CmbProdProducto.SelectedValue is not int prodId)
        {
            MessageBox.Show("Seleccione un tipo de ladrillo.", "Producción", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!long.TryParse(TxtProdCantidad.Text.Trim(), out long cant) || cant <= 0)
        {
            MessageBox.Show("Ingrese una cantidad válida mayor a cero.", "Producción", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string resp = TxtProdResponsable.Text.Trim();
        string obs = TxtProdObservaciones.Text.Trim();

        switch (CmbProdOperacion.SelectedIndex)
        {
            case 0:
                _service.RegistrarEntradaSecado(prodId, cant, resp, obs);
                MostrarEstado($"Se ingresaron {cant:N0} unidades a Secado.");
                break;
            case 1:
                _service.TrasladarSecadoAHorno(prodId, cant, resp, obs);
                MostrarEstado($"Se trasladaron {cant:N0} unidades de Secado a Horno.");
                break;
            case 2:
                _service.FinalizarHornoAInventario(prodId, cant, resp, obs);
                MostrarEstado($"Cocción finalizada: {cant:N0} unidades pasaron del Horno al Inventario Disponible.");
                break;
            case 3:
                string etapa = (CmbProdEtapaPerdida.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Secado";
                string causa = TxtProdCausa.Text.Trim();
                _service.RegistrarPerdidaProduccion(prodId, etapa, cant, causa, resp, obs);
                MostrarEstado($"Pérdida registrada ({cant:N0} unid. en {etapa}).");
                break;
        }

        TxtProdObservaciones.Clear();
        RecargarTodo();
        MessageBox.Show("Movimiento de producción registrado correctamente en SQLite.", "Producción", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ==================== MÓDULO 3: INVENTARIO Y KARDEX ====================
    private void GridInventarioProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridInventarioProductos.SelectedItem is Producto p)
        {
            _productoEditandoId = p.Id;
            TxtInvCodigo.Text = p.Codigo;
            TxtInvNombre.Text = p.Nombre;
            TxtInvCostoUnit.Text = p.CostoUnitarioCop.ToString();
            TxtInvPrecioMillar.Text = p.PrecioMillarCop.ToString();
            TxtInvStockInicial.Text = p.StockActual.ToString();
        }
    }

    private void BtnLimpiarProducto_Click(object sender, RoutedEventArgs e)
    {
        _productoEditandoId = 0;
        TxtInvCodigo.Clear();
        TxtInvNombre.Clear();
        TxtInvCostoUnit.Text = "500";
        TxtInvPrecioMillar.Text = "500000";
        TxtInvStockInicial.Text = "0";
        GridInventarioProductos.SelectedItem = null;
    }

    private void BtnGuardarProducto_Click(object sender, RoutedEventArgs e)
    {
        long.TryParse(TxtInvCostoUnit.Text.Trim(), out long costo);
        long.TryParse(TxtInvPrecioMillar.Text.Trim(), out long pmillar);
        long.TryParse(TxtInvStockInicial.Text.Trim(), out long stkIni);

        var prod = new Producto
        {
            Id = _productoEditandoId,
            Codigo = TxtInvCodigo.Text.Trim(),
            Nombre = TxtInvNombre.Text.Trim(),
            CostoUnitarioCop = costo,
            PrecioMillarCop = pmillar,
            PrecioUnitarioCop = pmillar > 0 ? pmillar / 1000 : costo,
            StockActual = stkIni,
            Activo = true
        };

        _service.GuardarProducto(prod);
        BtnLimpiarProducto_Click(sender, e);
        RecargarTodo();
        MostrarEstado($"Producto '{prod.Nombre}' guardado correctamente.");
    }

    private void BtnRegistrarAjusteKardex_Click(object sender, RoutedEventArgs e)
    {
        if (CmbKardexProducto.SelectedValue is not int prodId) return;
        if (!long.TryParse(TxtKardexCantidad.Text.Trim(), out long cant) || cant <= 0)
        {
            MessageBox.Show("Ingrese una cantidad mayor a cero.", "Kardex", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        string concepto = (CmbKardexConcepto.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "EntradaManual";
        _service.RegistrarAjusteInventario(prodId, concepto, cant, TxtKardexReferencia.Text.Trim(), "Administrador", TxtKardexObs.Text.Trim());
        TxtKardexObs.Clear();
        RecargarTodo();
        MostrarEstado($"Movimiento de Kardex ({concepto}: {cant:N0} unid.) registrado.");
    }

    // ==================== MÓDULO 4: VENTAS, PAGOS Y DEVOLUCIONES ====================
    private void CmbVentaLineaProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ActualizarPrecioLineaVenta();
    }

    private void ActualizarPrecioLineaVenta()
    {
        if (CmbVentaLineaProducto?.SelectedItem is Producto p && TxtVentaLineaPrecio != null)
        {
            string unidad = (CmbVentaLineaUnidad?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unidad";
            TxtVentaLineaPrecio.Text = string.Equals(unidad, "Millar", StringComparison.OrdinalIgnoreCase)
                ? p.PrecioMillarCop.ToString()
                : p.PrecioUnitarioCop.ToString();
        }
    }

    private void BtnAgregarLineaVenta_Click(object sender, RoutedEventArgs e)
    {
        if (CmbVentaLineaProducto.SelectedItem is not Producto prod)
        {
            MessageBox.Show("Seleccione un producto.", "Ventas", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!long.TryParse(TxtVentaLineaCantidad.Text.Trim(), out long cant) || cant <= 0)
        {
            MessageBox.Show("Cantidad inválida.", "Ventas", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!long.TryParse(TxtVentaLineaPrecio.Text.Trim(), out long precio) || precio < 0)
        {
            MessageBox.Show("Precio inválido.", "Ventas", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        decimal.TryParse(TxtVentaLineaDescuento.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal descPct);

        string unidad = (CmbVentaLineaUnidad.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unidad";
        bool esMillar = string.Equals(unidad, "Millar", StringComparison.OrdinalIgnoreCase);
        long unidFisicas = esMillar ? cant * 1000L : cant;

        long bruto = cant * precio;
        long desc = (long)Math.Round(bruto * (descPct / 100m), 0, MidpointRounding.AwayFromZero);

        _lineasVentaActual.Add(new VentaDetalle
        {
            ProductoId = prod.Id,
            ProductoNombre = prod.Nombre,
            Cantidad = cant,
            Unidad = esMillar ? "Millar" : "Unidad",
            CantidadUnidadesFisicas = unidFisicas,
            PrecioPorUnidadMedidaCop = precio,
            DescuentoPct = descPct,
            TotalLineaCop = bruto - desc,
            CostoUnitarioCop = prod.CostoUnitarioCop
        });

        RecalcularResumenVentaEnPantalla();
    }

    private void BtnQuitarLineaVenta_Click(object sender, RoutedEventArgs e)
    {
        if (GridVentaLineasActuales.SelectedItem is VentaDetalle d)
        {
            _lineasVentaActual.Remove(d);
            RecalcularResumenVentaEnPantalla();
        }
    }

    private void TxtVentaRecalcular_TextChanged(object sender, TextChangedEventArgs e) => RecalcularResumenVentaEnPantalla();
    private void ChkVentaIncluirFlete_Changed(object sender, RoutedEventArgs e) => RecalcularResumenVentaEnPantalla();

    private void RecalcularResumenVentaEnPantalla()
    {
        if (TxtVentaResumenTotal == null) return;
        long totalLineas = _lineasVentaActual.Sum(x => x.TotalLineaCop);
        long.TryParse(TxtVentaFlete?.Text?.Trim(), out long flete);
        bool incFlete = ChkVentaIncluirFlete?.IsChecked == true;
        long total = totalLineas + (incFlete ? flete : 0);
        long totalUnidades = _lineasVentaActual.Sum(x => x.CantidadUnidadesFisicas);

        TxtVentaResumenTotal.Text = $"TOTAL VENTA: ${total:N0} COP";
        TxtVentaResumenUnidades.Text = $"{_lineasVentaActual.Count} línea(s) • {totalUnidades:N0} unidades físicas a descontar";
    }

    private void BtnGuardarVentaCompleta_Click(object sender, RoutedEventArgs e)
    {
        if (_lineasVentaActual.Count == 0)
        {
            MessageBox.Show("Agregue al menos un tipo de ladrillo al pedido antes de confirmar la venta.", "Ventas", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        long.TryParse(TxtVentaFlete.Text.Trim(), out long flete);
        long.TryParse(TxtVentaAbonoInicial.Text.Trim(), out long abono);
        string medio = (CmbVentaMedioPago.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";

        var venta = new Venta
        {
            ClienteNombre = TxtVentaCliente.Text.Trim(),
            Telefono = TxtVentaTelefono.Text.Trim(),
            Direccion = TxtVentaDireccion.Text.Trim(),
            VendedorId = CmbVentaVendedor.SelectedValue as int?,
            PlacaTransporte = TxtVentaPlaca.Text.Trim(),
            ConductorNombre = TxtVentaConductor.Text.Trim(),
            FleteAsignadoCop = flete,
            FleteIncluidoEnFactura = ChkVentaIncluirFlete.IsChecked == true,
            Observaciones = TxtVentaObservaciones.Text.Trim(),
            Detalles = _lineasVentaActual.ToList()
        };

        var creada = _service.CrearVenta(venta, abono, medio);
        _lineasVentaActual.Clear();
        TxtVentaCliente.Clear();
        TxtVentaTelefono.Clear();
        TxtVentaDireccion.Clear();
        TxtVentaObservaciones.Clear();
        TxtVentaAbonoInicial.Text = "0";
        RecalcularResumenVentaEnPantalla();
        RecargarTodo();

        MostrarEstado($"Venta y Factura {creada.FacturaId} registradas por ${creada.TotalVentaCop:N0} COP.");
        MessageBox.Show(
            $"¡Venta registrada exitosamente!\n\nFactura Nro: {creada.FacturaId}\nTotal: ${creada.TotalVentaCop:N0} COP\nSaldo Pendiente: ${creada.SaldoClienteCop:N0} COP\nComisión Vendedor: ${creada.ComisionCop:N0} COP\nGanancia Neta: ${creada.GananciaNetaCop:N0} COP",
            "Venta Exitosa",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnFiltrarVentas_Click(object sender, RoutedEventArgs e)
    {
        GridHistorialVentas.ItemsSource = _service.GetVentas(TxtFiltroVentas.Text.Trim());
    }

    private void BtnExportarPdfVentaSeleccionada_Click(object sender, RoutedEventArgs e)
    {
        if (GridHistorialVentas.SelectedItem is not Venta v)
        {
            MessageBox.Show("Seleccione una venta de la tabla.", "Facturación PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var fac = _service.GetFacturas(v.FacturaId).FirstOrDefault();
        if (fac == null) return;
        string pdfPath = _service.GenerarPdfFactura(fac.Id);
        MostrarEstado($"PDF generado en: {pdfPath}");
        AbrirArchivoLocal(pdfPath);
        RecargarTodo();
    }

    private void BtnAnularVentaSeleccionada_Click(object sender, RoutedEventArgs e)
    {
        if (GridHistorialVentas.SelectedItem is not Venta v)
        {
            MessageBox.Show("Seleccione una venta para anular.", "Anular Venta", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"¿Está seguro de ANULAR la venta {v.FacturaId} de {v.ClienteNombre}?\nEsto devolverá las unidades al inventario en el Kardex y anulará la factura y comisión.",
                "Confirmar Anulación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _service.AnularVenta(v.Id, "Anulación autorizada desde interfaz", "Administrador");
            RecargarTodo();
            MostrarEstado($"Venta {v.FacturaId} anulada e inventario restituido.");
        }
    }

    private void BtnRegistrarAbono_Click(object sender, RoutedEventArgs e)
    {
        if (GridHistorialVentas.SelectedItem is not Venta v)
        {
            MessageBox.Show("Seleccione primero una venta con saldo pendiente en la tabla superior.", "Pagos y Abonos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!long.TryParse(TxtPagoMonto.Text.Trim(), out long monto) || monto <= 0)
        {
            MessageBox.Show("Ingrese un monto de abono válido.", "Pagos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        string medio = (CmbPagoMedio.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";
        var pago = _service.RegistrarPago(v.Id, monto, medio, TxtPagoReferencia.Text.Trim(), "Abono registrado en cartera");
        RecargarTodo();
        MostrarEstado($"Abono de ${monto:N0} aplicado a {v.FacturaId}. Nuevo saldo: ${pago.SaldoNuevoCop:N0} COP.");
        MessageBox.Show($"Abono registrado con éxito.\nSaldo Anterior: ${pago.SaldoAnteriorCop:N0}\nNuevo Saldo: ${pago.SaldoNuevoCop:N0}", "Pago Registrado", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnRegistrarDevolucion_Click(object sender, RoutedEventArgs e)
    {
        if (GridHistorialVentas.SelectedItem is not Venta v)
        {
            MessageBox.Show("Seleccione primero la venta en la tabla superior.", "Devoluciones", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CmbDevProducto.SelectedValue is not int prodId) return;
        if (!long.TryParse(TxtDevCantidad.Text.Trim(), out long cant) || cant <= 0)
        {
            MessageBox.Show("Ingrese una cantidad de unidades devueltas mayor a cero.", "Devoluciones", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        long.TryParse(TxtDevDescuentoSaldo.Text.Trim(), out long descSaldo);
        string estadoProd = (CmbDevEstadoProducto.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Apto";

        var dev = new Devolucion
        {
            VentaId = v.Id,
            ProductoId = prodId,
            CantidadUnidades = cant,
            EstadoProducto = estadoProd,
            AprobadaParaReingreso = true,
            Motivo = TxtDevMotivo.Text.Trim(),
            RecibidoPor = TxtDevRecibidoPor.Text.Trim(),
            ValorDescontadoSaldoCop = descSaldo
        };

        var procesada = _service.RegistrarDevolucion(dev);
        RecargarTodo();
        string msgStock = procesada.RetornadoABodega
            ? "El material estaba APTO y fue reincorporado al Inventario Disponible (Kardex)."
            : "El material estaba DAÑADO, por lo que NO sumó al inventario disponible y se registró en Dañados.";
        MostrarEstado($"Devolución registrada en {v.FacturaId}. {msgStock}");
        MessageBox.Show($"Devolución procesada correctamente.\n\n{msgStock}", "Devolución", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ==================== MÓDULO 5: FACTURACIÓN Y PDF ====================
    private void BtnFiltrarFacturas_Click(object sender, RoutedEventArgs e)
    {
        GridFacturas.ItemsSource = _service.GetFacturas(TxtFiltroFacturas.Text.Trim());
    }

    private void BtnGenerarPdfFacturaModulo_Click(object sender, RoutedEventArgs e)
    {
        if (GridFacturas.SelectedItem is not Factura f)
        {
            MessageBox.Show("Seleccione una factura de la tabla.", "Facturación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        string ruta = _service.GenerarPdfFactura(f.Id);
        RecargarTodo();
        MostrarEstado($"Factura PDF generada en: {ruta}");
        AbrirArchivoLocal(ruta);
    }

    // ==================== MÓDULO 6: COTIZACIONES ====================
    private void CmbCotProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ActualizarPrecioCotizacion();
    }

    private void ActualizarPrecioCotizacion()
    {
        if (CmbCotProducto?.SelectedItem is Producto p && TxtCotPrecio != null)
        {
            string unidad = (CmbCotUnidad?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unidad";
            TxtCotPrecio.Text = string.Equals(unidad, "Millar", StringComparison.OrdinalIgnoreCase)
                ? p.PrecioMillarCop.ToString()
                : p.PrecioUnitarioCop.ToString();
        }
    }

    private void BtnCrearCotizacion_Click(object sender, RoutedEventArgs e)
    {
        if (CmbCotProducto.SelectedItem is not Producto prod) return;
        if (!long.TryParse(TxtCotCantidad.Text.Trim(), out long cant) || cant <= 0)
        {
            MessageBox.Show("Ingrese una cantidad válida.", "Cotizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        long.TryParse(TxtCotPrecio.Text.Trim(), out long precio);
        decimal.TryParse(TxtCotDescuento.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal desc);
        string unidad = (CmbCotUnidad.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unidad";

        var cot = new Cotizacion
        {
            ClienteNombre = TxtCotCliente.Text.Trim(),
            Telefono = TxtCotTelefono.Text.Trim(),
            VigenteHastaIso = TxtCotVigencia.Text.Trim(),
            Observaciones = TxtCotObservaciones.Text.Trim(),
            Estado = "Aceptada",
            Detalles = new List<CotizacionDetalle>
            {
                new()
                {
                    ProductoId = prod.Id,
                    ProductoNombre = prod.Nombre,
                    Cantidad = cant,
                    Unidad = unidad,
                    PrecioUnitarioCop = precio,
                    DescuentoPct = desc
                }
            }
        };

        var creada = _service.CrearCotizacion(cot);
        TxtCotCliente.Clear();
        TxtCotTelefono.Clear();
        TxtCotObservaciones.Clear();
        RecargarTodo();
        MostrarEstado($"Cotización {creada.CotizacionCodigo} creada por ${creada.TotalCop:N0} COP.");
    }

    private void BtnConvertirCotizacionEnVenta_Click(object sender, RoutedEventArgs e)
    {
        if (GridCotizaciones.SelectedItem is not Cotizacion cot)
        {
            MessageBox.Show("Seleccione una cotización de la tabla para convertirla en venta.", "Cotizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vends = _service.GetVendedores(true);
        int? vendId = vends.FirstOrDefault()?.Id;

        var venta = _service.ConvertirCotizacionEnVenta(cot.Id, "POR-ASIGNAR", "", 0, vendId, 0);
        RecargarTodo();
        MostrarEstado($"Cotización {cot.CotizacionCodigo} convertida en Venta/Factura {venta.FacturaId}.");
        MessageBox.Show($"¡Cotización {cot.CotizacionCodigo} convertida exitosamente en la Venta {venta.FacturaId} sin duplicar registros!", "Conversión Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnGenerarPdfCotizacion_Click(object sender, RoutedEventArgs e)
    {
        if (GridCotizaciones.SelectedItem is not Cotizacion cot)
        {
            MessageBox.Show("Seleccione una cotización de la tabla.", "Cotizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        string pdf = _service.GenerarPdfCotizacion(cot.Id);
        RecargarTodo();
        MostrarEstado($"Cotización PDF generada en: {pdf}");
        AbrirArchivoLocal(pdf);
    }

    // ==================== MÓDULO 7: TRANSPORTE ====================
    private void BtnRegistrarFlete_Click(object sender, RoutedEventArgs e)
    {
        long.TryParse(TxtTransCostoFlete.Text.Trim(), out long costo);
        _service.RegistrarFlete(new FleteTransporte
        {
            Placa = TxtTransPlaca.Text.Trim(),
            Conductor = TxtTransConductor.Text.Trim(),
            Telefono = TxtTransTelefono.Text.Trim(),
            ClientePedido = TxtTransPedido.Text.Trim(),
            CostoFleteCop = costo,
            EstadoEntrega = "En Ruta"
        });
        TxtTransPedido.Clear();
        RecargarTodo();
        MostrarEstado("Flete/salida de transporte registrado.");
    }

    private void BtnGuardarVehiculo_Click(object sender, RoutedEventArgs e)
    {
        long.TryParse(TxtVehCapacidad.Text.Trim(), out long cap);
        _service.GuardarVehiculo(new Vehiculo
        {
            Placa = TxtVehPlaca.Text.Trim(),
            Descripcion = TxtVehDesc.Text.Trim(),
            CapacidadUnidades = cap,
            Activo = true
        });
        RecargarTodo();
        MostrarEstado("Vehículo guardado en la flota.");
    }

    private void BtnMarcarFleteEntregado_Click(object sender, RoutedEventArgs e)
    {
        if (GridFletes.SelectedItem is not FleteTransporte f)
        {
            MessageBox.Show("Seleccione un flete de la tabla.", "Transporte", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (f.VentaId.HasValue)
        {
            _service.ActualizarEstadoEntregaVenta(f.VentaId.Value, "Entregado");
        }
        RecargarTodo();
        MostrarEstado($"Despacho de placa {f.Placa} marcado como ENTREGADO.");
    }

    // ==================== MÓDULO 8: VENDEDORES Y COMISIONES ====================
    private void BtnGuardarVendedor_Click(object sender, RoutedEventArgs e)
    {
        decimal.TryParse(TxtVendPct.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pct);
        string estado = (CmbVendEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Activo";
        _service.GuardarVendedor(new Vendedor
        {
            Codigo = TxtVendCodigo.Text.Trim(),
            Nombre = TxtVendNombre.Text.Trim(),
            ComisionPct = pct,
            Estado = estado
        });
        TxtVendNombre.Clear();
        RecargarTodo();
        MostrarEstado("Vendedor guardado correctamente.");
    }

    private void BtnPagarComision_Click(object sender, RoutedEventArgs e)
    {
        if (GridComisiones.SelectedItem is not ComisionRegistro c)
        {
            MessageBox.Show("Seleccione una comisión pendiente de la tabla.", "Comisiones", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _service.MarcarComisionPagada(c.Id);
        RecargarTodo();
        MostrarEstado($"Comisión de {c.VendedorNombre} (${c.ComisionCop:N0}) marcada como PAGADA.");
    }

    // ==================== MÓDULO 9: INFORMES ====================
    private void BtnExportarVentasCsv_Click(object sender, RoutedEventArgs e)
    {
        string path = _service.ExportarInformeCsv("ventas", null, TxtInfDesde.Text.Trim(), TxtInfHasta.Text.Trim());
        MostrarEstado($"Informe CSV de Ventas exportado: {path}");
        AbrirArchivoLocal(path);
    }

    private void BtnExportarVentasPdf_Click(object sender, RoutedEventArgs e)
    {
        string path = _service.ExportarInformePdf("ventas", null, TxtInfDesde.Text.Trim(), TxtInfHasta.Text.Trim());
        MostrarEstado($"Informe PDF de Ventas exportado: {path}");
        AbrirArchivoLocal(path);
    }

    private void BtnExportarInventarioCsv_Click(object sender, RoutedEventArgs e)
    {
        string path = _service.ExportarInformeCsv("inventario");
        MostrarEstado($"Informe CSV de Inventario exportado: {path}");
        AbrirArchivoLocal(path);
    }

    private void BtnExportarInventarioPdf_Click(object sender, RoutedEventArgs e)
    {
        string path = _service.ExportarInformePdf("inventario");
        MostrarEstado($"Informe PDF de Inventario exportado: {path}");
        AbrirArchivoLocal(path);
    }

    private void BtnExportarCarteraCsv_Click(object sender, RoutedEventArgs e)
    {
        string path = _service.ExportarInformeCsv("cartera");
        MostrarEstado($"Informe CSV de Cartera exportado: {path}");
        AbrirArchivoLocal(path);
    }

    private void BtnExportarComisionesCsv_Click(object sender, RoutedEventArgs e)
    {
        string path = _service.ExportarInformeCsv("comisiones");
        MostrarEstado($"Informe CSV de Comisiones exportado: {path}");
        AbrirArchivoLocal(path);
    }

    // ==================== MÓDULO 10: CONFIGURACIÓN, MIGRACIÓN Y BACKUPS ====================
    private void GridConfiguraciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridConfiguraciones.SelectedItem is ConfiguracionItem item)
        {
            TxtCfgClave.Text = item.Parametro;
            TxtCfgValor.Text = item.Valor;
        }
    }

    private void BtnGuardarParametroCfg_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCfgClave.Text)) return;
        _service.GuardarConfiguracion(TxtCfgClave.Text.Trim(), TxtCfgValor.Text.Trim());
        RecargarTodo();
        MostrarEstado($"Parámetro '{TxtCfgClave.Text}' actualizado.");
    }

    private void BtnAccesoBackupRapido_Click(object sender, RoutedEventArgs e)
    {
        string ruta = _service.CrearCopiaSeguridad();
        MostrarEstado($"Copia de seguridad creada en: {ruta}");
        MessageBox.Show($"Copia de seguridad SQLite creada exitosamente en:\n\n{ruta}", "Respaldo Local", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnRestaurarBackup_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Seleccionar archivo de respaldo SQLite (.db)",
            Filter = "Base de datos SQLite (*.db)|*.db|Todos los archivos (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            if (MessageBox.Show("¿Desea restaurar esta copia de seguridad? Se creará un respaldo preventivo automático antes de restaurar.",
                    "Confirmar Restauración", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _service.RestaurarCopiaSeguridad(dlg.FileName);
                RecargarTodo();
                MostrarEstado("Copia de seguridad restaurada y verificada con éxito.");
                MessageBox.Show("Base de datos restaurada exitosamente.", "Restauración", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void BtnVerificarIntegridad_Click(object sender, RoutedEventArgs e)
    {
        bool ok = _service.VerificarIntegridadBaseDatos(out string msg);
        MessageBox.Show($"Resultado de PRAGMA integrity_check:\n\nEstado: {(ok ? "OK (Base íntegra)" : "ALERTA")}\nDetalle: {msg}",
            "Integridad SQLite", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnSeleccionarExcel_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            InitialDirectory = @"D:\antigravity\ladrillera",
            Filter = "Libros de Excel (*.xlsm;*.xlsx)|*.xlsm;*.xlsx"
        };
        if (dlg.ShowDialog() == true)
        {
            TxtRutaExcelMigracion.Text = dlg.FileName;
        }
    }

    private void BtnEjecutarMigracionExcel_Click(object sender, RoutedEventArgs e)
    {
        string ruta = TxtRutaExcelMigracion.Text.Trim();
        var res = _service.ImportarDesdeExcel(ruta);
        RecargarTodo();
        MessageBox.Show(
            $"Informe de Migración desde Excel:\n\nArchivo: {res.ArchivoOrigen}\nProductos nuevos importados: {res.ProductosImportados}\nVendedores nuevos importados: {res.VendedoresImportados}\nParámetros nuevos importados: {res.ConfiguracionesImportadas}\nFilas omitidas por idempotencia (ya existentes): {res.FilasOmitidasDuplicadas}\nErrores: {res.Errores.Count}",
            "Reporte de Migración Excel → SQLite",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static void AbrirArchivoLocal(string ruta)
    {
        try
        {
            if (File.Exists(ruta))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Si no hay visor predeterminado, ignorar
        }
    }
}
