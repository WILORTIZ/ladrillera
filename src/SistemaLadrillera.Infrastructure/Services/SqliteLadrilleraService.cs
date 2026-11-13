using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using SistemaLadrillera.Application.Interfaces;
using SistemaLadrillera.Domain.Entities;

namespace SistemaLadrillera.Infrastructure.Services;

public class SqliteLadrilleraService : ILadrilleraService
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly string _baseDocsFolder;

    public string DatabasePath => _dbPath;

    public SqliteLadrilleraService(string? customDbPath = null)
    {
        _baseDocsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Ladrillera");
        Directory.CreateDirectory(_baseDocsFolder);
        Directory.CreateDirectory(Path.Combine(_baseDocsFolder, "Facturas"));
        Directory.CreateDirectory(Path.Combine(_baseDocsFolder, "Cotizaciones"));
        Directory.CreateDirectory(Path.Combine(_baseDocsFolder, "Informes"));
        Directory.CreateDirectory(Path.Combine(_baseDocsFolder, "Backups"));
        Directory.CreateDirectory(Path.Combine(_baseDocsFolder, "Logo"));

        _dbPath = customDbPath ?? Path.Combine(_baseDocsFolder, "SistemaLadrillera.db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            ForeignKeys = true,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    public void InitializeDatabase()
    {
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        string ddl = @"
CREATE TABLE IF NOT EXISTS Productos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Codigo TEXT NOT NULL UNIQUE,
    Nombre TEXT NOT NULL UNIQUE,
    StockActual INTEGER NOT NULL DEFAULT 0 CHECK(StockActual >= 0),
    StockSecado INTEGER NOT NULL DEFAULT 0 CHECK(StockSecado >= 0),
    StockHorno INTEGER NOT NULL DEFAULT 0 CHECK(StockHorno >= 0),
    EntradasAcumuladas INTEGER NOT NULL DEFAULT 0,
    VentasAcumuladas INTEGER NOT NULL DEFAULT 0,
    DanadosAcumulados INTEGER NOT NULL DEFAULT 0,
    CostoUnitarioCop INTEGER NOT NULL DEFAULT 0 CHECK(CostoUnitarioCop >= 0),
    PrecioUnitarioCop INTEGER NOT NULL DEFAULT 0 CHECK(PrecioUnitarioCop >= 0),
    PrecioMillarCop INTEGER NOT NULL DEFAULT 0 CHECK(PrecioMillarCop >= 0),
    Activo INTEGER NOT NULL DEFAULT 1,
    FechaActualizacionIso TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Clientes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Documento TEXT NOT NULL DEFAULT '',
    Nombre TEXT NOT NULL UNIQUE,
    Telefono TEXT NOT NULL DEFAULT '',
    Direccion TEXT NOT NULL DEFAULT '',
    SaldoPendienteCop INTEGER NOT NULL DEFAULT 0,
    Activo INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Vendedores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Codigo TEXT NOT NULL UNIQUE,
    Nombre TEXT NOT NULL,
    ComisionPct REAL NOT NULL DEFAULT 0 CHECK(ComisionPct >= 0 AND ComisionPct <= 100),
    Estado TEXT NOT NULL DEFAULT 'Activo',
    FechaIngresoIso TEXT NOT NULL,
    FechaSalidaIso TEXT
);

CREATE TABLE IF NOT EXISTS ProduccionMovimientos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FechaIso TEXT NOT NULL,
    ProductoId INTEGER NOT NULL,
    ProductoNombre TEXT NOT NULL,
    TipoMovimiento TEXT NOT NULL,
    Etapa TEXT NOT NULL,
    CantidadUnidades INTEGER NOT NULL CHECK(CantidadUnidades > 0),
    CausaPerdida TEXT NOT NULL DEFAULT '',
    Responsable TEXT NOT NULL DEFAULT '',
    Observaciones TEXT NOT NULL DEFAULT '',
    Anulado INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY(ProductoId) REFERENCES Productos(Id)
);

CREATE TABLE IF NOT EXISTS InventarioMovimientos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FechaIso TEXT NOT NULL,
    ProductoId INTEGER NOT NULL,
    ProductoNombre TEXT NOT NULL,
    Concepto TEXT NOT NULL,
    Referencia TEXT NOT NULL DEFAULT '',
    CantidadEntrada INTEGER NOT NULL DEFAULT 0 CHECK(CantidadEntrada >= 0),
    CantidadSalida INTEGER NOT NULL DEFAULT 0 CHECK(CantidadSalida >= 0),
    SaldoPosterior INTEGER NOT NULL DEFAULT 0,
    CostoUnitarioCop INTEGER NOT NULL DEFAULT 0,
    Responsable TEXT NOT NULL DEFAULT 'Sistema',
    Observaciones TEXT NOT NULL DEFAULT '',
    FOREIGN KEY(ProductoId) REFERENCES Productos(Id)
);

CREATE TABLE IF NOT EXISTS Ventas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FechaIso TEXT NOT NULL,
    Hora TEXT NOT NULL,
    FacturaId TEXT NOT NULL UNIQUE,
    ClienteId INTEGER,
    ClienteNombre TEXT NOT NULL,
    Telefono TEXT NOT NULL DEFAULT '',
    Direccion TEXT NOT NULL DEFAULT '',
    TipoLadrilloResumen TEXT NOT NULL DEFAULT '',
    CantidadResumen INTEGER NOT NULL DEFAULT 0,
    UnidadResumen TEXT NOT NULL DEFAULT 'Unidad',
    PrecioUnitarioResumenCop INTEGER NOT NULL DEFAULT 0,
    DescuentoPctResumen REAL NOT NULL DEFAULT 0,
    SubtotalCop INTEGER NOT NULL DEFAULT 0,
    DescuentoCop INTEGER NOT NULL DEFAULT 0,
    TotalVentaCop INTEGER NOT NULL DEFAULT 0,
    PlacaTransporte TEXT NOT NULL DEFAULT '',
    ConductorNombre TEXT NOT NULL DEFAULT '',
    FleteAsignadoCop INTEGER NOT NULL DEFAULT 0,
    FleteIncluidoEnFactura INTEGER NOT NULL DEFAULT 0,
    EstadoPago TEXT NOT NULL DEFAULT 'Pendiente',
    MontoPagadoCop INTEGER NOT NULL DEFAULT 0,
    SaldoClienteCop INTEGER NOT NULL DEFAULT 0,
    EstadoEntrega TEXT NOT NULL DEFAULT 'Pendiente',
    VendedorId INTEGER,
    VendedorNombre TEXT NOT NULL DEFAULT '',
    ComisionPct REAL NOT NULL DEFAULT 0,
    ComisionCop INTEGER NOT NULL DEFAULT 0,
    GananciaNetaCop INTEGER NOT NULL DEFAULT 0,
    MotivoDevolucion TEXT NOT NULL DEFAULT '',
    RetornadoABodega TEXT NOT NULL DEFAULT 'No',
    FechaRetornoIso TEXT NOT NULL DEFAULT '',
    RecibidoPor TEXT NOT NULL DEFAULT '',
    Observaciones TEXT NOT NULL DEFAULT '',
    CotizacionOrigenId INTEGER,
    Anulada INTEGER NOT NULL DEFAULT 0,
    MotivoAnulacion TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS VentaDetalles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VentaId INTEGER NOT NULL,
    ProductoId INTEGER NOT NULL,
    ProductoNombre TEXT NOT NULL,
    Cantidad INTEGER NOT NULL CHECK(Cantidad > 0),
    Unidad TEXT NOT NULL DEFAULT 'Unidad',
    CantidadUnidadesFisicas INTEGER NOT NULL CHECK(CantidadUnidadesFisicas > 0),
    PrecioPorUnidadMedidaCop INTEGER NOT NULL CHECK(PrecioPorUnidadMedidaCop >= 0),
    DescuentoPct REAL NOT NULL DEFAULT 0,
    TotalLineaCop INTEGER NOT NULL DEFAULT 0,
    CostoUnitarioCop INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY(VentaId) REFERENCES Ventas(Id),
    FOREIGN KEY(ProductoId) REFERENCES Productos(Id)
);

CREATE TABLE IF NOT EXISTS Facturas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FacturaCodigo TEXT NOT NULL UNIQUE,
    VentaId INTEGER NOT NULL UNIQUE,
    FechaIso TEXT NOT NULL,
    ClienteNombre TEXT NOT NULL,
    Telefono TEXT NOT NULL DEFAULT '',
    Direccion TEXT NOT NULL DEFAULT '',
    VendedorNombre TEXT NOT NULL DEFAULT '',
    SubtotalCop INTEGER NOT NULL DEFAULT 0,
    DescuentoCop INTEGER NOT NULL DEFAULT 0,
    FleteCop INTEGER NOT NULL DEFAULT 0,
    TotalCop INTEGER NOT NULL DEFAULT 0,
    Estado TEXT NOT NULL DEFAULT 'Emitida',
    RutaPdf TEXT NOT NULL DEFAULT '',
    Observaciones TEXT NOT NULL DEFAULT '',
    FOREIGN KEY(VentaId) REFERENCES Ventas(Id)
);

CREATE TABLE IF NOT EXISTS FacturaDetalles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FacturaId INTEGER NOT NULL,
    ProductoNombre TEXT NOT NULL,
    Cantidad INTEGER NOT NULL,
    Unidad TEXT NOT NULL,
    PrecioUnitarioCop INTEGER NOT NULL,
    DescuentoPct REAL NOT NULL DEFAULT 0,
    TotalLineaCop INTEGER NOT NULL,
    FOREIGN KEY(FacturaId) REFERENCES Facturas(Id)
);

CREATE TABLE IF NOT EXISTS Cotizaciones (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CotizacionCodigo TEXT NOT NULL UNIQUE,
    FechaIso TEXT NOT NULL,
    ClienteNombre TEXT NOT NULL,
    Telefono TEXT NOT NULL DEFAULT '',
    TipoLadrilloResumen TEXT NOT NULL DEFAULT '',
    CantidadResumen INTEGER NOT NULL DEFAULT 0,
    UnidadResumen TEXT NOT NULL DEFAULT 'Unidad',
    PrecioUnitarioResumenCop INTEGER NOT NULL DEFAULT 0,
    DescuentoPctResumen REAL NOT NULL DEFAULT 0,
    TotalCop INTEGER NOT NULL DEFAULT 0,
    Estado TEXT NOT NULL DEFAULT 'Enviada',
    VigenteHastaIso TEXT NOT NULL,
    ConvertidaEnVenta TEXT NOT NULL DEFAULT 'No',
    VentaGeneradaId INTEGER,
    RutaPdf TEXT NOT NULL DEFAULT '',
    Observaciones TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS CotizacionDetalles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CotizacionId INTEGER NOT NULL,
    ProductoId INTEGER NOT NULL,
    ProductoNombre TEXT NOT NULL,
    Cantidad INTEGER NOT NULL,
    Unidad TEXT NOT NULL DEFAULT 'Unidad',
    CantidadUnidadesFisicas INTEGER NOT NULL,
    PrecioUnitarioCop INTEGER NOT NULL,
    DescuentoPct REAL NOT NULL DEFAULT 0,
    TotalLineaCop INTEGER NOT NULL,
    FOREIGN KEY(CotizacionId) REFERENCES Cotizaciones(Id)
);

CREATE TABLE IF NOT EXISTS Vehiculos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Placa TEXT NOT NULL UNIQUE,
    Descripcion TEXT NOT NULL DEFAULT '',
    CapacidadUnidades INTEGER NOT NULL DEFAULT 0,
    Activo INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Conductores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL UNIQUE,
    Telefono TEXT NOT NULL DEFAULT '',
    Documento TEXT NOT NULL DEFAULT '',
    PlacaHabitual TEXT NOT NULL DEFAULT '',
    Activo INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Fletes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FechaSalidaIso TEXT NOT NULL,
    Placa TEXT NOT NULL,
    Conductor TEXT NOT NULL DEFAULT '',
    Telefono TEXT NOT NULL DEFAULT '',
    VentaId INTEGER,
    ClientePedido TEXT NOT NULL DEFAULT '',
    CostoFleteCop INTEGER NOT NULL DEFAULT 0,
    EstadoEntrega TEXT NOT NULL DEFAULT 'En Ruta',
    Observaciones TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS Devoluciones (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VentaId INTEGER NOT NULL,
    FacturaId TEXT NOT NULL,
    FechaRetornoIso TEXT NOT NULL,
    ClienteNombre TEXT NOT NULL,
    ProductoId INTEGER NOT NULL,
    ProductoNombre TEXT NOT NULL,
    CantidadUnidades INTEGER NOT NULL CHECK(CantidadUnidades > 0),
    Motivo TEXT NOT NULL,
    EstadoProducto TEXT NOT NULL,
    AprobadaParaReingreso INTEGER NOT NULL DEFAULT 1,
    RetornadoABodega INTEGER NOT NULL DEFAULT 0,
    RecibidoPor TEXT NOT NULL DEFAULT '',
    ValorDescontadoSaldoCop INTEGER NOT NULL DEFAULT 0,
    Observaciones TEXT NOT NULL DEFAULT '',
    FOREIGN KEY(VentaId) REFERENCES Ventas(Id),
    FOREIGN KEY(ProductoId) REFERENCES Productos(Id)
);

CREATE TABLE IF NOT EXISTS Pagos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VentaId INTEGER NOT NULL,
    FacturaId TEXT NOT NULL,
    FechaIso TEXT NOT NULL,
    ClienteNombre TEXT NOT NULL,
    MontoCop INTEGER NOT NULL CHECK(MontoCop > 0),
    SaldoAnteriorCop INTEGER NOT NULL,
    SaldoNuevoCop INTEGER NOT NULL,
    MedioPago TEXT NOT NULL DEFAULT 'Efectivo',
    Referencia TEXT NOT NULL DEFAULT '',
    Observaciones TEXT NOT NULL DEFAULT '',
    FOREIGN KEY(VentaId) REFERENCES Ventas(Id)
);

CREATE TABLE IF NOT EXISTS Comisiones (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VentaId INTEGER NOT NULL,
    FacturaId TEXT NOT NULL,
    VendedorId INTEGER,
    VendedorNombre TEXT NOT NULL,
    FechaIso TEXT NOT NULL,
    BaseVentaCop INTEGER NOT NULL,
    ComisionPct REAL NOT NULL,
    ComisionCop INTEGER NOT NULL,
    Estado TEXT NOT NULL DEFAULT 'Pendiente',
    FechaPagoIso TEXT,
    FOREIGN KEY(VentaId) REFERENCES Ventas(Id)
);

