# Manual de Usuario, Técnico y Diccionario de Datos — Sistema Ladrillera (100% Offline)

## 1. Requisitos e Instalación Autónoma (Windows x64)
- **Sistema Operativo:** Windows 10 / Windows 11 (arquitectura 64 bits `win-x64`).
- **Conexión a Internet:** **No requerida (100% Offline)**. No utiliza servicios en la nube, CDN ni servidores externos.
- **Ejecutable Directo:** Ubicado en `D:\antigravity\ladrillera\Publicacion_Offline\SistemaLadrillera.Wpf.exe` (o compilado en `D:\antigravity\ladrillera\src\SistemaLadrillera.Wpf\bin\Release\net8.0-windows\win-x64\publish\`).
- **Base de Datos Local:** Se crea automáticamente al abrir el programa en:
  `C:\Users\<TuUsuario>\Documents\Ladrillera\SistemaLadrillera.db`

---

## 2. Guía de Operación por Módulo (Menú Lateral)

### Módulo 1: Dashboard / Inicio
Muestra en tiempo real los indicadores calculados directamente desde SQLite:
- **Ventas del Día y del Mes (COP)** con cálculo automático de ganancia neta.
- **Cartera / Saldo Pendiente de Clientes (COP)**.
- **Unidades en Inventario Disponible, En Secado, En Horno y Dañados/Pérdidas**.
- Tabla resumen por tipo de ladrillo (`Farol`, `Bloquelon`, `Maciso`, `Bloque cemento`) y últimas ventas.

### Módulo 2: Producción (Secado, Horno y Pérdidas)
Controla el ciclo físico del ladrillo sin duplicar cantidades:
1. **Ingreso a Secado:** Registra los ladrillos crudos moldeados que entran a patio/galpón de secado.
2. **Traslado de Secado a Horno:** Descuenta de `Secado` y suma a `Horno` validando que exista saldo suficiente.
3. **Salida de Horno a Inventario Disponible:** Cuando termina la cocción, descuenta de `Horno`, suma al `Inventario Disponible` y genera automáticamente la entrada en el **Kardex**.
4. **Pérdidas y Material Dañado:** Permite descontar roturas indicando la etapa (`Secado`, `Horno` o `Inventario`) y la causa.

### Módulo 3: Inventario y Kardex
- Permite crear nuevos productos o actualizar costos y precios por millar/unidad.
- **Regla de Integridad:** Nunca se modifica el stock sin generar un registro en `InventarioMovimientos` (**Kardex**).
- Incluye formulario para registrar entradas manuales, ajustes positivos/negativos o bajas por rotura.

### Módulo 4: Ventas, Saldos/Pagos y Devoluciones
- **4.1 Nueva Venta Multiproducto:**
  - Permite agregar uno o varios tipos de ladrillo en un mismo pedido eligiendo **Unidad** o **Millar (`1 Millar = 1.000 Unidades`)**.
  - Calcula automáticamente descuentos, comisión del vendedor activo (`ComisionCOP = TotalProductos * %Comision`), flete y ganancia neta (`TotalProductos - Comision - Flete`).
  - Valida que no se venda más del inventario disponible (salvo que habilites `PermitirVentaSinStock = Si` en Configuración).
- **4.2 Historial, Abonos y Devoluciones:**
  - Permite registrar **abonos parciales o pago total** actualizando el saldo del cliente.
  - Permite registrar **devoluciones**: solo suma nuevamente al inventario disponible si el estado del ladrillo es **Apto**. Si se marca como **Dañado**, no ingresa al inventario vendible y se acumula en dañados.

### Módulo 5: Facturación y PDF Local
- Genera consecutivos únicos atómicos (`FAC-YYYYMM-0001`).
- Exporta y abre el comprobante PDF en `Documents\Ladrillera\Facturas\`.
- **Nota Legal:** El comprobante PDF incluye la leyenda expresa indicando que es un documento interno de control y **no sustituye la Factura Electrónica DIAN**.

### Módulo 6: Cotizaciones
- Crea cotizaciones con consecutivo `COT-YYYYMM-0001`, vigencia y exportación a PDF.
- Botón **"Aceptar y Convertir en Venta (1 Clic)"**: convierte la cotización en venta real descontando inventario y bloqueando reconversiones duplicadas.

### Módulo 7: Transporte y Fletes
- Administra vehículos (placas y capacidad), conductores e historial de fletes vinculados a las ventas.
- Permite marcar despachos como **Entregado**.

### Módulo 8: Vendedores y Comisiones
- Administra vendedores (`V001 - Juan Pérez`, `V002 - Ana Gómez`, etc.) y su porcentaje de comisión.
- Lista todas las comisiones generadas por venta y permite marcarlas como **Pagadas**.

### Módulo 9: Informes y Exportación
- Exporta reportes verificables en formato **CSV (compatible con Excel)** y **PDF** para:
  - Ventas y Utilidad Neta por rango de fechas
  - Inventario y Producción
  - Cuentas por Cobrar (Cartera)
  - Comisiones de Vendedores

### Módulo 10: Configuración, Migración Excel y Copias de Seguridad
- **Copias de Seguridad (`.db`):** Crea respaldos instantáneos mediante la API transaccional de SQLite y verifica la salud del archivo con `PRAGMA integrity_check`.
- **Migrador desde Excel:** Importa datos desde cualquiera de los archivos `.xlsm`/`.xlsx` en `D:\antigravity\ladrillera` con estrategia de idempotencia (puedes ejecutarlo varias veces sin duplicar productos ni vendedores).

---

## 3. Diccionario de Datos SQLite (Tablas Principales)

| Tabla SQLite | Clave Primaria | Descripción y Reglas de Integridad |
| :--- | :--- | :--- |
| `Productos` | `Id` (`UNIQUE Codigo, Nombre`) | Catálogo de ladrillos, `StockActual`, `StockSecado`, `StockHorno`, costos y precios en pesos enteros (`INTEGER`). |
| `Clientes` | `Id` (`UNIQUE Nombre`) | Directorio de clientes y saldo acumulado. |
| `Vendedores` | `Id` (`UNIQUE Codigo`) | Vendedores y `%` de comisión (`CHECK 0..100`). |
| `ProduccionMovimientos` | `Id` (`FK ProductoId`) | Trazabilidad de entradas a secado, traslados a horno, salidas a inventario y pérdidas. |
| `InventarioMovimientos` | `Id` (`FK ProductoId`) | **Kardex** histórico inmutable con entrada, salida y `SaldoPosterior`. |
| `Ventas` / `VentaDetalles` | `Id` (`UNIQUE FacturaId`) | Encabezado de 26 campos compatibles con Excel + detalle multiproducto. |
| `Facturas` / `FacturaDetalles` | `Id` (`UNIQUE FacturaCodigo`) | Comprobantes emitidos y ruta del PDF local. |
| `Cotizaciones` / `CotizacionDetalles` | `Id` (`UNIQUE CotizacionCodigo`) | Cotizaciones y bandera `ConvertidaEnVenta` + `VentaGeneradaId`. |
| `Pagos` | `Id` (`FK VentaId`) | Historial de abonos parciales y cancelaciones de saldo. |
| `Devoluciones` | `Id` (`FK VentaId, ProductoId`) | Devoluciones con control de estado `Apto` / `Dañado`. |
| `Vehiculos`, `Conductores`, `Fletes` | `Id` | Flota de transporte y despachos asociados a pedidos. |
| `Comisiones` | `Id` (`FK VentaId`) | Liquidación de comisiones por venta y estado de pago. |
| `Configuracion` | `Parametro` | Parámetros globales del sistema. |
| `ControlConsecutivos` | `Tipo` (`FAC`, `COT`) | Contadores atómicos por periodo. |

---

## 4. Procedimiento de Respaldo y Restauración + Límites de SQLite Local
1. **Respaldo Rápido:** Haz clic en el botón verde **"💾 Respaldo Rápido"** en la barra superior o desde el Módulo 10. Se guardará un archivo `.db` fechado en `Documents\Ladrillera\Backups\`.
2. **Restauración Segura:** En el Módulo 10 haz clic en **"Restaurar Copia de Seguridad"**. El sistema ejecuta primero `PRAGMA integrity_check` sobre el archivo elegido, crea un respaldo preventivo de la base actual y restaura los datos.
3. **Nota Técnica sobre SQLite Local:** SQLite con `journal_mode = WAL` es sumamente robusto para **un equipo de escritorio local** frente a cortes de energía o cierres inesperados. No se debe colocar el archivo `.db` en una carpeta compartida de red para escritura simultánea desde varios computadores; si en el futuro se requiere operación multiusuario en red local, deberá exponerse mediante un servicio API en red local.

