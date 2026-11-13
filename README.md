# 🧱 Sistema de Gestión Integral para Ladrillera (100% Offline)

Aplicación de escritorio profesional para Windows desarrollada en **C# (.NET 8 LTS) + WPF + SQLite**, diseñada para operar **100% sin conexión a internet (Offline)** reemplazando los libros de Excel y macros originales del *Sistema Ladrillera*.

---

## 🚀 Descarga e Instalación Directa

En la raíz de este repositorio se encuentran listos para usar:

- 💿 **Instalador Autocontenido para Windows (x64):** [`Instalador_SistemaLadrillera_Setup.exe`](Instalador_SistemaLadrillera_Setup.exe) *(no requiere internet ni tener .NET instalado previamente; crea accesos directos en el Escritorio y Menú Inicio)*.
- ⚡ **Ejecutable Portable:** [`Publicacion_Offline/SistemaLadrillera.Wpf.exe`](Publicacion_Offline/SistemaLadrillera.Wpf.exe) o mediante [`Abrir_Sistema_Ladrillera.bat`](Abrir_Sistema_Ladrillera.bat).
- 📖 **Manual Completo (Usuario, Técnico y Diccionario de Datos):** [`MANUAL_SISTEMA_LADRILLERA.md`](MANUAL_SISTEMA_LADRILLERA.md).

---

## 📋 Módulos Incluidos

1. **📊 Dashboard / Inicio:** Indicadores KPI en tiempo real (Ventas del día/mes, ganancia neta, cartera de clientes, unidades disponibles, en secado, en horno y pérdidas).
2. **🔥 Producción (Secado, Horno y Pérdidas):** Flujo trazable de ladrillo crudo en secado → traslado a horno → salida de cocción a inventario disponible y control de roturas por etapa.
3. **📦 Inventario y Kardex:** Catálogo de productos (`Farol`, `Bloquelón`, `Macizo`, `Bloque cemento`), entradas, ajustes y Kardex histórico inmutable.
4. **🛒 Ventas, Saldos/Pagos y Devoluciones:** Ventas multiproducto por **Unidad** o **Millar (`1 Millar = 1.000 Unidades`)**, cálculo automático de comisión y utilidad neta, abonos parciales y devoluciones con reingreso condicional (solo si el material es `Apto`).
5. **🧾 Facturación y PDF Local:** Consecutivos únicos atómicos (`FAC-YYYYMM-0001`) y exportación a PDF local con nota legal (comprobante interno no DIAN).
6. **📋 Cotizaciones:** Consecutivos `COT-YYYYMM-0001`, exportación a PDF y conversión a venta en 1 clic sin duplicar registros.
7. **🚚 Transporte y Fletes:** Registro de vehículos/placas, conductores, costos de flete y estados de entrega.
8. **👔 Vendedores y Comisiones:** Gestión de vendedores (`V001`, `V002`, etc.) y liquidación de comisiones.
9. **📈 Informes y Exportación:** Reportes exportables a **CSV (Excel)** y **PDF** de Ventas, Inventario, Cartera y Comisiones.
10. **⚙️ Configuración, Migrador Excel y Copias de Seguridad:** Respaldo y restauración de base SQLite (`.db`) con validación `PRAGMA integrity_check` e importador idempotente desde archivos `.xlsm` / `.xlsx`.

---

## 🛠️ Arquitectura de la Solución (`src/`)

- `SistemaLadrillera.Domain`: Entidades de negocio y modelos.
- `SistemaLadrillera.Application`: Contratos y servicios de aplicación.
- `SistemaLadrillera.Infrastructure`: Persistencia transaccional SQLite (`WAL` + `Foreign Keys`), generador PDF local y lector OpenXML de Excel.
- `SistemaLadrillera.Wpf`: Interfaz gráfica moderna estilo panel administrativo Bootstrap industrial.
- `SistemaLadrillera.Tests`: Pruebas automatizadas `xUnit` para los criterios de aceptación.