CREATE TABLE IF NOT EXISTS Configuracion (
    Parametro TEXT PRIMARY KEY,
    Valor TEXT NOT NULL,
    Descripcion TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS ControlConsecutivos (
    Tipo TEXT PRIMARY KEY,
    Prefijo TEXT NOT NULL,
    IntervaloReinicio TEXT NOT NULL DEFAULT 'Mensual',
    DiasReinicio INTEGER NOT NULL DEFAULT 30,
    LastKey TEXT NOT NULL DEFAULT '',
    LastCount INTEGER NOT NULL DEFAULT 0
);
";
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = ddl;
            cmd.ExecuteNonQuery();
        }

        SeedInitialData(conn, tx);
        tx.Commit();
    }

    private void SeedInitialData(SqliteConnection conn, SqliteTransaction tx)
    {
        using var countCmd = conn.CreateCommand();
        countCmd.Transaction = tx;
        countCmd.CommandText = "SELECT COUNT(*) FROM Productos;";
        long prodCount = (long)(countCmd.ExecuteScalar() ?? 0L);

        if (prodCount == 0)
        {
            var initialProducts = new (string Codigo, string Nombre, long Stock, long Secado, long Horno, long Costo, long PrecioMillar)[]
            {
                ("PRD-01", "Farol", 500, 1000, 2000, 500, 500000),
                ("PRD-02", "Bloquelon", 1000, 1000, 2000, 1000, 1000000),
                ("PRD-03", "Maciso", 300, 1000, 2000, 300, 300000),
                ("PRD-04", "Bloque cemento", 1500, 1000, 2000, 1500, 1500000)
            };

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            foreach (var p in initialProducts)
            {
                using var ins = conn.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = @"
INSERT INTO Productos (Codigo, Nombre, StockActual, StockSecado, StockHorno, EntradasAcumuladas, VentasAcumuladas, DanadosAcumulados, CostoUnitarioCop, PrecioUnitarioCop, PrecioMillarCop, Activo, FechaActualizacionIso)
VALUES ($cod, $nom, $stk, $sec, $hor, $stk, 0, 0, $costo, $punit, $pmil, 1, $fec);
SELECT last_insert_rowid();";
                ins.Parameters.AddWithValue("$cod", p.Codigo);
                ins.Parameters.AddWithValue("$nom", p.Nombre);
                ins.Parameters.AddWithValue("$stk", p.Stock);
                ins.Parameters.AddWithValue("$sec", p.Secado);
                ins.Parameters.AddWithValue("$hor", p.Horno);
                ins.Parameters.AddWithValue("$costo", p.Costo);
                ins.Parameters.AddWithValue("$punit", p.PrecioMillar / 1000);
                ins.Parameters.AddWithValue("$pmil", p.PrecioMillar);
                ins.Parameters.AddWithValue("$fec", now);
                long newProdId = (long)ins.ExecuteScalar()!;

                using var kar = conn.CreateCommand();
                kar.Transaction = tx;
                kar.CommandText = @"
INSERT INTO InventarioMovimientos (FechaIso, ProductoId, ProductoNombre, Concepto, Referencia, CantidadEntrada, CantidadSalida, SaldoPosterior, CostoUnitarioCop, Responsable, Observaciones)
VALUES ($fec, $pid, $pnom, 'SaldoInicial', 'MIG-INICIAL-EXCEL', $cant, 0, $cant, $costo, 'Sistema', 'Saldo inicial importado de libros base Sistema Ladrillera');";
                kar.Parameters.AddWithValue("$fec", now);
                kar.Parameters.AddWithValue("$pid", newProdId);
                kar.Parameters.AddWithValue("$pnom", p.Nombre);
                kar.Parameters.AddWithValue("$cant", p.Stock);
                kar.Parameters.AddWithValue("$costo", p.Costo);
                kar.ExecuteNonQuery();
            }
        }

        countCmd.CommandText = "SELECT COUNT(*) FROM Vendedores;";
        long vendCount = (long)(countCmd.ExecuteScalar() ?? 0L);
        if (vendCount == 0)
        {
            var sellers = new (string Cod, string Nom, decimal Pct)[]
            {
                ("V001", "Juan Pérez", 2m),
                ("V002", "Ana Gómez", 3m)
            };
            foreach (var v in sellers)
            {
                using var insV = conn.CreateCommand();
                insV.Transaction = tx;
                insV.CommandText = @"
INSERT INTO Vendedores (Codigo, Nombre, ComisionPct, Estado, FechaIngresoIso)
VALUES ($c, $n, $p, 'Activo', '2025-11-05');";
                insV.Parameters.AddWithValue("$c", v.Cod);
                insV.Parameters.AddWithValue("$n", v.Nom);
                insV.Parameters.AddWithValue("$p", (double)v.Pct);
                insV.ExecuteNonQuery();
            }
        }

        var configs = new (string K, string V, string D)[]
        {
            ("Nombre_Ladrillera", "LADRILLERA SAN ANDRES S.A.S.", "Nombre o razón social en reportes y comprobantes"),
            ("NIT_Ladrillera", "900.123.456-7", "NIT o identificación tributaria"),
            ("Telefono_Ladrillera", "300 000 0000", "Teléfono de contacto de la planta"),
            ("Direccion_Ladrillera", "Km 4 Vía Principal Planta Ladrillera", "Dirección física de la planta"),
            ("Prefijo_FAC", "FAC", "Prefijo de comprobantes de facturación"),
            ("Prefijo_COT", "COT", "Prefijo de cotizaciones"),
            ("Intervalo_Reinicio", "Mensual", "Criterio de clave de periodo (Mensual / Anual / Nunca)"),
            ("Dias_Reinicio", "30", "Días de vigencia por defecto en cotizaciones y control"),
            ("Carpeta_Base", _baseDocsFolder, "Carpeta local para PDFs y respaldos"),
            ("LogoFolder", Path.Combine(_baseDocsFolder, "Logo"), "Carpeta para el logo de la empresa"),
            ("IncluirFleteEnFactura", "No", "Si es 'Si', suma el flete al total de la factura; si es 'No', es costo interno"),
            ("PermitirVentaSinStock", "No", "Si es 'Si', permite vender aunque supere existencias con confirmación"),
            ("ComisionPorDefecto", "2", "Porcentaje de comisión por defecto (%)"),
            ("Moneda", "COP", "Moneda oficial del sistema (Pesos Colombianos)")
        };

        foreach (var cfg in configs)
        {
            using var insC = conn.CreateCommand();
            insC.Transaction = tx;
            insC.CommandText = @"
INSERT OR IGNORE INTO Configuracion (Parametro, Valor, Descripcion)
VALUES ($k, $v, $d);";
            insC.Parameters.AddWithValue("$k", cfg.K);
            insC.Parameters.AddWithValue("$v", cfg.V);
            insC.Parameters.AddWithValue("$d", cfg.D);
            insC.ExecuteNonQuery();
        }

        using var insConsec = conn.CreateCommand();
        insConsec.Transaction = tx;
        insConsec.CommandText = @"
INSERT OR IGNORE INTO ControlConsecutivos (Tipo, Prefijo, IntervaloReinicio, DiasReinicio, LastKey, LastCount)
VALUES ('FAC', 'FAC', 'Mensual', 30, '', 0),
       ('COT', 'COT', 'Mensual', 30, '', 0);";
        insConsec.ExecuteNonQuery();
    }

    // =========================================================================
    // 1. DASHBOARD
    // =========================================================================
    public DashboardKpis GetDashboardKpis(string? desdeIso = null, string? hastaIso = null)
    {
        using var conn = OpenConnection();
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string monthPrefix = DateTime.Now.ToString("yyyy-MM");

        var kpis = new DashboardKpis
        {
            ResumenProductos = GetProductos(false)
        };

        kpis.TotalUnidadesInventario = kpis.ResumenProductos.Sum(p => p.StockActual);
        kpis.TotalUnidadesSecado = kpis.ResumenProductos.Sum(p => p.StockSecado);
        kpis.TotalUnidadesHorno = kpis.ResumenProductos.Sum(p => p.StockHorno);
        kpis.TotalUnidadesPerdidas = kpis.ResumenProductos.Sum(p => p.DanadosAcumulados);

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COALESCE(SUM(TotalVentaCop), 0), COUNT(*) FROM Ventas WHERE Anulada = 0 AND FechaIso = $today;";
            cmd.Parameters.AddWithValue("$today", today);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                kpis.VentasHoyCop = r.GetInt64(0);
                kpis.CantidadVentasHoy = r.GetInt32(1);
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            if (!string.IsNullOrWhiteSpace(desdeIso) && !string.IsNullOrWhiteSpace(hastaIso))
            {
                cmd.CommandText = @"
SELECT COALESCE(SUM(TotalVentaCop), 0), COUNT(*), COALESCE(SUM(SaldoClienteCop), 0), COALESCE(SUM(GananciaNetaCop), 0)
FROM Ventas WHERE Anulada = 0 AND FechaIso >= $d AND FechaIso <= $h;";
                cmd.Parameters.AddWithValue("$d", desdeIso);
                cmd.Parameters.AddWithValue("$h", hastaIso);
            }
            else
            {
                cmd.CommandText = @"
SELECT COALESCE(SUM(TotalVentaCop), 0), COUNT(*), COALESCE(SUM(SaldoClienteCop), 0), COALESCE(SUM(GananciaNetaCop), 0)
FROM Ventas WHERE Anulada = 0 AND substr(FechaIso, 1, 7) = $m;";
                cmd.Parameters.AddWithValue("$m", monthPrefix);
            }
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                kpis.VentasMesCop = r.GetInt64(0);
                kpis.CantidadVentasMes = r.GetInt32(1);
                kpis.GananciaNetaMesCop = r.GetInt64(3);
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COALESCE(SUM(SaldoClienteCop), 0) FROM Ventas WHERE Anulada = 0 AND SaldoClienteCop > 0;";
            kpis.SaldoPendienteClientesCop = (long)(cmd.ExecuteScalar() ?? 0L);
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Ventas WHERE Anulada = 0 AND EstadoEntrega = 'Pendiente';";
            kpis.EntregasPendientes = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COALESCE(SUM(ComisionCop), 0) FROM Comisiones WHERE Estado = 'Pendiente';";
            kpis.ComisionesPendientesCop = (long)(cmd.ExecuteScalar() ?? 0L);
        }

        return kpis;
    }

    // =========================================================================
    // 2. PRODUCCIÓN (SECADO, HORNO, PÉRDIDAS)
    // =========================================================================
    public List<ProduccionMovimiento> GetProduccionMovimientos(string? etapa = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        if (string.IsNullOrWhiteSpace(etapa))
        {
            cmd.CommandText = "SELECT Id, FechaIso, ProductoId, ProductoNombre, TipoMovimiento, Etapa, CantidadUnidades, CausaPerdida, Responsable, Observaciones, Anulado FROM ProduccionMovimientos ORDER BY Id DESC;";
        }
        else
        {
            cmd.CommandText = "SELECT Id, FechaIso, ProductoId, ProductoNombre, TipoMovimiento, Etapa, CantidadUnidades, CausaPerdida, Responsable, Observaciones, Anulado FROM ProduccionMovimientos WHERE Etapa = $et ORDER BY Id DESC;";
            cmd.Parameters.AddWithValue("$et", etapa);
        }

        var list = new List<ProduccionMovimiento>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ProduccionMovimiento
            {
                Id = r.GetInt32(0),
                FechaIso = r.GetString(1),
                ProductoId = r.GetInt32(2),
                ProductoNombre = r.GetString(3),
                TipoMovimiento = r.GetString(4),
                Etapa = r.GetString(5),
                CantidadUnidades = r.GetInt64(6),
                CausaPerdida = r.GetString(7),
                Responsable = r.GetString(8),
                Observaciones = r.GetString(9),
                Anulado = r.GetInt32(10) == 1
            });
        }
        return list;
    }

    public void RegistrarEntradaSecado(int productoId, long cantidadUnidades, string responsable, string observaciones)
    {
        if (cantidadUnidades <= 0) throw new ArgumentException("La cantidad para ingreso a Secado debe ser mayor a cero.");
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        var prod = GetProductoInternal(conn, tx, productoId) ?? throw new InvalidOperationException("Producto no encontrado.");
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (var upd = conn.CreateCommand())
        {
            upd.Transaction = tx;
            upd.CommandText = "UPDATE Productos SET StockSecado = StockSecado + $c, FechaActualizacionIso = $f WHERE Id = $id;";
            upd.Parameters.AddWithValue("$c", cantidadUnidades);
            upd.Parameters.AddWithValue("$f", now);
            upd.Parameters.AddWithValue("$id", productoId);
            upd.ExecuteNonQuery();
        }

        InsertProduccionMov(conn, tx, now, prod.Id, prod.Nombre, "EntradaSecado", "Secado", cantidadUnidades, "", responsable, observaciones);
        tx.Commit();
    }

    public void TrasladarSecadoAHorno(int productoId, long cantidadUnidades, string responsable, string observaciones)
    {
        if (cantidadUnidades <= 0) throw new ArgumentException("La cantidad a trasladar a Horno debe ser mayor a cero.");
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        var prod = GetProductoInternal(conn, tx, productoId) ?? throw new InvalidOperationException("Producto no encontrado.");
        if (prod.StockSecado < cantidadUnidades)
            throw new InvalidOperationException($"No hay suficientes unidades de {prod.Nombre} en Secado ({prod.StockSecado:N0}) para trasladar {cantidadUnidades:N0} al Horno.");

        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        using (var upd = conn.CreateCommand())
        {
            upd.Transaction = tx;
            upd.CommandText = "UPDATE Productos SET StockSecado = StockSecado - $c, StockHorno = StockHorno + $c, FechaActualizacionIso = $f WHERE Id = $id;";
            upd.Parameters.AddWithValue("$c", cantidadUnidades);
            upd.Parameters.AddWithValue("$f", now);
            upd.Parameters.AddWithValue("$id", productoId);
            upd.ExecuteNonQuery();
        }

        InsertProduccionMov(conn, tx, now, prod.Id, prod.Nombre, "TrasladoAHorno", "Horno", cantidadUnidades, "", responsable, observaciones);
        tx.Commit();
    }

    public void FinalizarHornoAInventario(int productoId, long cantidadUnidades, string responsable, string observaciones)
    {
        if (cantidadUnidades <= 0) throw new ArgumentException("La cantidad que sale del Horno debe ser mayor a cero.");
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        var prod = GetProductoInternal(conn, tx, productoId) ?? throw new InvalidOperationException("Producto no encontrado.");
        if (prod.StockHorno < cantidadUnidades)
            throw new InvalidOperationException($"No hay suficientes unidades de {prod.Nombre} en Horno ({prod.StockHorno:N0}) para pasar {cantidadUnidades:N0} a Inventario Disponible.");

        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        long nuevoStock = prod.StockActual + cantidadUnidades;

        using (var upd = conn.CreateCommand())
        {
            upd.Transaction = tx;
            upd.CommandText = @"
UPDATE Productos
SET StockHorno = StockHorno - $c,
    StockActual = StockActual + $c,
    EntradasAcumuladas = EntradasAcumuladas + $c,
    FechaActualizacionIso = $f
WHERE Id = $id;";
            upd.Parameters.AddWithValue("$c", cantidadUnidades);
            upd.Parameters.AddWithValue("$f", now);
            upd.Parameters.AddWithValue("$id", productoId);
            upd.ExecuteNonQuery();
        }

        InsertProduccionMov(conn, tx, now, prod.Id, prod.Nombre, "SalidaHornoAInventario", "Inventario", cantidadUnidades, "", responsable, observaciones);
        InsertKardexInternal(conn, tx, now, prod.Id, prod.Nombre, "EntradaProduccion", "HORNO-COCCION", cantidadUnidades, 0, nuevoStock, prod.CostoUnitarioCop, responsable, observaciones);
        tx.Commit();
    }

    public void RegistrarPerdidaProduccion(int productoId, string etapa, long cantidadUnidades, string causa, string responsable, string observaciones)
    {
        if (cantidadUnidades <= 0) throw new ArgumentException("La cantidad dañada debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(causa)) throw new ArgumentException("Debe indicar la causa de la pérdida o rotura.");

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        var prod = GetProductoInternal(conn, tx, productoId) ?? throw new InvalidOperationException("Producto no encontrado.");
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (string.Equals(etapa, "Secado", StringComparison.OrdinalIgnoreCase))
        {
            if (prod.StockSecado < cantidadUnidades)
                throw new InvalidOperationException($"La pérdida ({cantidadUnidades:N0}) supera la cantidad actual en Secado ({prod.StockSecado:N0}).");

            using var upd = conn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE Productos SET StockSecado = StockSecado - $c, DanadosAcumulados = DanadosAcumulados + $c, FechaActualizacionIso = $f WHERE Id = $id;";
            upd.Parameters.AddWithValue("$c", cantidadUnidades);
            upd.Parameters.AddWithValue("$f", now);
            upd.Parameters.AddWithValue("$id", productoId);
            upd.ExecuteNonQuery();

            InsertProduccionMov(conn, tx, now, prod.Id, prod.Nombre, "PerdidaSecado", "Secado", cantidadUnidades, causa, responsable, observaciones);
        }
        else if (string.Equals(etapa, "Horno", StringComparison.OrdinalIgnoreCase))
        {
            if (prod.StockHorno < cantidadUnidades)
                throw new InvalidOperationException($"La pérdida ({cantidadUnidades:N0}) supera la cantidad actual en Horno ({prod.StockHorno:N0}).");

            using var upd = conn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE Productos SET StockHorno = StockHorno - $c, DanadosAcumulados = DanadosAcumulados + $c, FechaActualizacionIso = $f WHERE Id = $id;";
            upd.Parameters.AddWithValue("$c", cantidadUnidades);
            upd.Parameters.AddWithValue("$f", now);
            upd.Parameters.AddWithValue("$id", productoId);
            upd.ExecuteNonQuery();

            InsertProduccionMov(conn, tx, now, prod.Id, prod.Nombre, "PerdidaHorno", "Horno", cantidadUnidades, causa, responsable, observaciones);
        }
        else
        {
            if (prod.StockActual < cantidadUnidades)
                throw new InvalidOperationException($"La pérdida en patio/bodega ({cantidadUnidades:N0}) supera el inventario disponible ({prod.StockActual:N0}).");

            long nuevoSaldo = prod.StockActual - cantidadUnidades;
            using var upd = conn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE Productos SET StockActual = StockActual - $c, DanadosAcumulados = DanadosAcumulados + $c, FechaActualizacionIso = $f WHERE Id = $id;";
            upd.Parameters.AddWithValue("$c", cantidadUnidades);
            upd.Parameters.AddWithValue("$f", now);
            upd.Parameters.AddWithValue("$id", productoId);
            upd.ExecuteNonQuery();

            InsertProduccionMov(conn, tx, now, prod.Id, prod.Nombre, "PerdidaInventario", "Inventario", cantidadUnidades, causa, responsable, observaciones);
            InsertKardexInternal(conn, tx, now, prod.Id, prod.Nombre, "PerdidaRotura", causa, 0, cantidadUnidades, nuevoSaldo, prod.CostoUnitarioCop, responsable, observaciones);
        }

        tx.Commit();
    }

    private static void InsertProduccionMov(SqliteConnection conn, SqliteTransaction tx, string fechaIso, int prodId, string prodNombre, string tipo, string etapa, long cant, string causa, string resp, string obs)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
INSERT INTO ProduccionMovimientos (FechaIso, ProductoId, ProductoNombre, TipoMovimiento, Etapa, CantidadUnidades, CausaPerdida, Responsable, Observaciones, Anulado)
VALUES ($f, $pid, $pnom, $t, $e, $c, $cau, $r, $o, 0);";
        cmd.Parameters.AddWithValue("$f", fechaIso);
        cmd.Parameters.AddWithValue("$pid", prodId);
        cmd.Parameters.AddWithValue("$pnom", prodNombre);
        cmd.Parameters.AddWithValue("$t", tipo);
        cmd.Parameters.AddWithValue("$e", etapa);
        cmd.Parameters.AddWithValue("$c", cant);
        cmd.Parameters.AddWithValue("$cau", causa ?? "");
        cmd.Parameters.AddWithValue("$r", string.IsNullOrWhiteSpace(resp) ? "Operador" : resp.Trim());
        cmd.Parameters.AddWithValue("$o", obs ?? "");
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 3. INVENTARIO Y KARDEX
    // =========================================================================
    public List<Producto> GetProductos(bool soloActivos = false)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = soloActivos
            ? "SELECT Id, Codigo, Nombre, StockActual, StockSecado, StockHorno, EntradasAcumuladas, VentasAcumuladas, DanadosAcumulados, CostoUnitarioCop, PrecioUnitarioCop, PrecioMillarCop, Activo, FechaActualizacionIso FROM Productos WHERE Activo = 1 ORDER BY Id;"
            : "SELECT Id, Codigo, Nombre, StockActual, StockSecado, StockHorno, EntradasAcumuladas, VentasAcumuladas, DanadosAcumulados, CostoUnitarioCop, PrecioUnitarioCop, PrecioMillarCop, Activo, FechaActualizacionIso FROM Productos ORDER BY Id;";

        var list = new List<Producto>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(MapProducto(r));
        }
        return list;
    }

    public Producto? GetProductoById(int id)
    {
        using var conn = OpenConnection();
        return GetProductoInternal(conn, null, id);
    }

    private static Producto? GetProductoInternal(SqliteConnection conn, SqliteTransaction? tx, int id)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT Id, Codigo, Nombre, StockActual, StockSecado, StockHorno, EntradasAcumuladas, VentasAcumuladas, DanadosAcumulados, CostoUnitarioCop, PrecioUnitarioCop, PrecioMillarCop, Activo, FechaActualizacionIso FROM Productos WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapProducto(r) : null;
    }

    private static Producto MapProducto(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Codigo = r.GetString(1),
        Nombre = r.GetString(2),
        StockActual = r.GetInt64(3),
        StockSecado = r.GetInt64(4),
        StockHorno = r.GetInt64(5),
        EntradasAcumuladas = r.GetInt64(6),
        VentasAcumuladas = r.GetInt64(7),
        DanadosAcumulados = r.GetInt64(8),
        CostoUnitarioCop = r.GetInt64(9),
        PrecioUnitarioCop = r.GetInt64(10),
        PrecioMillarCop = r.GetInt64(11),
        Activo = r.GetInt32(12) == 1,
        FechaActualizacionIso = r.GetString(13)
    };

    public void GuardarProducto(Producto producto)
    {
        if (string.IsNullOrWhiteSpace(producto.Nombre))
            throw new ArgumentException("El nombre del producto es obligatorio.");
        if (producto.CostoUnitarioCop < 0 || producto.PrecioMillarCop < 0)
            throw new ArgumentException("Los costos y precios no pueden ser negativos.");

        if (producto.PrecioUnitarioCop <= 0 && producto.PrecioMillarCop > 0)
            producto.PrecioUnitarioCop = producto.PrecioMillarCop / 1000;
        if (producto.PrecioMillarCop <= 0 && producto.PrecioUnitarioCop > 0)
            producto.PrecioMillarCop = producto.PrecioUnitarioCop * 1000;

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (producto.Id == 0)
        {
            if (string.IsNullOrWhiteSpace(producto.Codigo))
            {
                producto.Codigo = "PRD-" + DateTime.Now.ToString("HHmmss");
            }
            using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = @"
INSERT INTO Productos (Codigo, Nombre, StockActual, StockSecado, StockHorno, EntradasAcumuladas, VentasAcumuladas, DanadosAcumulados, CostoUnitarioCop, PrecioUnitarioCop, PrecioMillarCop, Activo, FechaActualizacionIso)
VALUES ($cod, $nom, $stk, $sec, $hor, $stk, 0, 0, $costo, $punit, $pmil, $act, $fec);
SELECT last_insert_rowid();";
            ins.Parameters.AddWithValue("$cod", producto.Codigo.Trim());
            ins.Parameters.AddWithValue("$nom", producto.Nombre.Trim());
            ins.Parameters.AddWithValue("$stk", producto.StockActual);
            ins.Parameters.AddWithValue("$sec", producto.StockSecado);
            ins.Parameters.AddWithValue("$hor", producto.StockHorno);
            ins.Parameters.AddWithValue("$costo", producto.CostoUnitarioCop);
            ins.Parameters.AddWithValue("$punit", producto.PrecioUnitarioCop);
            ins.Parameters.AddWithValue("$pmil", producto.PrecioMillarCop);
            ins.Parameters.AddWithValue("$act", producto.Activo ? 1 : 0);
            ins.Parameters.AddWithValue("$fec", now);
            producto.Id = Convert.ToInt32(ins.ExecuteScalar());

            if (producto.StockActual > 0)
            {
                InsertKardexInternal(conn, tx, now, producto.Id, producto.Nombre, "SaldoInicial", "ALTA-PRODUCTO", producto.StockActual, 0, producto.StockActual, producto.CostoUnitarioCop, "Administrador", "Existencia inicial al crear producto");
            }
        }
        else
        {
            // Regla del Prompt Maestro: nunca modificar existencias sin generar movimiento Kardex.
            // GuardarProducto solo actualiza metadatos y precios; el stock se mueve por Kardex.
            using var upd = conn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = @"
UPDATE Productos
SET Codigo = $cod,
    Nombre = $nom,
    CostoUnitarioCop = $costo,
    PrecioUnitarioCop = $punit,
    PrecioMillarCop = $pmil,
    Activo = $act,
    FechaActualizacionIso = $fec
WHERE Id = $id;";
            upd.Parameters.AddWithValue("$cod", producto.Codigo.Trim());
            upd.Parameters.AddWithValue("$nom", producto.Nombre.Trim());
            upd.Parameters.AddWithValue("$costo", producto.CostoUnitarioCop);
            upd.Parameters.AddWithValue("$punit", producto.PrecioUnitarioCop);
            upd.Parameters.AddWithValue("$pmil", producto.PrecioMillarCop);
            upd.Parameters.AddWithValue("$act", producto.Activo ? 1 : 0);
            upd.Parameters.AddWithValue("$fec", now);
            upd.Parameters.AddWithValue("$id", producto.Id);
            upd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public void RegistrarAjusteInventario(int productoId, string concepto, long cantidadUnidades, string referencia, string responsable, string observaciones)
    {
        if (cantidadUnidades <= 0)
            throw new ArgumentException("La cantidad del movimiento de inventario debe ser mayor a cero.");

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        var prod = GetProductoInternal(conn, tx, productoId) ?? throw new InvalidOperationException("Producto no encontrado.");
        bool esSalida = concepto.Contains("Salida", StringComparison.OrdinalIgnoreCase)
                        || concepto.Contains("Negativo", StringComparison.OrdinalIgnoreCase)
                        || concepto.Contains("Perdida", StringComparison.OrdinalIgnoreCase);

        if (esSalida && prod.StockActual < cantidadUnidades)
            throw new InvalidOperationException($"Stock insuficiente de {prod.Nombre} (Actual: {prod.StockActual:N0}, Solicitado: {cantidadUnidades:N0}).");

        long entrada = esSalida ? 0 : cantidadUnidades;
        long salida = esSalida ? cantidadUnidades : 0;
        long nuevoSaldo = prod.StockActual + entrada - salida;
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (var upd = conn.CreateCommand())
        {
            upd.Transaction = tx;
            upd.CommandText = @"
UPDATE Productos
SET StockActual = $nuevo,
    EntradasAcumuladas = EntradasAcumuladas + $ent,
    FechaActualizacionIso = $fec
WHERE Id = $id;";
            upd.Parameters.AddWithValue("$nuevo", nuevoSaldo);
            upd.Parameters.AddWithValue("$ent", entrada);
            upd.Parameters.AddWithValue("$fec", now);
            upd.Parameters.AddWithValue("$id", productoId);
            upd.ExecuteNonQuery();
        }

        InsertKardexInternal(conn, tx, now, prod.Id, prod.Nombre, concepto, referencia, entrada, salida, nuevoSaldo, prod.CostoUnitarioCop, responsable, observaciones);
        tx.Commit();
    }

    public List<InventarioMovimiento> GetKardexMovimientos(int? productoId = null, string? desdeIso = null, string? hastaIso = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        var sb = new StringBuilder("SELECT Id, FechaIso, ProductoId, ProductoNombre, Concepto, Referencia, CantidadEntrada, CantidadSalida, SaldoPosterior, CostoUnitarioCop, Responsable, Observaciones FROM InventarioMovimientos WHERE 1=1");
        if (productoId.HasValue && productoId.Value > 0)
        {
            sb.Append(" AND ProductoId = $pid");
            cmd.Parameters.AddWithValue("$pid", productoId.Value);
        }
        if (!string.IsNullOrWhiteSpace(desdeIso))
        {
            sb.Append(" AND substr(FechaIso, 1, 10) >= $d");
            cmd.Parameters.AddWithValue("$d", desdeIso);
        }
        if (!string.IsNullOrWhiteSpace(hastaIso))
        {
            sb.Append(" AND substr(FechaIso, 1, 10) <= $h");
            cmd.Parameters.AddWithValue("$h", hastaIso);
        }
        sb.Append(" ORDER BY Id DESC;");
        cmd.CommandText = sb.ToString();

        var list = new List<InventarioMovimiento>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new InventarioMovimiento
            {
                Id = r.GetInt32(0),
                FechaIso = r.GetString(1),
                ProductoId = r.GetInt32(2),
                ProductoNombre = r.GetString(3),
                Concepto = r.GetString(4),
                Referencia = r.GetString(5),
                CantidadEntrada = r.GetInt64(6),
                CantidadSalida = r.GetInt64(7),
                SaldoPosterior = r.GetInt64(8),
                CostoUnitarioCop = r.GetInt64(9),
                Responsable = r.GetString(10),
                Observaciones = r.GetString(11)
            });
        }
        return list;
    }

    private static void InsertKardexInternal(SqliteConnection conn, SqliteTransaction tx, string fechaIso, int prodId, string prodNom, string concepto, string referencia, long entrada, long salida, long saldoPosterior, long costoUnit, string responsable, string observaciones)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
INSERT INTO InventarioMovimientos (FechaIso, ProductoId, ProductoNombre, Concepto, Referencia, CantidadEntrada, CantidadSalida, SaldoPosterior, CostoUnitarioCop, Responsable, Observaciones)
VALUES ($f, $pid, $pnom, $con, $ref, $ent, $sal, $sald, $cos, $resp, $obs);";
        cmd.Parameters.AddWithValue("$f", fechaIso);
        cmd.Parameters.AddWithValue("$pid", prodId);
        cmd.Parameters.AddWithValue("$pnom", prodNom);
        cmd.Parameters.AddWithValue("$con", concepto);
        cmd.Parameters.AddWithValue("$ref", referencia ?? "");
        cmd.Parameters.AddWithValue("$ent", entrada);
        cmd.Parameters.AddWithValue("$sal", salida);
        cmd.Parameters.AddWithValue("$sald", saldoPosterior);
        cmd.Parameters.AddWithValue("$cos", costoUnit);
        cmd.Parameters.AddWithValue("$resp", string.IsNullOrWhiteSpace(responsable) ? "Sistema" : responsable);
        cmd.Parameters.AddWithValue("$obs", observaciones ?? "");
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 4. VENTAS, PAGOS Y DEVOLUCIONES (CON TRANSACCIÓN ATÓMICA)
    // =========================================================================
    public Venta CrearVenta(Venta venta, long abonoInicialCop = 0, string medioPagoInicial = "Efectivo")
    {
        if (string.IsNullOrWhiteSpace(venta.ClienteNombre))
            throw new ArgumentException("El nombre del cliente es obligatorio.");
        if (venta.Detalles == null || venta.Detalles.Count == 0)
            throw new ArgumentException("Debe agregar al menos un producto a la venta.");

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        bool permitirSinStock = string.Equals(GetConfigInternal(conn, tx, "PermitirVentaSinStock", "No"), "Si", StringComparison.OrdinalIgnoreCase);
        bool incluirFleteCfg = string.Equals(GetConfigInternal(conn, tx, "IncluirFleteEnFactura", "No"), "Si", StringComparison.OrdinalIgnoreCase);
        venta.FleteIncluidoEnFactura = venta.FleteIncluidoEnFactura || incluirFleteCfg;

        if (string.IsNullOrWhiteSpace(venta.FacturaId))
        {
            venta.FacturaId = GenerarSiguienteConsecutivoInternal(conn, tx, "FAC");
        }

        long subtotalBruto = 0;
        long descuentoTotal = 0;
        long totalLineas = 0;

        foreach (var d in venta.Detalles)
        {
            if (d.Cantidad <= 0)
                throw new ArgumentException($"La cantidad del producto {d.ProductoNombre} debe ser mayor a cero.");
            if (d.DescuentoPct < 0 || d.DescuentoPct > 100)
                throw new ArgumentException("El porcentaje de descuento debe estar entre 0% y 100%.");

            var prod = GetProductoInternal(conn, tx, d.ProductoId)
                       ?? throw new InvalidOperationException($"Producto ID {d.ProductoId} no existe.");

            d.ProductoNombre = prod.Nombre;
            d.CostoUnitarioCop = prod.CostoUnitarioCop;
            bool esMillar = string.Equals(d.Unidad, "Millar", StringComparison.OrdinalIgnoreCase);
            d.Unidad = esMillar ? "Millar" : "Unidad";
            d.CantidadUnidadesFisicas = esMillar ? d.Cantidad * 1000L : d.Cantidad;

            if (d.PrecioPorUnidadMedidaCop <= 0)
            {
                d.PrecioPorUnidadMedidaCop = esMillar ? prod.PrecioMillarCop : prod.PrecioUnitarioCop;
            }

            if (!permitirSinStock && prod.StockActual < d.CantidadUnidadesFisicas)
            {
                throw new InvalidOperationException(
                    $"Inventario insuficiente para '{prod.Nombre}'. Disponible: {prod.StockActual:N0} unidades, Solicitado: {d.CantidadUnidadesFisicas:N0} unidades.");
            }

            long bruto = d.Cantidad * d.PrecioPorUnidadMedidaCop;
            long desc = (long)Math.Round(bruto * (d.DescuentoPct / 100m), 0, MidpointRounding.AwayFromZero);
            d.TotalLineaCop = bruto - desc;

            subtotalBruto += bruto;
            descuentoTotal += desc;
            totalLineas += d.TotalLineaCop;
        }

        venta.SubtotalCop = subtotalBruto;
        venta.DescuentoCop = descuentoTotal;
        venta.TotalVentaCop = totalLineas + (venta.FleteIncluidoEnFactura ? venta.FleteAsignadoCop : 0);

        // Resumen primera línea o combinado para hoja Ventas
        if (venta.Detalles.Count == 1)
        {
            var first = venta.Detalles[0];
            venta.TipoLadrilloResumen = first.ProductoNombre;
            venta.CantidadResumen = first.Cantidad;
            venta.UnidadResumen = first.Unidad;
            venta.PrecioUnitarioResumenCop = first.PrecioPorUnidadMedidaCop;
            venta.DescuentoPctResumen = first.DescuentoPct;
        }
        else
        {
            venta.TipoLadrilloResumen = string.Join(" + ", venta.Detalles.Select(x => $"{x.ProductoNombre} ({x.Cantidad} {x.Unidad})"));
            venta.CantidadResumen = venta.Detalles.Sum(x => x.CantidadUnidadesFisicas);
            venta.UnidadResumen = "Unidad";
            venta.PrecioUnitarioResumenCop = 0;
            venta.DescuentoPctResumen = 0;
        }

        // Vendedor y comisión (exactamente igual a la macro VBA + parametrizable)
        if (venta.VendedorId.HasValue && venta.VendedorId.Value > 0)
        {
            using var vCmd = conn.CreateCommand();
            vCmd.Transaction = tx;
            vCmd.CommandText = "SELECT Nombre, ComisionPct FROM Vendedores WHERE Id = $vid;";
            vCmd.Parameters.AddWithValue("$vid", venta.VendedorId.Value);
            using var vr = vCmd.ExecuteReader();
            if (vr.Read())
            {
                venta.VendedorNombre = vr.GetString(0);
                if (venta.ComisionPct <= 0)
                    venta.ComisionPct = (decimal)vr.GetDouble(1);
            }
        }
        else if (!string.IsNullOrWhiteSpace(venta.VendedorNombre))
        {
            using var vCmd = conn.CreateCommand();
            vCmd.Transaction = tx;
            vCmd.CommandText = "SELECT Id, ComisionPct FROM Vendedores WHERE lower(Nombre) = lower($nom);";
            vCmd.Parameters.AddWithValue("$nom", venta.VendedorNombre.Trim());
            using var vr = vCmd.ExecuteReader();
            if (vr.Read())
            {
                venta.VendedorId = vr.GetInt32(0);
                if (venta.ComisionPct <= 0)
                    venta.ComisionPct = (decimal)vr.GetDouble(1);
            }
        }

        venta.ComisionCop = (long)Math.Round(totalLineas * (venta.ComisionPct / 100m), 0, MidpointRounding.AwayFromZero);
        // Regla VBA: GananciaNeta = TotalProductos - ComisionCOP - Flete
        venta.GananciaNetaCop = totalLineas - venta.ComisionCop - venta.FleteAsignadoCop;

        if (abonoInicialCop < 0) abonoInicialCop = 0;
        if (abonoInicialCop > venta.TotalVentaCop) abonoInicialCop = venta.TotalVentaCop;

        venta.MontoPagadoCop = abonoInicialCop;
        venta.SaldoClienteCop = venta.TotalVentaCop - abonoInicialCop;
        venta.EstadoPago = venta.SaldoClienteCop == 0 ? "Pagado" : (abonoInicialCop > 0 ? "Parcial" : "Pendiente");
        if (string.IsNullOrWhiteSpace(venta.EstadoEntrega)) venta.EstadoEntrega = "Pendiente";
        if (string.IsNullOrWhiteSpace(venta.FechaIso)) venta.FechaIso = DateTime.Now.ToString("yyyy-MM-dd");
        if (string.IsNullOrWhiteSpace(venta.Hora)) venta.Hora = DateTime.Now.ToString("HH:mm:ss");

        // Upsert cliente
        int clienteId = UpsertClienteInternal(conn, tx, venta.ClienteNombre, venta.Telefono, venta.Direccion, venta.SaldoClienteCop);
        venta.ClienteId = clienteId;

        using (var insV = conn.CreateCommand())
        {
            insV.Transaction = tx;
            insV.CommandText = @"
INSERT INTO Ventas (
    FechaIso, Hora, FacturaId, ClienteId, ClienteNombre, Telefono, Direccion,
    TipoLadrilloResumen, CantidadResumen, UnidadResumen, PrecioUnitarioResumenCop, DescuentoPctResumen,
    SubtotalCop, DescuentoCop, TotalVentaCop, PlacaTransporte, ConductorNombre, FleteAsignadoCop, FleteIncluidoEnFactura,
    EstadoPago, MontoPagadoCop, SaldoClienteCop, EstadoEntrega,
    VendedorId, VendedorNombre, ComisionPct, ComisionCop, GananciaNetaCop,
    MotivoDevolucion, RetornadoABodega, FechaRetornoIso, RecibidoPor, Observaciones, CotizacionOrigenId, Anulada, MotivoAnulacion
) VALUES (
    $fec, $hor, $fac, $cid, $cnom, $tel, $dir,
    $tpo, $cant, $uni, $prec, $descPct,
    $sub, $desc, $tot, $pla, $cond, $fle, $finc,
    $epag, $pag, $sal, $eent,
    $vid, $vnom, $cpct, $ccop, $gnet,
    '', 'No', '', '', $obs, $cotId, 0, ''
);
SELECT last_insert_rowid();";
            insV.Parameters.AddWithValue("$fec", venta.FechaIso);
            insV.Parameters.AddWithValue("$hor", venta.Hora);
            insV.Parameters.AddWithValue("$fac", venta.FacturaId);
            insV.Parameters.AddWithValue("$cid", venta.ClienteId.Value);
            insV.Parameters.AddWithValue("$cnom", venta.ClienteNombre.Trim());
            insV.Parameters.AddWithValue("$tel", venta.Telefono ?? "");
            insV.Parameters.AddWithValue("$dir", venta.Direccion ?? "");
            insV.Parameters.AddWithValue("$tpo", venta.TipoLadrilloResumen);
            insV.Parameters.AddWithValue("$cant", venta.CantidadResumen);
            insV.Parameters.AddWithValue("$uni", venta.UnidadResumen);
            insV.Parameters.AddWithValue("$prec", venta.PrecioUnitarioResumenCop);
            insV.Parameters.AddWithValue("$descPct", (double)venta.DescuentoPctResumen);
            insV.Parameters.AddWithValue("$sub", venta.SubtotalCop);
            insV.Parameters.AddWithValue("$desc", venta.DescuentoCop);
            insV.Parameters.AddWithValue("$tot", venta.TotalVentaCop);
            insV.Parameters.AddWithValue("$pla", venta.PlacaTransporte ?? "");
            insV.Parameters.AddWithValue("$cond", venta.ConductorNombre ?? "");
            insV.Parameters.AddWithValue("$fle", venta.FleteAsignadoCop);
            insV.Parameters.AddWithValue("$finc", venta.FleteIncluidoEnFactura ? 1 : 0);
            insV.Parameters.AddWithValue("$epag", venta.EstadoPago);
            insV.Parameters.AddWithValue("$pag", venta.MontoPagadoCop);
            insV.Parameters.AddWithValue("$sal", venta.SaldoClienteCop);
            insV.Parameters.AddWithValue("$eent", venta.EstadoEntrega);
            insV.Parameters.AddWithValue("$vid", (object?)venta.VendedorId ?? DBNull.Value);
            insV.Parameters.AddWithValue("$vnom", venta.VendedorNombre ?? "");
            insV.Parameters.AddWithValue("$cpct", (double)venta.ComisionPct);
            insV.Parameters.AddWithValue("$ccop", venta.ComisionCop);
            insV.Parameters.AddWithValue("$gnet", venta.GananciaNetaCop);
            insV.Parameters.AddWithValue("$obs", venta.Observaciones ?? "");
            insV.Parameters.AddWithValue("$cotId", (object?)venta.CotizacionOrigenId ?? DBNull.Value);

            venta.Id = Convert.ToInt32(insV.ExecuteScalar());
        }

        string nowFull = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        foreach (var det in venta.Detalles)
        {
            det.VentaId = venta.Id;
            using (var insD = conn.CreateCommand())
            {
                insD.Transaction = tx;
                insD.CommandText = @"
INSERT INTO VentaDetalles (VentaId, ProductoId, ProductoNombre, Cantidad, Unidad, CantidadUnidadesFisicas, PrecioPorUnidadMedidaCop, DescuentoPct, TotalLineaCop, CostoUnitarioCop)
VALUES ($vid, $pid, $pnom, $cant, $uni, $ufis, $prec, $dpct, $tot, $cost);";
                insD.Parameters.AddWithValue("$vid", venta.Id);
                insD.Parameters.AddWithValue("$pid", det.ProductoId);
                insD.Parameters.AddWithValue("$pnom", det.ProductoNombre);
                insD.Parameters.AddWithValue("$cant", det.Cantidad);
                insD.Parameters.AddWithValue("$uni", det.Unidad);
                insD.Parameters.AddWithValue("$ufis", det.CantidadUnidadesFisicas);
                insD.Parameters.AddWithValue("$prec", det.PrecioPorUnidadMedidaCop);
                insD.Parameters.AddWithValue("$dpct", (double)det.DescuentoPct);
                insD.Parameters.AddWithValue("$tot", det.TotalLineaCop);
                insD.Parameters.AddWithValue("$cost", det.CostoUnitarioCop);
                insD.ExecuteNonQuery();
            }

            // Descontar inventario exactamente una vez y registrar en Kardex
            var prodActual = GetProductoInternal(conn, tx, det.ProductoId)!;
            long nuevoSaldo = Math.Max(0, prodActual.StockActual - det.CantidadUnidadesFisicas);
            using (var updP = conn.CreateCommand())
            {
                updP.Transaction = tx;
                updP.CommandText = @"
UPDATE Productos
SET StockActual = $nuevo,
    VentasAcumuladas = VentasAcumuladas + $ufis,
    FechaActualizacionIso = $fec
WHERE Id = $pid;";
                updP.Parameters.AddWithValue("$nuevo", nuevoSaldo);
                updP.Parameters.AddWithValue("$ufis", det.CantidadUnidadesFisicas);
                updP.Parameters.AddWithValue("$fec", nowFull);
                updP.Parameters.AddWithValue("$pid", det.ProductoId);
                updP.ExecuteNonQuery();
            }

            InsertKardexInternal(conn, tx, nowFull, det.ProductoId, det.ProductoNombre, "SalidaVenta", venta.FacturaId, 0, det.CantidadUnidadesFisicas, nuevoSaldo, det.CostoUnitarioCop, venta.VendedorNombre, $"Venta a {venta.ClienteNombre} ({det.Cantidad} {det.Unidad})");
        }

        // Crear registro de Factura vinculada
        long facturaIdNum;
        using (var insF = conn.CreateCommand())
        {
            insF.Transaction = tx;
            insF.CommandText = @"
INSERT INTO Facturas (FacturaCodigo, VentaId, FechaIso, ClienteNombre, Telefono, Direccion, VendedorNombre, SubtotalCop, DescuentoCop, FleteCop, TotalCop, Estado, RutaPdf, Observaciones)
VALUES ($cod, $vid, $fec, $cnom, $tel, $dir, $vnom, $sub, $desc, $fle, $tot, $est, '', $obs);
SELECT last_insert_rowid();";
            insF.Parameters.AddWithValue("$cod", venta.FacturaId);
            insF.Parameters.AddWithValue("$vid", venta.Id);
            insF.Parameters.AddWithValue("$fec", venta.FechaIso);
            insF.Parameters.AddWithValue("$cnom", venta.ClienteNombre);
            insF.Parameters.AddWithValue("$tel", venta.Telefono ?? "");
            insF.Parameters.AddWithValue("$dir", venta.Direccion ?? "");
            insF.Parameters.AddWithValue("$vnom", venta.VendedorNombre ?? "");
            insF.Parameters.AddWithValue("$sub", venta.SubtotalCop);
            insF.Parameters.AddWithValue("$desc", venta.DescuentoCop);
            insF.Parameters.AddWithValue("$fle", venta.FleteIncluidoEnFactura ? venta.FleteAsignadoCop : 0L);
            insF.Parameters.AddWithValue("$tot", venta.TotalVentaCop);
            insF.Parameters.AddWithValue("$est", venta.EstadoPago == "Pagado" ? "Pagada" : "Emitida");
            insF.Parameters.AddWithValue("$obs", venta.Observaciones ?? "");
            facturaIdNum = (long)insF.ExecuteScalar()!;
        }

        foreach (var det in venta.Detalles)
        {
            using var insFD = conn.CreateCommand();
            insFD.Transaction = tx;
            insFD.CommandText = @"
INSERT INTO FacturaDetalles (FacturaId, ProductoNombre, Cantidad, Unidad, PrecioUnitarioCop, DescuentoPct, TotalLineaCop)
VALUES ($fid, $pnom, $cant, $uni, $prec, $dpct, $tot);";
            insFD.Parameters.AddWithValue("$fid", facturaIdNum);
            insFD.Parameters.AddWithValue("$pnom", det.ProductoNombre);
            insFD.Parameters.AddWithValue("$cant", det.Cantidad);
            insFD.Parameters.AddWithValue("$uni", det.Unidad);
            insFD.Parameters.AddWithValue("$prec", det.PrecioPorUnidadMedidaCop);
            insFD.Parameters.AddWithValue("$dpct", (double)det.DescuentoPct);
            insFD.Parameters.AddWithValue("$tot", det.TotalLineaCop);
            insFD.ExecuteNonQuery();
        }

        // Registrar pago inicial si hubo abono
        if (abonoInicialCop > 0)
        {
            using var insPag = conn.CreateCommand();
            insPag.Transaction = tx;
            insPag.CommandText = @"
INSERT INTO Pagos (VentaId, FacturaId, FechaIso, ClienteNombre, MontoCop, SaldoAnteriorCop, SaldoNuevoCop, MedioPago, Referencia, Observaciones)
VALUES ($vid, $fac, $fec, $cnom, $mon, $sant, $snue, $med, 'PAGO-INICIAL', 'Abono al registrar la venta');";
            insPag.Parameters.AddWithValue("$vid", venta.Id);
            insPag.Parameters.AddWithValue("$fac", venta.FacturaId);
            insPag.Parameters.AddWithValue("$fec", nowFull);
            insPag.Parameters.AddWithValue("$cnom", venta.ClienteNombre);
            insPag.Parameters.AddWithValue("$mon", abonoInicialCop);
            insPag.Parameters.AddWithValue("$sant", venta.TotalVentaCop);
            insPag.Parameters.AddWithValue("$snue", venta.SaldoClienteCop);
            insPag.Parameters.AddWithValue("$med", medioPagoInicial);
            insPag.ExecuteNonQuery();
        }

        // Registrar comisión si aplica
        if (venta.ComisionCop > 0 && !string.IsNullOrWhiteSpace(venta.VendedorNombre))
        {
            using var insCom = conn.CreateCommand();
            insCom.Transaction = tx;
            insCom.CommandText = @"
INSERT INTO Comisiones (VentaId, FacturaId, VendedorId, VendedorNombre, FechaIso, BaseVentaCop, ComisionPct, ComisionCop, Estado)
VALUES ($vid, $fac, $vendId, $vnom, $fec, $base, $pct, $cop, 'Pendiente');";
            insCom.Parameters.AddWithValue("$vid", venta.Id);
            insCom.Parameters.AddWithValue("$fac", venta.FacturaId);
            insCom.Parameters.AddWithValue("$vendId", (object?)venta.VendedorId ?? DBNull.Value);
            insCom.Parameters.AddWithValue("$vnom", venta.VendedorNombre);
            insCom.Parameters.AddWithValue("$fec", venta.FechaIso);
            insCom.Parameters.AddWithValue("$base", totalLineas);
            insCom.Parameters.AddWithValue("$pct", (double)venta.ComisionPct);
            insCom.Parameters.AddWithValue("$cop", venta.ComisionCop);
            insCom.ExecuteNonQuery();
        }

        // Registrar flete en tabla Fletes si se indicó placa o costo de flete
        if (!string.IsNullOrWhiteSpace(venta.PlacaTransporte) || venta.FleteAsignadoCop > 0)
        {
            using var insFl = conn.CreateCommand();
            insFl.Transaction = tx;
            insFl.CommandText = @"
INSERT INTO Fletes (FechaSalidaIso, Placa, Conductor, Telefono, VentaId, ClientePedido, CostoFleteCop, EstadoEntrega, Observaciones)
VALUES ($fec, $pla, $cond, $tel, $vid, $ped, $cos, $est, 'Generado desde Venta');";
            insFl.Parameters.AddWithValue("$fec", venta.FechaIso);
            insFl.Parameters.AddWithValue("$pla", string.IsNullOrWhiteSpace(venta.PlacaTransporte) ? "SIN-PLACA" : venta.PlacaTransporte);
            insFl.Parameters.AddWithValue("$cond", venta.ConductorNombre ?? "");
            insFl.Parameters.AddWithValue("$tel", venta.Telefono ?? "");
            insFl.Parameters.AddWithValue("$vid", venta.Id);
            insFl.Parameters.AddWithValue("$ped", $"{venta.FacturaId} - {venta.ClienteNombre}");
            insFl.Parameters.AddWithValue("$cos", venta.FleteAsignadoCop);
            insFl.Parameters.AddWithValue("$est", venta.EstadoEntrega);
            insFl.ExecuteNonQuery();
        }

        tx.Commit();
        return venta;
    }

    public List<Venta> GetVentas(string? filtro = null, string? estadoPago = null, string? desdeIso = null, string? hastaIso = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        var sb = new StringBuilder(@"
SELECT Id, FechaIso, Hora, FacturaId, ClienteId, ClienteNombre, Telefono, Direccion,
       TipoLadrilloResumen, CantidadResumen, UnidadResumen, PrecioUnitarioResumenCop, DescuentoPctResumen,
       SubtotalCop, DescuentoCop, TotalVentaCop, PlacaTransporte, ConductorNombre, FleteAsignadoCop, FleteIncluidoEnFactura,
       EstadoPago, MontoPagadoCop, SaldoClienteCop, EstadoEntrega,
       VendedorId, VendedorNombre, ComisionPct, ComisionCop, GananciaNetaCop,
       MotivoDevolucion, RetornadoABodega, FechaRetornoIso, RecibidoPor, Observaciones, CotizacionOrigenId, Anulada, MotivoAnulacion
FROM Ventas WHERE 1=1");

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            sb.Append(" AND (ClienteNombre LIKE $f OR FacturaId LIKE $f OR TipoLadrilloResumen LIKE $f OR PlacaTransporte LIKE $f)");
            cmd.Parameters.AddWithValue("$f", "%" + filtro.Trim() + "%");
        }
        if (!string.IsNullOrWhiteSpace(estadoPago) && estadoPago != "Todos")
        {
            sb.Append(" AND EstadoPago = $ep");
            cmd.Parameters.AddWithValue("$ep", estadoPago);
        }
        if (!string.IsNullOrWhiteSpace(desdeIso))
        {
            sb.Append(" AND FechaIso >= $d");
            cmd.Parameters.AddWithValue("$d", desdeIso);
        }
        if (!string.IsNullOrWhiteSpace(hastaIso))
        {
            sb.Append(" AND FechaIso <= $h");
            cmd.Parameters.AddWithValue("$h", hastaIso);
        }
        sb.Append(" ORDER BY Id DESC;");
        cmd.CommandText = sb.ToString();

        var list = new List<Venta>();
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                list.Add(MapVenta(r));
            }
        }

        foreach (var v in list)
        {
            v.Detalles = GetVentaDetallesInternal(conn, v.Id);
        }
        return list;
    }

    public Venta? GetVentaById(int ventaId)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT Id, FechaIso, Hora, FacturaId, ClienteId, ClienteNombre, Telefono, Direccion,
       TipoLadrilloResumen, CantidadResumen, UnidadResumen, PrecioUnitarioResumenCop, DescuentoPctResumen,
       SubtotalCop, DescuentoCop, TotalVentaCop, PlacaTransporte, ConductorNombre, FleteAsignadoCop, FleteIncluidoEnFactura,
       EstadoPago, MontoPagadoCop, SaldoClienteCop, EstadoEntrega,
       VendedorId, VendedorNombre, ComisionPct, ComisionCop, GananciaNetaCop,
       MotivoDevolucion, RetornadoABodega, FechaRetornoIso, RecibidoPor, Observaciones, CotizacionOrigenId, Anulada, MotivoAnulacion
FROM Ventas WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", ventaId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var v = MapVenta(r);
        r.Close();
        v.Detalles = GetVentaDetallesInternal(conn, v.Id);
        return v;
    }

    private static Venta MapVenta(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        FechaIso = r.GetString(1),
        Hora = r.GetString(2),
        FacturaId = r.GetString(3),
        ClienteId = r.IsDBNull(4) ? null : r.GetInt32(4),
        ClienteNombre = r.GetString(5),
        Telefono = r.GetString(6),
        Direccion = r.GetString(7),
        TipoLadrilloResumen = r.GetString(8),
        CantidadResumen = r.GetInt64(9),
        UnidadResumen = r.GetString(10),
        PrecioUnitarioResumenCop = r.GetInt64(11),
        DescuentoPctResumen = (decimal)r.GetDouble(12),
        SubtotalCop = r.GetInt64(13),
        DescuentoCop = r.GetInt64(14),
        TotalVentaCop = r.GetInt64(15),
        PlacaTransporte = r.GetString(16),
        ConductorNombre = r.GetString(17),
        FleteAsignadoCop = r.GetInt64(18),
        FleteIncluidoEnFactura = r.GetInt32(19) == 1,
        EstadoPago = r.GetString(20),
        MontoPagadoCop = r.GetInt64(21),
        SaldoClienteCop = r.GetInt64(22),
        EstadoEntrega = r.GetString(23),
        VendedorId = r.IsDBNull(24) ? null : r.GetInt32(24),
        VendedorNombre = r.GetString(25),
        ComisionPct = (decimal)r.GetDouble(26),
        ComisionCop = r.GetInt64(27),
        GananciaNetaCop = r.GetInt64(28),
        MotivoDevolucion = r.GetString(29),
        RetornadoABodega = r.GetString(30),
        FechaRetornoIso = r.GetString(31),
        RecibidoPor = r.GetString(32),
        Observaciones = r.GetString(33),
        CotizacionOrigenId = r.IsDBNull(34) ? null : r.GetInt32(34),
        Anulada = r.GetInt32(35) == 1,
        MotivoAnulacion = r.GetString(36)
    };

    private static List<VentaDetalle> GetVentaDetallesInternal(SqliteConnection conn, int ventaId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, VentaId, ProductoId, ProductoNombre, Cantidad, Unidad, CantidadUnidadesFisicas, PrecioPorUnidadMedidaCop, DescuentoPct, TotalLineaCop, CostoUnitarioCop FROM VentaDetalles WHERE VentaId = $vid;";
        cmd.Parameters.AddWithValue("$vid", ventaId);
        var list = new List<VentaDetalle>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new VentaDetalle
            {
                Id = r.GetInt32(0),
                VentaId = r.GetInt32(1),
                ProductoId = r.GetInt32(2),
                ProductoNombre = r.GetString(3),
                Cantidad = r.GetInt64(4),
                Unidad = r.GetString(5),
                CantidadUnidadesFisicas = r.GetInt64(6),
                PrecioPorUnidadMedidaCop = r.GetInt64(7),
                DescuentoPct = (decimal)r.GetDouble(8),
                TotalLineaCop = r.GetInt64(9),
                CostoUnitarioCop = r.GetInt64(10)
            });
        }
        return list;
    }

    public void AnularVenta(int ventaId, string motivo, string usuario = "Administrador")
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Debe especificar el motivo de anulación.");

        var venta = GetVentaById(ventaId) ?? throw new InvalidOperationException("Venta no encontrada.");
        if (venta.Anulada)
            throw new InvalidOperationException("La venta ya se encuentra anulada.");

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (var updV = conn.CreateCommand())
        {
            updV.Transaction = tx;
            updV.CommandText = "UPDATE Ventas SET Anulada = 1, EstadoPago = 'Anulado', EstadoEntrega = 'Anulado', SaldoClienteCop = 0, MotivoAnulacion = $mot WHERE Id = $id;";
            updV.Parameters.AddWithValue("$mot", motivo.Trim());
            updV.Parameters.AddWithValue("$id", ventaId);
            updV.ExecuteNonQuery();
        }

        using (var updF = conn.CreateCommand())
        {
            updF.Transaction = tx;
            updF.CommandText = "UPDATE Facturas SET Estado = 'Anulada' WHERE VentaId = $id;";
            updF.Parameters.AddWithValue("$id", ventaId);
            updF.ExecuteNonQuery();
        }

        using (var updC = conn.CreateCommand())
        {
            updC.Transaction = tx;
            updC.CommandText = "UPDATE Comisiones SET Estado = 'Anulada' WHERE VentaId = $id;";
            updC.Parameters.AddWithValue("$id", ventaId);
            updC.ExecuteNonQuery();
        }

        // Revertir inventario en Kardex
        foreach (var det in venta.Detalles)
        {
            var prod = GetProductoInternal(conn, tx, det.ProductoId)!;
            long nuevoSaldo = prod.StockActual + det.CantidadUnidadesFisicas;
            using (var updP = conn.CreateCommand())
            {
                updP.Transaction = tx;
                updP.CommandText = "UPDATE Productos SET StockActual = $nuevo, VentasAcumuladas = MAX(0, VentasAcumuladas - $ufis), FechaActualizacionIso = $fec WHERE Id = $pid;";
                updP.Parameters.AddWithValue("$nuevo", nuevoSaldo);
                updP.Parameters.AddWithValue("$ufis", det.CantidadUnidadesFisicas);
                updP.Parameters.AddWithValue("$fec", now);
                updP.Parameters.AddWithValue("$pid", det.ProductoId);
                updP.ExecuteNonQuery();
            }

            InsertKardexInternal(conn, tx, now, det.ProductoId, det.ProductoNombre, "AnulacionVenta", venta.FacturaId, det.CantidadUnidadesFisicas, 0, nuevoSaldo, det.CostoUnitarioCop, usuario, $"Anulación venta: {motivo}");
        }

        tx.Commit();
    }

    public Pago RegistrarPago(int ventaId, long montoCop, string medioPago, string referencia, string observaciones)
    {
        if (montoCop <= 0) throw new ArgumentException("El monto del abono/pago debe ser mayor a cero.");

        var venta = GetVentaById(ventaId) ?? throw new InvalidOperationException("Venta no encontrada.");
        if (venta.Anulada) throw new InvalidOperationException("No se puede registrar pagos sobre una venta anulada.");
        if (venta.SaldoClienteCop <= 0) throw new InvalidOperationException("Esta venta ya se encuentra totalmente pagada.");
        if (montoCop > venta.SaldoClienteCop)
            throw new InvalidOperationException($"El monto (${montoCop:N0}) supera el saldo pendiente (${venta.SaldoClienteCop:N0}).");

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        long saldoAnt = venta.SaldoClienteCop;
        long saldoNuevo = saldoAnt - montoCop;
        long nuevoPagado = venta.MontoPagadoCop + montoCop;
        string nuevoEstado = saldoNuevo == 0 ? "Pagado" : "Parcial";
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (var updV = conn.CreateCommand())
        {
            updV.Transaction = tx;
            updV.CommandText = "UPDATE Ventas SET MontoPagadoCop = $pag, SaldoClienteCop = $sal, EstadoPago = $est WHERE Id = $id;";
            updV.Parameters.AddWithValue("$pag", nuevoPagado);
            updV.Parameters.AddWithValue("$sal", saldoNuevo);
            updV.Parameters.AddWithValue("$est", nuevoEstado);
            updV.Parameters.AddWithValue("$id", ventaId);
            updV.ExecuteNonQuery();
        }

        if (saldoNuevo == 0)
        {
            using var updF = conn.CreateCommand();
            updF.Transaction = tx;
            updF.CommandText = "UPDATE Facturas SET Estado = 'Pagada' WHERE VentaId = $id;";
            updF.Parameters.AddWithValue("$id", ventaId);
            updF.ExecuteNonQuery();
        }

        var pago = new Pago
        {
            VentaId = ventaId,
            FacturaId = venta.FacturaId,
            FechaIso = now,
            ClienteNombre = venta.ClienteNombre,
            MontoCop = montoCop,
            SaldoAnteriorCop = saldoAnt,
            SaldoNuevoCop = saldoNuevo,
            MedioPago = string.IsNullOrWhiteSpace(medioPago) ? "Efectivo" : medioPago,
            Referencia = referencia ?? "",
            Observaciones = observaciones ?? ""
        };

        using (var insP = conn.CreateCommand())
        {
            insP.Transaction = tx;
            insP.CommandText = @"
INSERT INTO Pagos (VentaId, FacturaId, FechaIso, ClienteNombre, MontoCop, SaldoAnteriorCop, SaldoNuevoCop, MedioPago, Referencia, Observaciones)
VALUES ($vid, $fac, $fec, $cnom, $mon, $sant, $snue, $med, $ref, $obs);
SELECT last_insert_rowid();";
            insP.Parameters.AddWithValue("$vid", pago.VentaId);
            insP.Parameters.AddWithValue("$fac", pago.FacturaId);
            insP.Parameters.AddWithValue("$fec", pago.FechaIso);
            insP.Parameters.AddWithValue("$cnom", pago.ClienteNombre);
            insP.Parameters.AddWithValue("$mon", pago.MontoCop);
            insP.Parameters.AddWithValue("$sant", pago.SaldoAnteriorCop);
            insP.Parameters.AddWithValue("$snue", pago.SaldoNuevoCop);
            insP.Parameters.AddWithValue("$med", pago.MedioPago);
            insP.Parameters.AddWithValue("$ref", pago.Referencia);
            insP.Parameters.AddWithValue("$obs", pago.Observaciones);
            pago.Id = Convert.ToInt32(insP.ExecuteScalar());
        }

        tx.Commit();
        return pago;
    }

    public List<Pago> GetPagos(int? ventaId = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        if (ventaId.HasValue)
        {
            cmd.CommandText = "SELECT Id, VentaId, FacturaId, FechaIso, ClienteNombre, MontoCop, SaldoAnteriorCop, SaldoNuevoCop, MedioPago, Referencia, Observaciones FROM Pagos WHERE VentaId = $vid ORDER BY Id DESC;";
            cmd.Parameters.AddWithValue("$vid", ventaId.Value);
        }
        else
        {
            cmd.CommandText = "SELECT Id, VentaId, FacturaId, FechaIso, ClienteNombre, MontoCop, SaldoAnteriorCop, SaldoNuevoCop, MedioPago, Referencia, Observaciones FROM Pagos ORDER BY Id DESC;";
        }

        var list = new List<Pago>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Pago
            {
                Id = r.GetInt32(0),
                VentaId = r.GetInt32(1),
                FacturaId = r.GetString(2),
                FechaIso = r.GetString(3),
                ClienteNombre = r.GetString(4),
                MontoCop = r.GetInt64(5),
                SaldoAnteriorCop = r.GetInt64(6),
                SaldoNuevoCop = r.GetInt64(7),
                MedioPago = r.GetString(8),
                Referencia = r.GetString(9),
                Observaciones = r.GetString(10)
            });
        }
        return list;
    }

    public Devolucion RegistrarDevolucion(Devolucion devolucion)
    {
        if (devolucion.CantidadUnidades <= 0)
            throw new ArgumentException("La cantidad devuelta debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(devolucion.Motivo))
            throw new ArgumentException("Debe indicar el motivo de la devolución.");

        var venta = GetVentaById(devolucion.VentaId) ?? throw new InvalidOperationException("Venta asociada no encontrada.");
        if (venta.Anulada) throw new InvalidOperationException("No se puede registrar devoluciones sobre una venta anulada.");

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        var prod = GetProductoInternal(conn, tx, devolucion.ProductoId)
                   ?? throw new InvalidOperationException("Producto devuelto no encontrado.");

        devolucion.FacturaId = venta.FacturaId;
        devolucion.ClienteNombre = venta.ClienteNombre;
        devolucion.ProductoNombre = prod.Nombre;
        if (string.IsNullOrWhiteSpace(devolucion.FechaRetornoIso))
            devolucion.FechaRetornoIso = DateTime.Now.ToString("yyyy-MM-dd");

        // CRITERIO DE ACEPTACIÓN #4:
        // Una devolución solo suma inventario si fue aprobada y el producto es apto.
        bool esApto = string.Equals(devolucion.EstadoProducto, "Apto", StringComparison.OrdinalIgnoreCase);
        devolucion.RetornadoABodega = esApto && devolucion.AprobadaParaReingreso;

        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (devolucion.RetornadoABodega)
        {
            long nuevoSaldo = prod.StockActual + devolucion.CantidadUnidades;
            using (var updP = conn.CreateCommand())
            {
                updP.Transaction = tx;
                updP.CommandText = "UPDATE Productos SET StockActual = $nuevo, FechaActualizacionIso = $fec WHERE Id = $pid;";
                updP.Parameters.AddWithValue("$nuevo", nuevoSaldo);
                updP.Parameters.AddWithValue("$fec", now);
                updP.Parameters.AddWithValue("$pid", prod.Id);
                updP.ExecuteNonQuery();
            }

            InsertKardexInternal(conn, tx, now, prod.Id, prod.Nombre, "ReingresoDevolucion", venta.FacturaId,
                devolucion.CantidadUnidades, 0, nuevoSaldo, prod.CostoUnitarioCop, devolucion.RecibidoPor,
                $"Devolución aprobada ({devolucion.Motivo})");
        }
        else if (!esApto)
        {
            using var updDan = conn.CreateCommand();
            updDan.Transaction = tx;
            updDan.CommandText = "UPDATE Productos SET DanadosAcumulados = DanadosAcumulados + $c, FechaActualizacionIso = $fec WHERE Id = $pid;";
            updDan.Parameters.AddWithValue("$c", devolucion.CantidadUnidades);
            updDan.Parameters.AddWithValue("$fec", now);
            updDan.Parameters.AddWithValue("$pid", prod.Id);
            updDan.ExecuteNonQuery();
        }

        // Actualizar campos de devolución en la fila de Venta para trazabilidad con hoja Excel
        using (var updV = conn.CreateCommand())
        {
            updV.Transaction = tx;
            updV.CommandText = @"
UPDATE Ventas
SET MotivoDevolucion = $mot,
    RetornadoABodega = $ret,
    FechaRetornoIso = $fdev,
    RecibidoPor = $rec,
    SaldoClienteCop = MAX(0, SaldoClienteCop - $valDesc)
WHERE Id = $vid;";
            updV.Parameters.AddWithValue("$mot", devolucion.Motivo);
            updV.Parameters.AddWithValue("$ret", devolucion.RetornadoABodega ? "Si" : "No");
            updV.Parameters.AddWithValue("$fdev", devolucion.FechaRetornoIso);
            updV.Parameters.AddWithValue("$rec", devolucion.RecibidoPor ?? "");
            updV.Parameters.AddWithValue("$valDesc", devolucion.ValorDescontadoSaldoCop);
            updV.Parameters.AddWithValue("$vid", venta.Id);
            updV.ExecuteNonQuery();
        }

        using (var insD = conn.CreateCommand())
        {
            insD.Transaction = tx;
            insD.CommandText = @"
INSERT INTO Devoluciones (VentaId, FacturaId, FechaRetornoIso, ClienteNombre, ProductoId, ProductoNombre, CantidadUnidades, Motivo, EstadoProducto, AprobadaParaReingreso, RetornadoABodega, RecibidoPor, ValorDescontadoSaldoCop, Observaciones)
VALUES ($vid, $fac, $fec, $cnom, $pid, $pnom, $cant, $mot, $est, $apr, $ret, $rec, $val, $obs);
SELECT last_insert_rowid();";
            insD.Parameters.AddWithValue("$vid", devolucion.VentaId);
            insD.Parameters.AddWithValue("$fac", devolucion.FacturaId);
            insD.Parameters.AddWithValue("$fec", devolucion.FechaRetornoIso);
            insD.Parameters.AddWithValue("$cnom", devolucion.ClienteNombre);
            insD.Parameters.AddWithValue("$pid", devolucion.ProductoId);
            insD.Parameters.AddWithValue("$pnom", devolucion.ProductoNombre);
            insD.Parameters.AddWithValue("$cant", devolucion.CantidadUnidades);
            insD.Parameters.AddWithValue("$mot", devolucion.Motivo);
            insD.Parameters.AddWithValue("$est", devolucion.EstadoProducto);
            insD.Parameters.AddWithValue("$apr", devolucion.AprobadaParaReingreso ? 1 : 0);
            insD.Parameters.AddWithValue("$ret", devolucion.RetornadoABodega ? 1 : 0);
            insD.Parameters.AddWithValue("$rec", devolucion.RecibidoPor ?? "");
            insD.Parameters.AddWithValue("$val", devolucion.ValorDescontadoSaldoCop);
            insD.Parameters.AddWithValue("$obs", devolucion.Observaciones ?? "");
            devolucion.Id = Convert.ToInt32(insD.ExecuteScalar());
        }

        tx.Commit();
        return devolucion;
    }

    public List<Devolucion> GetDevoluciones(int? ventaId = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        if (ventaId.HasValue)
        {
            cmd.CommandText = "SELECT Id, VentaId, FacturaId, FechaRetornoIso, ClienteNombre, ProductoId, ProductoNombre, CantidadUnidades, Motivo, EstadoProducto, AprobadaParaReingreso, RetornadoABodega, RecibidoPor, ValorDescontadoSaldoCop, Observaciones FROM Devoluciones WHERE VentaId = $vid ORDER BY Id DESC;";
            cmd.Parameters.AddWithValue("$vid", ventaId.Value);
        }
        else
        {
            cmd.CommandText = "SELECT Id, VentaId, FacturaId, FechaRetornoIso, ClienteNombre, ProductoId, ProductoNombre, CantidadUnidades, Motivo, EstadoProducto, AprobadaParaReingreso, RetornadoABodega, RecibidoPor, ValorDescontadoSaldoCop, Observaciones FROM Devoluciones ORDER BY Id DESC;";
        }

        var list = new List<Devolucion>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Devolucion
            {
                Id = r.GetInt32(0),
                VentaId = r.GetInt32(1),
                FacturaId = r.GetString(2),
                FechaRetornoIso = r.GetString(3),
                ClienteNombre = r.GetString(4),
                ProductoId = r.GetInt32(5),
                ProductoNombre = r.GetString(6),
                CantidadUnidades = r.GetInt64(7),
                Motivo = r.GetString(8),
                EstadoProducto = r.GetString(9),
                AprobadaParaReingreso = r.GetInt32(10) == 1,
                RetornadoABodega = r.GetInt32(11) == 1,
                RecibidoPor = r.GetString(12),
                ValorDescontadoSaldoCop = r.GetInt64(13),
                Observaciones = r.GetString(14)
            });
        }
        return list;
    }

    // =========================================================================
    // 5. FACTURACIÓN Y PDF
    // =========================================================================
    public List<Factura> GetFacturas(string? filtro = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        if (string.IsNullOrWhiteSpace(filtro))
        {
            cmd.CommandText = "SELECT Id, FacturaCodigo, VentaId, FechaIso, ClienteNombre, Telefono, Direccion, VendedorNombre, SubtotalCop, DescuentoCop, FleteCop, TotalCop, Estado, RutaPdf, Observaciones FROM Facturas ORDER BY Id DESC;";
        }
        else
        {
            cmd.CommandText = "SELECT Id, FacturaCodigo, VentaId, FechaIso, ClienteNombre, Telefono, Direccion, VendedorNombre, SubtotalCop, DescuentoCop, FleteCop, TotalCop, Estado, RutaPdf, Observaciones FROM Facturas WHERE FacturaCodigo LIKE $f OR ClienteNombre LIKE $f ORDER BY Id DESC;";
            cmd.Parameters.AddWithValue("$f", "%" + filtro.Trim() + "%");
        }

        var list = new List<Factura>();
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                list.Add(MapFactura(r));
            }
        }

        foreach (var f in list)
        {
            f.Detalles = GetFacturaDetallesInternal(conn, f.Id);
        }
        return list;
    }

    public Factura? GetFacturaById(int facturaId)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, FacturaCodigo, VentaId, FechaIso, ClienteNombre, Telefono, Direccion, VendedorNombre, SubtotalCop, DescuentoCop, FleteCop, TotalCop, Estado, RutaPdf, Observaciones FROM Facturas WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", facturaId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var f = MapFactura(r);
        r.Close();
        f.Detalles = GetFacturaDetallesInternal(conn, f.Id);
        return f;
    }

    private static Factura MapFactura(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        FacturaCodigo = r.GetString(1),
        VentaId = r.GetInt32(2),
        FechaIso = r.GetString(3),
        ClienteNombre = r.GetString(4),
        Telefono = r.GetString(5),
        Direccion = r.GetString(6),
        VendedorNombre = r.GetString(7),
        SubtotalCop = r.GetInt64(8),
        DescuentoCop = r.GetInt64(9),
        FleteCop = r.GetInt64(10),
        TotalCop = r.GetInt64(11),
        Estado = r.GetString(12),
        RutaPdf = r.GetString(13),
        Observaciones = r.GetString(14)
    };

    private static List<FacturaDetalle> GetFacturaDetallesInternal(SqliteConnection conn, int facturaId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, FacturaId, ProductoNombre, Cantidad, Unidad, PrecioUnitarioCop, DescuentoPct, TotalLineaCop FROM FacturaDetalles WHERE FacturaId = $fid;";
        cmd.Parameters.AddWithValue("$fid", facturaId);
        var list = new List<FacturaDetalle>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new FacturaDetalle
            {
                Id = r.GetInt32(0),
                FacturaId = r.GetInt32(1),
                ProductoNombre = r.GetString(2),
                Cantidad = r.GetInt64(3),
                Unidad = r.GetString(4),
                PrecioUnitarioCop = r.GetInt64(5),
                DescuentoPct = (decimal)r.GetDouble(6),
                TotalLineaCop = r.GetInt64(7)
            });
        }
        return list;
    }

    public string GenerarPdfFactura(int facturaId, string? rutaDestino = null)
    {
        var fac = GetFacturaById(facturaId) ?? throw new InvalidOperationException("Factura no encontrada.");
        var venta = GetVentaById(fac.VentaId);

        string carpetaBase = GetConfigValor("Carpeta_Base", _baseDocsFolder);
        string ruta = rutaDestino ?? Path.Combine(carpetaBase, "Facturas", $"{fac.FacturaCodigo}.pdf");

        string emp = GetConfigValor("Nombre_Ladrillera", "LADRILLERA SAN ANDRES S.A.S.");
        string nit = GetConfigValor("NIT_Ladrillera", "900.123.456-7");
        string dir = GetConfigValor("Direccion_Ladrillera", "Planta Principal");
        string tel = GetConfigValor("Telefono_Ladrillera", "300 000 0000");

        PdfDocumentGenerator.GenerarFacturaPdf(fac, venta, emp, nit, dir, tel, ruta);

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Facturas SET RutaPdf = $r WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$r", ruta);
        cmd.Parameters.AddWithValue("$id", facturaId);
        cmd.ExecuteNonQuery();

        return ruta;
    }

    // =========================================================================
    // 6. COTIZACIONES Y CONVERSIÓN A VENTA SIN DUPLICAR
    // =========================================================================
    public Cotizacion CrearCotizacion(Cotizacion cot)
    {
        if (string.IsNullOrWhiteSpace(cot.ClienteNombre))
            throw new ArgumentException("El nombre del cliente es obligatorio.");
        if (cot.Detalles == null || cot.Detalles.Count == 0)
            throw new ArgumentException("Debe agregar al menos un producto a la cotización.");

        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        if (string.IsNullOrWhiteSpace(cot.CotizacionCodigo))
        {
            cot.CotizacionCodigo = GenerarSiguienteConsecutivoInternal(conn, tx, "COT");
        }

        long total = 0;
        foreach (var d in cot.Detalles)
        {
            if (d.Cantidad <= 0) throw new ArgumentException("La cantidad cotizada debe ser mayor a cero.");
            var prod = GetProductoInternal(conn, tx, d.ProductoId)
                       ?? throw new InvalidOperationException("Producto no encontrado.");
            d.ProductoNombre = prod.Nombre;
            bool esMillar = string.Equals(d.Unidad, "Millar", StringComparison.OrdinalIgnoreCase);
            d.Unidad = esMillar ? "Millar" : "Unidad";
            d.CantidadUnidadesFisicas = esMillar ? d.Cantidad * 1000L : d.Cantidad;
            if (d.PrecioUnitarioCop <= 0)
                d.PrecioUnitarioCop = esMillar ? prod.PrecioMillarCop : prod.PrecioUnitarioCop;

            long bruto = d.Cantidad * d.PrecioUnitarioCop;
            long desc = (long)Math.Round(bruto * (d.DescuentoPct / 100m), 0, MidpointRounding.AwayFromZero);
            d.TotalLineaCop = bruto - desc;
            total += d.TotalLineaCop;
        }

        cot.TotalCop = total;
        var first = cot.Detalles[0];
        cot.TipoLadrilloResumen = cot.Detalles.Count == 1
            ? first.ProductoNombre
            : string.Join(" + ", cot.Detalles.Select(x => $"{x.ProductoNombre} ({x.Cantidad} {x.Unidad})"));
        cot.CantidadResumen = first.Cantidad;
        cot.UnidadResumen = first.Unidad;
        cot.PrecioUnitarioResumenCop = first.PrecioUnitarioCop;
        cot.DescuentoPctResumen = first.DescuentoPct;

        if (string.IsNullOrWhiteSpace(cot.FechaIso))
            cot.FechaIso = DateTime.Now.ToString("yyyy-MM-dd");
        if (string.IsNullOrWhiteSpace(cot.VigenteHastaIso))
            cot.VigenteHastaIso = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
        if (string.IsNullOrWhiteSpace(cot.Estado))
            cot.Estado = "Enviada";
        cot.ConvertidaEnVenta = "No";

        using (var ins = conn.CreateCommand())
        {
            ins.Transaction = tx;
            ins.CommandText = @"
INSERT INTO Cotizaciones (CotizacionCodigo, FechaIso, ClienteNombre, Telefono, TipoLadrilloResumen, CantidadResumen, UnidadResumen, PrecioUnitarioResumenCop, DescuentoPctResumen, TotalCop, Estado, VigenteHastaIso, ConvertidaEnVenta, Observaciones)
VALUES ($cod, $fec, $cnom, $tel, $tpo, $cant, $uni, $prec, $dpct, $tot, $est, $vig, 'No', $obs);
SELECT last_insert_rowid();";
            ins.Parameters.AddWithValue("$cod", cot.CotizacionCodigo);
            ins.Parameters.AddWithValue("$fec", cot.FechaIso);
            ins.Parameters.AddWithValue("$cnom", cot.ClienteNombre.Trim());
            ins.Parameters.AddWithValue("$tel", cot.Telefono ?? "");
            ins.Parameters.AddWithValue("$tpo", cot.TipoLadrilloResumen);
            ins.Parameters.AddWithValue("$cant", cot.CantidadResumen);
            ins.Parameters.AddWithValue("$uni", cot.UnidadResumen);
            ins.Parameters.AddWithValue("$prec", cot.PrecioUnitarioResumenCop);
            ins.Parameters.AddWithValue("$dpct", (double)cot.DescuentoPctResumen);
            ins.Parameters.AddWithValue("$tot", cot.TotalCop);
            ins.Parameters.AddWithValue("$est", cot.Estado);
            ins.Parameters.AddWithValue("$vig", cot.VigenteHastaIso);
            ins.Parameters.AddWithValue("$obs", cot.Observaciones ?? "");
            cot.Id = Convert.ToInt32(ins.ExecuteScalar());
        }

        foreach (var d in cot.Detalles)
        {
            d.CotizacionId = cot.Id;
            using var insD = conn.CreateCommand();
            insD.Transaction = tx;
            insD.CommandText = @"
INSERT INTO CotizacionDetalles (CotizacionId, ProductoId, ProductoNombre, Cantidad, Unidad, CantidadUnidadesFisicas, PrecioUnitarioCop, DescuentoPct, TotalLineaCop)
VALUES ($cid, $pid, $pnom, $cant, $uni, $ufis, $prec, $dpct, $tot);";
            insD.Parameters.AddWithValue("$cid", cot.Id);
            insD.Parameters.AddWithValue("$pid", d.ProductoId);
            insD.Parameters.AddWithValue("$pnom", d.ProductoNombre);
            insD.Parameters.AddWithValue("$cant", d.Cantidad);
            insD.Parameters.AddWithValue("$uni", d.Unidad);
            insD.Parameters.AddWithValue("$ufis", d.CantidadUnidadesFisicas);
            insD.Parameters.AddWithValue("$prec", d.PrecioUnitarioCop);
            insD.Parameters.AddWithValue("$dpct", (double)d.DescuentoPct);
            insD.Parameters.AddWithValue("$tot", d.TotalLineaCop);
            insD.ExecuteNonQuery();
        }

        tx.Commit();
        return cot;
    }

    public List<Cotizacion> GetCotizaciones(string? estado = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        if (string.IsNullOrWhiteSpace(estado) || estado == "Todas")
        {
            cmd.CommandText = "SELECT Id, CotizacionCodigo, FechaIso, ClienteNombre, Telefono, TipoLadrilloResumen, CantidadResumen, UnidadResumen, PrecioUnitarioResumenCop, DescuentoPctResumen, TotalCop, Estado, VigenteHastaIso, ConvertidaEnVenta, VentaGeneradaId, RutaPdf, Observaciones FROM Cotizaciones ORDER BY Id DESC;";
        }
        else
        {
            cmd.CommandText = "SELECT Id, CotizacionCodigo, FechaIso, ClienteNombre, Telefono, TipoLadrilloResumen, CantidadResumen, UnidadResumen, PrecioUnitarioResumenCop, DescuentoPctResumen, TotalCop, Estado, VigenteHastaIso, ConvertidaEnVenta, VentaGeneradaId, RutaPdf, Observaciones FROM Cotizaciones WHERE Estado = $e ORDER BY Id DESC;";
            cmd.Parameters.AddWithValue("$e", estado);
        }

        var list = new List<Cotizacion>();
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                list.Add(MapCotizacion(r));
            }
        }

        foreach (var c in list)
        {
            c.Detalles = GetCotizacionDetallesInternal(conn, c.Id);
        }
        return list;
    }

    private static Cotizacion MapCotizacion(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        CotizacionCodigo = r.GetString(1),
        FechaIso = r.GetString(2),
        ClienteNombre = r.GetString(3),
        Telefono = r.GetString(4),
        TipoLadrilloResumen = r.GetString(5),
        CantidadResumen = r.GetInt64(6),
        UnidadResumen = r.GetString(7),
        PrecioUnitarioResumenCop = r.GetInt64(8),
        DescuentoPctResumen = (decimal)r.GetDouble(9),
        TotalCop = r.GetInt64(10),
        Estado = r.GetString(11),
        VigenteHastaIso = r.GetString(12),
        ConvertidaEnVenta = r.GetString(13),
        VentaGeneradaId = r.IsDBNull(14) ? null : r.GetInt32(14),
        RutaPdf = r.GetString(15),
        Observaciones = r.GetString(16)
    };

    private static List<CotizacionDetalle> GetCotizacionDetallesInternal(SqliteConnection conn, int cotId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CotizacionId, ProductoId, ProductoNombre, Cantidad, Unidad, CantidadUnidadesFisicas, PrecioUnitarioCop, DescuentoPct, TotalLineaCop FROM CotizacionDetalles WHERE CotizacionId = $cid;";
        cmd.Parameters.AddWithValue("$cid", cotId);
        var list = new List<CotizacionDetalle>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new CotizacionDetalle
            {
                Id = r.GetInt32(0),
                CotizacionId = r.GetInt32(1),
                ProductoId = r.GetInt32(2),
                ProductoNombre = r.GetString(3),
                Cantidad = r.GetInt64(4),
                Unidad = r.GetString(5),
                CantidadUnidadesFisicas = r.GetInt64(6),
                PrecioUnitarioCop = r.GetInt64(7),
                DescuentoPct = (decimal)r.GetDouble(8),
                TotalLineaCop = r.GetInt64(9)
            });
        }
        return list;
    }

    public void CambiarEstadoCotizacion(int cotizacionId, string nuevoEstado)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Cotizaciones SET Estado = $e WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$e", nuevoEstado);
        cmd.Parameters.AddWithValue("$id", cotizacionId);
        cmd.ExecuteNonQuery();
    }

    public Venta ConvertirCotizacionEnVenta(int cotizacionId, string placaTransporte, string conductor, long fleteCop, int? vendedorId, long abonoInicialCop = 0)
    {
        var cot = GetCotizaciones().FirstOrDefault(c => c.Id == cotizacionId)
                  ?? throw new InvalidOperationException("Cotización no encontrada.");

        // CRITERIO DE ACEPTACIÓN #5:
        // Una cotización convertida no crea ventas duplicadas.
        if (string.Equals(cot.ConvertidaEnVenta, "Si", StringComparison.OrdinalIgnoreCase) || cot.Estado == "Convertida" || cot.VentaGeneradaId.HasValue)
        {
            throw new InvalidOperationException($"La cotización {cot.CotizacionCodigo} ya fue convertida en venta previamente (Venta ID #{cot.VentaGeneradaId}).");
        }
        if (cot.Estado == "Rechazada" || cot.Estado == "Vencida")
        {
            throw new InvalidOperationException($"No se puede convertir una cotización en estado '{cot.Estado}'.");
        }

        var nuevaVenta = new Venta
        {
            ClienteNombre = cot.ClienteNombre,
            Telefono = cot.Telefono,
            PlacaTransporte = placaTransporte,
            ConductorNombre = conductor,
            FleteAsignadoCop = fleteCop,
            VendedorId = vendedorId,
            CotizacionOrigenId = cot.Id,
            Observaciones = $"Generada desde Cotización {cot.CotizacionCodigo}. {cot.Observaciones}".Trim(),
            Detalles = cot.Detalles.Select(d => new VentaDetalle
            {
                ProductoId = d.ProductoId,
                ProductoNombre = d.ProductoNombre,
                Cantidad = d.Cantidad,
                Unidad = d.Unidad,
                PrecioPorUnidadMedidaCop = d.PrecioUnitarioCop,
                DescuentoPct = d.DescuentoPct
            }).ToList()
        };

        var ventaCreada = CrearVenta(nuevaVenta, abonoInicialCop, "Efectivo");

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Cotizaciones SET Estado = 'Convertida', ConvertidaEnVenta = 'Si', VentaGeneradaId = $vid WHERE Id = $cid;";
        cmd.Parameters.AddWithValue("$vid", ventaCreada.Id);
        cmd.Parameters.AddWithValue("$cid", cot.Id);
        cmd.ExecuteNonQuery();

        return ventaCreada;
    }

    public string GenerarPdfCotizacion(int cotizacionId, string? rutaDestino = null)
    {
        var cot = GetCotizaciones().FirstOrDefault(c => c.Id == cotizacionId)
                  ?? throw new InvalidOperationException("Cotización no encontrada.");

        string carpetaBase = GetConfigValor("Carpeta_Base", _baseDocsFolder);
        string ruta = rutaDestino ?? Path.Combine(carpetaBase, "Cotizaciones", $"{cot.CotizacionCodigo}.pdf");

        string emp = GetConfigValor("Nombre_Ladrillera", "LADRILLERA SAN ANDRES S.A.S.");
        string nit = GetConfigValor("NIT_Ladrillera", "900.123.456-7");
        string tel = GetConfigValor("Telefono_Ladrillera", "300 000 0000");

        PdfDocumentGenerator.GenerarCotizacionPdf(cot, emp, nit, tel, ruta);

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Cotizaciones SET RutaPdf = $r WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$r", ruta);
        cmd.Parameters.AddWithValue("$id", cotizacionId);
        cmd.ExecuteNonQuery();

        return ruta;
    }

    // =========================================================================
    // 7. TRANSPORTE
    // =========================================================================
    public List<Vehiculo> GetVehiculos()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Placa, Descripcion, CapacidadUnidades, Activo FROM Vehiculos ORDER BY Placa;";
        var list = new List<Vehiculo>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Vehiculo
            {
                Id = r.GetInt32(0),
                Placa = r.GetString(1),
                Descripcion = r.GetString(2),
                CapacidadUnidades = r.GetInt64(3),
                Activo = r.GetInt32(4) == 1
            });
        }
        return list;
    }

    public void GuardarVehiculo(Vehiculo v)
    {
        if (string.IsNullOrWhiteSpace(v.Placa)) throw new ArgumentException("La placa del vehículo es obligatoria.");
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Vehiculos (Placa, Descripcion, CapacidadUnidades, Activo)
VALUES ($p, $d, $c, $a)
ON CONFLICT(Placa) DO UPDATE SET Descripcion = $d, CapacidadUnidades = $c, Activo = $a;";
        cmd.Parameters.AddWithValue("$p", v.Placa.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("$d", v.Descripcion ?? "");
        cmd.Parameters.AddWithValue("$c", v.CapacidadUnidades);
        cmd.Parameters.AddWithValue("$a", v.Activo ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public List<Conductor> GetConductores()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Nombre, Telefono, Documento, PlacaHabitual, Activo FROM Conductores ORDER BY Nombre;";
        var list = new List<Conductor>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Conductor
            {
                Id = r.GetInt32(0),
                Nombre = r.GetString(1),
                Telefono = r.GetString(2),
                Documento = r.GetString(3),
                PlacaHabitual = r.GetString(4),
                Activo = r.GetInt32(5) == 1
            });
        }
        return list;
    }

    public void GuardarConductor(Conductor c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre)) throw new ArgumentException("El nombre del conductor es obligatorio.");
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Conductores (Nombre, Telefono, Documento, PlacaHabitual, Activo)
VALUES ($n, $t, $doc, $p, $a)
ON CONFLICT(Nombre) DO UPDATE SET Telefono = $t, Documento = $doc, PlacaHabitual = $p, Activo = $a;";
        cmd.Parameters.AddWithValue("$n", c.Nombre.Trim());
        cmd.Parameters.AddWithValue("$t", c.Telefono ?? "");
        cmd.Parameters.AddWithValue("$doc", c.Documento ?? "");
        cmd.Parameters.AddWithValue("$p", c.PlacaHabitual ?? "");
        cmd.Parameters.AddWithValue("$a", c.Activo ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public List<FleteTransporte> GetFletes()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, FechaSalidaIso, Placa, Conductor, Telefono, VentaId, ClientePedido, CostoFleteCop, EstadoEntrega, Observaciones FROM Fletes ORDER BY Id DESC;";
        var list = new List<FleteTransporte>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new FleteTransporte
            {
                Id = r.GetInt32(0),
                FechaSalidaIso = r.GetString(1),
                Placa = r.GetString(2),
                Conductor = r.GetString(3),
                Telefono = r.GetString(4),
                VentaId = r.IsDBNull(5) ? null : r.GetInt32(5),
                ClientePedido = r.GetString(6),
                CostoFleteCop = r.GetInt64(7),
                EstadoEntrega = r.GetString(8),
                Observaciones = r.GetString(9)
            });
        }
        return list;
    }

    public void RegistrarFlete(FleteTransporte f)
    {
        if (string.IsNullOrWhiteSpace(f.Placa)) throw new ArgumentException("La placa es obligatoria para registrar un flete.");
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Fletes (FechaSalidaIso, Placa, Conductor, Telefono, VentaId, ClientePedido, CostoFleteCop, EstadoEntrega, Observaciones)
VALUES ($fec, $pla, $cond, $tel, $vid, $ped, $cos, $est, $obs);";
        cmd.Parameters.AddWithValue("$fec", string.IsNullOrWhiteSpace(f.FechaSalidaIso) ? DateTime.Now.ToString("yyyy-MM-dd") : f.FechaSalidaIso);
        cmd.Parameters.AddWithValue("$pla", f.Placa.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("$cond", f.Conductor ?? "");
        cmd.Parameters.AddWithValue("$tel", f.Telefono ?? "");
        cmd.Parameters.AddWithValue("$vid", (object?)f.VentaId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ped", f.ClientePedido ?? "");
        cmd.Parameters.AddWithValue("$cos", f.CostoFleteCop);
        cmd.Parameters.AddWithValue("$est", string.IsNullOrWhiteSpace(f.EstadoEntrega) ? "En Ruta" : f.EstadoEntrega);
        cmd.Parameters.AddWithValue("$obs", f.Observaciones ?? "");
        cmd.ExecuteNonQuery();
    }

    public void ActualizarEstadoEntregaVenta(int ventaId, string nuevoEstadoEntrega)
    {
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Ventas SET EstadoEntrega = $e WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$e", nuevoEstadoEntrega);
            cmd.Parameters.AddWithValue("$id", ventaId);
            cmd.ExecuteNonQuery();
        }

        using (var cmd2 = conn.CreateCommand())
        {
            cmd2.Transaction = tx;
            cmd2.CommandText = "UPDATE Fletes SET EstadoEntrega = $e WHERE VentaId = $id;";
            cmd2.Parameters.AddWithValue("$e", nuevoEstadoEntrega);
            cmd2.Parameters.AddWithValue("$id", ventaId);
            cmd2.ExecuteNonQuery();
        }

        tx.Commit();
    }

    // =========================================================================
    // 8. VENDEDORES Y COMISIONES
    // =========================================================================
    public List<Vendedor> GetVendedores(bool soloActivos = false)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = soloActivos
            ? "SELECT Id, Codigo, Nombre, ComisionPct, Estado, FechaIngresoIso, FechaSalidaIso FROM Vendedores WHERE Estado = 'Activo' ORDER BY Codigo;"
            : "SELECT Id, Codigo, Nombre, ComisionPct, Estado, FechaIngresoIso, FechaSalidaIso FROM Vendedores ORDER BY Codigo;";

        var list = new List<Vendedor>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Vendedor
            {
                Id = r.GetInt32(0),
                Codigo = r.GetString(1),
                Nombre = r.GetString(2),
                ComisionPct = (decimal)r.GetDouble(3),
                Estado = r.GetString(4),
                FechaIngresoIso = r.GetString(5),
                FechaSalidaIso = r.IsDBNull(6) ? null : r.GetString(6)
            });
        }
        return list;
    }

    public void GuardarVendedor(Vendedor v)
    {
        if (string.IsNullOrWhiteSpace(v.Nombre)) throw new ArgumentException("El nombre del vendedor es obligatorio.");
        if (v.ComisionPct < 0 || v.ComisionPct > 100) throw new ArgumentException("El porcentaje de comisión debe estar entre 0 y 100.");

        using var conn = OpenConnection();
        if (string.IsNullOrWhiteSpace(v.Codigo))
        {
            v.Codigo = "V" + DateTime.Now.ToString("mmss");
        }
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Vendedores (Codigo, Nombre, ComisionPct, Estado, FechaIngresoIso, FechaSalidaIso)
VALUES ($c, $n, $p, $e, $fi, $fs)
ON CONFLICT(Codigo) DO UPDATE SET Nombre = $n, ComisionPct = $p, Estado = $e, FechaSalidaIso = $fs;";
        cmd.Parameters.AddWithValue("$c", v.Codigo.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("$n", v.Nombre.Trim());
        cmd.Parameters.AddWithValue("$p", (double)v.ComisionPct);
        cmd.Parameters.AddWithValue("$e", string.IsNullOrWhiteSpace(v.Estado) ? "Activo" : v.Estado);
        cmd.Parameters.AddWithValue("$fi", string.IsNullOrWhiteSpace(v.FechaIngresoIso) ? DateTime.Now.ToString("yyyy-MM-dd") : v.FechaIngresoIso);
        cmd.Parameters.AddWithValue("$fs", (object?)v.FechaSalidaIso ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<ComisionRegistro> GetComisiones(int? vendedorId = null, string? estado = null)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        var sb = new StringBuilder("SELECT Id, VentaId, FacturaId, VendedorId, VendedorNombre, FechaIso, BaseVentaCop, ComisionPct, ComisionCop, Estado, FechaPagoIso FROM Comisiones WHERE 1=1");
        if (vendedorId.HasValue && vendedorId.Value > 0)
        {
            sb.Append(" AND VendedorId = $vid");
            cmd.Parameters.AddWithValue("$vid", vendedorId.Value);
        }
        if (!string.IsNullOrWhiteSpace(estado) && estado != "Todas")
        {
            sb.Append(" AND Estado = $est");
            cmd.Parameters.AddWithValue("$est", estado);
        }
        sb.Append(" ORDER BY Id DESC;");
        cmd.CommandText = sb.ToString();

        var list = new List<ComisionRegistro>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ComisionRegistro
            {
                Id = r.GetInt32(0),
                VentaId = r.GetInt32(1),
                FacturaId = r.GetString(2),
                VendedorId = r.IsDBNull(3) ? null : r.GetInt32(3),
                VendedorNombre = r.GetString(4),
                FechaIso = r.GetString(5),
                BaseVentaCop = r.GetInt64(6),
                ComisionPct = (decimal)r.GetDouble(7),
                ComisionCop = r.GetInt64(8),
                Estado = r.GetString(9),
                FechaPagoIso = r.IsDBNull(10) ? null : r.GetString(10)
            });
        }
        return list;
    }

    public void MarcarComisionPagada(int comisionId)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Comisiones SET Estado = 'Pagada', FechaPagoIso = $f WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$f", DateTime.Now.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$id", comisionId);
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // 9. INFORMES Y EXPORTACIÓN (CSV / PDF)
    // =========================================================================
    public string ExportarInformeCsv(string tipoInforme, string? rutaDestino = null, string? desdeIso = null, string? hastaIso = null)
    {
        string carpeta = Path.Combine(GetConfigValor("Carpeta_Base", _baseDocsFolder), "Informes");
        Directory.CreateDirectory(carpeta);
        string ruta = rutaDestino ?? Path.Combine(carpeta, $"Informe_{tipoInforme}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        switch (tipoInforme.ToLowerInvariant())
        {
            case "ventas":
                sb.AppendLine("Factura_ID;Fecha;Cliente;Productos;TotalVenta_COP;Pagado_COP;Saldo_COP;EstadoPago;EstadoEntrega;Vendedor;Comision_COP;GananciaNeta_COP");
                var ventas = GetVentas(null, null, desdeIso, hastaIso).Where(v => !v.Anulada).ToList();
                foreach (var v in ventas)
                {
                    sb.AppendLine($"{v.FacturaId};{v.FechaIso};{v.ClienteNombre};{v.TipoLadrilloResumen};{v.TotalVentaCop};{v.MontoPagadoCop};{v.SaldoClienteCop};{v.EstadoPago};{v.EstadoEntrega};{v.VendedorNombre};{v.ComisionCop};{v.GananciaNetaCop}");
                }
                sb.AppendLine($"TOTALES;;;;{ventas.Sum(x => x.TotalVentaCop)};{ventas.Sum(x => x.MontoPagadoCop)};{ventas.Sum(x => x.SaldoClienteCop)};;;;{ventas.Sum(x => x.ComisionCop)};{ventas.Sum(x => x.GananciaNetaCop)}");
                break;

            case "inventario":
                sb.AppendLine("Codigo;Producto;StockDisponible;EnSecado;EnHorno;Danados;CostoUnitario_COP;PrecioMillar_COP;ValorInventario_COP");
                var prods = GetProductos(false);
                foreach (var p in prods)
                {
                    sb.AppendLine($"{p.Codigo};{p.Nombre};{p.StockActual};{p.StockSecado};{p.StockHorno};{p.DanadosAcumulados};{p.CostoUnitarioCop};{p.PrecioMillarCop};{p.StockActual * p.CostoUnitarioCop}");
                }
                break;

            case "cartera":
                sb.AppendLine("Factura_ID;Fecha;Cliente;Telefono;TotalVenta_COP;Abonado_COP;SaldoPendiente_COP;EstadoPago");
                var cartera = GetVentas().Where(v => !v.Anulada && v.SaldoClienteCop > 0).ToList();
                foreach (var c in cartera)
                {
                    sb.AppendLine($"{c.FacturaId};{c.FechaIso};{c.ClienteNombre};{c.Telefono};{c.TotalVentaCop};{c.MontoPagadoCop};{c.SaldoClienteCop};{c.EstadoPago}");
                }
                sb.AppendLine($"TOTAL CARTERA;;;;;;{cartera.Sum(x => x.SaldoClienteCop)};");
                break;

            default:
                sb.AppendLine("ID;Fecha;Vendedor;Factura;Base_COP;Porcentaje;Comision_COP;Estado");
                var coms = GetComisiones();
                foreach (var c in coms)
                {
                    sb.AppendLine($"{c.Id};{c.FechaIso};{c.VendedorNombre};{c.FacturaId};{c.BaseVentaCop};{c.ComisionPct};{c.ComisionCop};{c.Estado}");
                }
                break;
        }

        File.WriteAllText(ruta, sb.ToString(), Encoding.UTF8);
        return ruta;
    }

    public string ExportarInformePdf(string tipoInforme, string? rutaDestino = null, string? desdeIso = null, string? hastaIso = null)
    {
        string carpeta = Path.Combine(GetConfigValor("Carpeta_Base", _baseDocsFolder), "Informes");
        Directory.CreateDirectory(carpeta);
        string ruta = rutaDestino ?? Path.Combine(carpeta, $"Informe_{tipoInforme}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        var lines = new List<string>();
        if (string.Equals(tipoInforme, "ventas", StringComparison.OrdinalIgnoreCase))
        {
            var ventas = GetVentas(null, null, desdeIso, hastaIso).Where(v => !v.Anulada).ToList();
            lines.Add("FACTURA          FECHA        CLIENTE                 TOTAL COP       SALDO COP    ESTADO");
            lines.Add("-----------------------------------------------------------------------------------------");
            foreach (var v in ventas)
            {
                lines.Add($"{v.FacturaId.PadRight(16)} {v.FechaIso.PadRight(12)} { (v.ClienteNombre.Length > 22 ? v.ClienteNombre[..22] : v.ClienteNombre).PadRight(22)} ${v.TotalVentaCop,12:N0}   ${v.SaldoClienteCop,10:N0}   {v.EstadoPago}");
            }
            lines.Add("-----------------------------------------------------------------------------------------");
            lines.Add($"TOTAL VENTAS: ${ventas.Sum(x => x.TotalVentaCop):N0} COP  |  SALDO CARTERA: ${ventas.Sum(x => x.SaldoClienteCop):N0} COP  |  GANANCIA NETA: ${ventas.Sum(x => x.GananciaNetaCop):N0} COP");
        }
        else
        {
            var prods = GetProductos(false);
            lines.Add("PRODUCTO               DISPONIBLE      SECADO       HORNO      DANADOS    PRECIO MILLAR");
            lines.Add("---------------------------------------------------------------------------------------");
            foreach (var p in prods)
            {
                lines.Add($"{p.Nombre.PadRight(20)} {p.StockActual,12:N0} {p.StockSecado,11:N0} {p.StockHorno,11:N0} {p.DanadosAcumulados,12:N0}   ${p.PrecioMillarCop,12:N0}");
            }
        }

        return PdfDocumentGenerator.GenerarInformePdf($"INFORME DE {tipoInforme.ToUpperInvariant()}", lines, ruta);
    }

    // =========================================================================
    // 10. CONFIGURACIÓN, CONSECUTIVOS, BACKUPS Y MIGRACIÓN EXCEL
    // =========================================================================
    public List<ConfiguracionItem> GetConfiguraciones()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Parametro, Valor, Descripcion FROM Configuracion ORDER BY Parametro;";
        var list = new List<ConfiguracionItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ConfiguracionItem
            {
                Parametro = r.GetString(0),
                Valor = r.GetString(1),
                Descripcion = r.GetString(2)
            });
        }
        return list;
    }

    public string GetConfigValor(string parametro, string valorPorDefecto = "")
    {
        using var conn = OpenConnection();
        return GetConfigInternal(conn, null, parametro, valorPorDefecto);
    }

    private static string GetConfigInternal(SqliteConnection conn, SqliteTransaction? tx, string parametro, string valorPorDefecto)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT Valor FROM Configuracion WHERE Parametro = $p;";
        cmd.Parameters.AddWithValue("$p", parametro);
        var res = cmd.ExecuteScalar();
        return res == null || res == DBNull.Value ? valorPorDefecto : Convert.ToString(res) ?? valorPorDefecto;
    }

    public void GuardarConfiguracion(string parametro, string valor, string descripcion = "")
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Configuracion (Parametro, Valor, Descripcion)
VALUES ($p, $v, $d)
ON CONFLICT(Parametro) DO UPDATE SET Valor = $v;";
        cmd.Parameters.AddWithValue("$p", parametro.Trim());
        cmd.Parameters.AddWithValue("$v", valor ?? "");
        cmd.Parameters.AddWithValue("$d", descripcion ?? "");
        cmd.ExecuteNonQuery();
    }

    public List<ControlConsecutivo> GetConsecutivos()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Tipo, Prefijo, IntervaloReinicio, DiasReinicio, LastKey, LastCount FROM ControlConsecutivos ORDER BY Tipo;";
        var list = new List<ControlConsecutivo>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ControlConsecutivo
            {
                Tipo = r.GetString(0),
                Prefijo = r.GetString(1),
                IntervaloReinicio = r.GetString(2),
                DiasReinicio = r.GetInt32(3),
                LastKey = r.GetString(4),
                LastCount = r.GetInt64(5)
            });
        }
        return list;
    }

    public void GuardarConsecutivo(ControlConsecutivo c)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO ControlConsecutivos (Tipo, Prefijo, IntervaloReinicio, DiasReinicio, LastKey, LastCount)
VALUES ($t, $p, $i, $d, $lk, $lc)
ON CONFLICT(Tipo) DO UPDATE SET Prefijo = $p, IntervaloReinicio = $i, DiasReinicio = $d;";
        cmd.Parameters.AddWithValue("$t", c.Tipo);
        cmd.Parameters.AddWithValue("$p", c.Prefijo.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("$i", c.IntervaloReinicio);
        cmd.Parameters.AddWithValue("$d", c.DiasReinicio);
        cmd.Parameters.AddWithValue("$lk", c.LastKey ?? "");
        cmd.Parameters.AddWithValue("$lc", c.LastCount);
        cmd.ExecuteNonQuery();
    }

    public string PeekSiguienteConsecutivo(string tipo)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Prefijo, IntervaloReinicio, LastKey, LastCount FROM ControlConsecutivos WHERE Tipo = $t;";
        cmd.Parameters.AddWithValue("$t", tipo);
        using var r = cmd.ExecuteReader();
        string pref = tipo;
        string intervalo = "Mensual";
        string lastKey = "";
        long lastCount = 0;
        if (r.Read())
        {
            pref = r.GetString(0);
            intervalo = r.GetString(1);
            lastKey = r.GetString(2);
            lastCount = r.GetInt64(3);
        }
        string currentKey = BuildPeriodKey(intervalo);
        long next = (lastKey == currentKey) ? lastCount + 1 : 1;
        return $"{pref}-{currentKey}-{next:D4}";
    }

    private static string GenerarSiguienteConsecutivoInternal(SqliteConnection conn, SqliteTransaction tx, string tipo)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT Prefijo, IntervaloReinicio, LastKey, LastCount FROM ControlConsecutivos WHERE Tipo = $t;";
        cmd.Parameters.AddWithValue("$t", tipo);
        string pref = tipo;
        string intervalo = "Mensual";
        string lastKey = "";
        long lastCount = 0;

        using (var r = cmd.ExecuteReader())
        {
            if (r.Read())
            {
                pref = r.GetString(0);
                intervalo = r.GetString(1);
                lastKey = r.GetString(2);
                lastCount = r.GetInt64(3);
            }
        }

        string currentKey = BuildPeriodKey(intervalo);
        long next = (lastKey == currentKey) ? lastCount + 1 : 1;
        string candidate;

        // Garantizar unicidad histórica absoluta
        while (true)
        {
            candidate = $"{pref}-{currentKey}-{next:D4}";
            using var chk = conn.CreateCommand();
            chk.Transaction = tx;
            chk.CommandText = tipo == "FAC"
                ? "SELECT COUNT(*) FROM Ventas WHERE FacturaId = $c;"
                : "SELECT COUNT(*) FROM Cotizaciones WHERE CotizacionCodigo = $c;";
            chk.Parameters.AddWithValue("$c", candidate);
            long exists = (long)(chk.ExecuteScalar() ?? 0L);
            if (exists == 0) break;
            next++;
        }

        using (var upd = conn.CreateCommand())
        {
            upd.Transaction = tx;
            upd.CommandText = "UPDATE ControlConsecutivos SET LastKey = $k, LastCount = $c WHERE Tipo = $t;";
            upd.Parameters.AddWithValue("$k", currentKey);
            upd.Parameters.AddWithValue("$c", next);
            upd.Parameters.AddWithValue("$t", tipo);
            upd.ExecuteNonQuery();
        }

        return candidate;
    }

    private static string BuildPeriodKey(string intervalo)
    {
        var now = DateTime.Now;
        return intervalo.ToLowerInvariant() switch
        {
            "anual" => now.ToString("yyyy"),
            "diario" => now.ToString("yyyyMMdd"),
            _ => now.ToString("yyyyMM")
        };
    }

    private static int UpsertClienteInternal(SqliteConnection conn, SqliteTransaction tx, string nombre, string? telefono, string? direccion, long deltaSaldo)
    {
        using var sel = conn.CreateCommand();
        sel.Transaction = tx;
        sel.CommandText = "SELECT Id FROM Clientes WHERE lower(Nombre) = lower($n);";
        sel.Parameters.AddWithValue("$n", nombre.Trim());
        var existing = sel.ExecuteScalar();
        if (existing != null && existing != DBNull.Value)
        {
            int id = Convert.ToInt32(existing);
            using var upd = conn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE Clientes SET Telefono = CASE WHEN $t <> '' THEN $t ELSE Telefono END, Direccion = CASE WHEN $d <> '' THEN $d ELSE Direccion END, SaldoPendienteCop = MAX(0, SaldoPendienteCop + $sal) WHERE Id = $id;";
            upd.Parameters.AddWithValue("$t", telefono ?? "");
            upd.Parameters.AddWithValue("$d", direccion ?? "");
            upd.Parameters.AddWithValue("$sal", deltaSaldo);
            upd.Parameters.AddWithValue("$id", id);
            upd.ExecuteNonQuery();
            return id;
        }
        else
        {
            using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = "INSERT INTO Clientes (Nombre, Telefono, Direccion, SaldoPendienteCop, Activo) VALUES ($n, $t, $d, $sal, 1); SELECT last_insert_rowid();";
            ins.Parameters.AddWithValue("$n", nombre.Trim());
            ins.Parameters.AddWithValue("$t", telefono ?? "");
            ins.Parameters.AddWithValue("$d", direccion ?? "");
            ins.Parameters.AddWithValue("$sal", Math.Max(0, deltaSaldo));
            return Convert.ToInt32(ins.ExecuteScalar());
        }
    }

    public string CrearCopiaSeguridad(string? carpetaDestino = null)
    {
        string carpeta = carpetaDestino ?? Path.Combine(GetConfigValor("Carpeta_Base", _baseDocsFolder), "Backups");
        Directory.CreateDirectory(carpeta);
        string backupFile = Path.Combine(carpeta, $"SistemaLadrillera_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

        using var source = OpenConnection();
        var destCs = new SqliteConnectionStringBuilder { DataSource = backupFile, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        using var dest = new SqliteConnection(destCs);
        dest.Open();
        source.BackupDatabase(dest);
        return backupFile;
    }

    public void RestaurarCopiaSeguridad(string rutaBackup)
    {
        if (!File.Exists(rutaBackup))
            throw new FileNotFoundException("El archivo de copia de seguridad no existe.", rutaBackup);

        // Validar integridad del archivo backup antes de restaurar
        var backupCs = new SqliteConnectionStringBuilder { DataSource = rutaBackup, Mode = SqliteOpenMode.ReadOnly }.ToString();
        using (var chkConn = new SqliteConnection(backupCs))
        {
            chkConn.Open();
            using var cmd = chkConn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            string res = Convert.ToString(cmd.ExecuteScalar()) ?? "";
            if (!string.Equals(res, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"El archivo de respaldo falló la verificación de integridad SQLite: {res}");
            }
        }

        // Hacer backup preventivo antes de sobrescribir
        CrearCopiaSeguridad();

        using var source = new SqliteConnection(backupCs);
        source.Open();
        using var dest = OpenConnection();
        source.BackupDatabase(dest);
    }

    public bool VerificarIntegridadBaseDatos(out string mensaje)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check;";
        mensaje = Convert.ToString(cmd.ExecuteScalar()) ?? "desconocido";
        return string.Equals(mensaje, "ok", StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // MIGRADOR IDEMPOTENTE DESDE EXCEL (.XLSX / .XLSM)
    // =========================================================================
    public ResultadoMigracionExcel ImportarDesdeExcel(string rutaArchivoExcel)
    {
        var reporte = new ResultadoMigracionExcel { ArchivoOrigen = rutaArchivoExcel };
        if (!File.Exists(rutaArchivoExcel))
        {
            reporte.Errores.Add($"No existe el archivo: {rutaArchivoExcel}");
            return reporte;
        }

        // Respaldo automático antes de importar
        CrearCopiaSeguridad();

        var sheets = OpenXmlExcelReader.ReadWorkbook(rutaArchivoExcel);
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        // 1. Inventario + Secado + Horno
        var secadoMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        if (sheets.TryGetValue("Secado", out var secRows))
        {
            foreach (var r in secRows.Skip(1))
            {
                string nom = GetCol(r, "A");
                long cant = ParseLong(GetCol(r, "B"));
                if (!string.IsNullOrWhiteSpace(nom)) secadoMap[nom] = cant;
            }
        }

        var hornoMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        if (sheets.TryGetValue("Horno", out var horRows))
        {
            foreach (var r in horRows.Skip(1))
            {
                string nom = GetCol(r, "A");
                long cant = ParseLong(GetCol(r, "B"));
                if (!string.IsNullOrWhiteSpace(nom)) hornoMap[nom] = cant;
            }
        }

        if (sheets.TryGetValue("Inventario", out var invRows))
        {
            int idx = 1;
            foreach (var r in invRows.Skip(1))
            {
                string nombre = GetCol(r, "A");
                if (string.IsNullOrWhiteSpace(nombre)) continue;

                long stock = ParseLong(GetCol(r, "B"));
                long costo = ParseLong(GetCol(r, "F"));
                long pmillar = ParseLong(GetCol(r, "G"));
                long punit = pmillar > 0 ? pmillar / 1000 : costo;
                long sec = secadoMap.TryGetValue(nombre, out var sVal) ? sVal : 0;
                long hor = hornoMap.TryGetValue(nombre, out var hVal) ? hVal : 0;

                using var chk = conn.CreateCommand();
                chk.Transaction = tx;
                chk.CommandText = "SELECT Id FROM Productos WHERE lower(Nombre) = lower($n);";
                chk.Parameters.AddWithValue("$n", nombre);
                var existing = chk.ExecuteScalar();

                if (existing == null || existing == DBNull.Value)
                {
                    using var ins = conn.CreateCommand();
                    ins.Transaction = tx;
                    ins.CommandText = @"
INSERT INTO Productos (Codigo, Nombre, StockActual, StockSecado, StockHorno, EntradasAcumuladas, VentasAcumuladas, DanadosAcumulados, CostoUnitarioCop, PrecioUnitarioCop, PrecioMillarCop, Activo, FechaActualizacionIso)
VALUES ($c, $n, $stk, $sec, $hor, $stk, 0, 0, $cos, $pu, $pm, 1, $fec);";
                    ins.Parameters.AddWithValue("$c", $"PRD-{idx:D2}");
                    ins.Parameters.AddWithValue("$n", nombre);
                    ins.Parameters.AddWithValue("$stk", stock);
                    ins.Parameters.AddWithValue("$sec", sec);
                    ins.Parameters.AddWithValue("$hor", hor);
                    ins.Parameters.AddWithValue("$cos", costo);
                    ins.Parameters.AddWithValue("$pu", punit);
                    ins.Parameters.AddWithValue("$pm", pmillar);
                    ins.Parameters.AddWithValue("$fec", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    ins.ExecuteNonQuery();
                    reporte.ProductosImportados++;
                }
                else
                {
                    reporte.FilasOmitidasDuplicadas++;
                }
                idx++;
            }
        }

        // 2. Vendedores
        if (sheets.TryGetValue("Vendedores", out var vendRows))
        {
            foreach (var r in vendRows.Skip(1))
            {
                string cod = GetCol(r, "A");
                string nom = GetCol(r, "B");
                if (string.IsNullOrWhiteSpace(cod) || string.IsNullOrWhiteSpace(nom)) continue;
                decimal pct = ParseDecimal(GetCol(r, "C"));
                string est = GetCol(r, "D");

                using var chk = conn.CreateCommand();
                chk.Transaction = tx;
                chk.CommandText = "SELECT COUNT(*) FROM Vendedores WHERE Codigo = $c;";
                chk.Parameters.AddWithValue("$c", cod);
                if ((long)(chk.ExecuteScalar() ?? 0L) == 0)
                {
                    using var ins = conn.CreateCommand();
                    ins.Transaction = tx;
                    ins.CommandText = "INSERT INTO Vendedores (Codigo, Nombre, ComisionPct, Estado, FechaIngresoIso) VALUES ($c, $n, $p, $e, $f);";
                    ins.Parameters.AddWithValue("$c", cod);
                    ins.Parameters.AddWithValue("$n", nom);
                    ins.Parameters.AddWithValue("$p", (double)pct);
                    ins.Parameters.AddWithValue("$e", string.IsNullOrWhiteSpace(est) ? "Activo" : est);
                    ins.Parameters.AddWithValue("$f", DateTime.Now.ToString("yyyy-MM-dd"));
                    ins.ExecuteNonQuery();
                    reporte.VendedoresImportados++;
                }
                else
                {
                    reporte.FilasOmitidasDuplicadas++;
                }
            }
        }

        // 3. Configuracion
        if (sheets.TryGetValue("Configuracion", out var cfgRows))
        {
            foreach (var r in cfgRows.Skip(1))
            {
                string k = GetCol(r, "A");
                string v = GetCol(r, "B");
                if (string.IsNullOrWhiteSpace(k)) continue;
                using var ins = conn.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = "INSERT OR IGNORE INTO Configuracion (Parametro, Valor, Descripcion) VALUES ($k, $v, 'Importado desde Excel');";
                ins.Parameters.AddWithValue("$k", k);
                ins.Parameters.AddWithValue("$v", v);
                if (ins.ExecuteNonQuery() > 0) reporte.ConfiguracionesImportadas++;
            }
        }

        tx.Commit();
        return reporte;
    }

    private static string GetCol(Dictionary<string, string> row, string col)
        => row.TryGetValue(col, out var val) ? val.Trim() : "";

    private static long ParseLong(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out long v)) return v;
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) return (long)Math.Round(d);
        return 0;
    }

    private static decimal ParseDecimal(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d) ? d : 0m;
    }
}

