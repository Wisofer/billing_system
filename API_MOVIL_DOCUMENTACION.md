# 📱 API Móvil - EMSINET Sistema de Facturación

## Documentación para Integración Flutter

**Versión:** 1.0  
**Base URL:** `http://tu-servidor:5000/api/movil`  
**Autenticación:** JWT Bearer Token

---

## 🔐 Autenticación

### Login
Obtener token JWT para acceder a los endpoints protegidos.

```
POST /api/movil/auth/login
Content-Type: application/json
```

**Body:**
```json
{
  "usuario": "string",
  "contrasena": "string"
}
```

**Respuesta exitosa (200):**
```json
{
  "success": true,
  "message": "Inicio de sesión exitoso",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 604800,
    "expiresAt": "2024-12-24T12:00:00Z",
    "usuario": {
      "id": 1,
      "nombreUsuario": "admin",
      "nombreCompleto": "Administrador",
      "rol": "Administrador",
      "esAdmin": true
    }
  }
}
```

**Error (401):**
```json
{
  "success": false,
  "message": "Credenciales inválidas"
}
```

### Verificar Token
```
GET /api/movil/auth/verificar
Authorization: Bearer {token}
```

### Obtener Usuario Actual
```
GET /api/movil/auth/me
Authorization: Bearer {token}
```

---

## 🔑 Uso del Token

**Todos los endpoints (excepto login) requieren el header:**
```
Authorization: Bearer {token}
```

**Ejemplo en Dart/Flutter:**
```dart
final response = await http.get(
  Uri.parse('$baseUrl/api/movil/clientes'),
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer $token',
  },
);
```

---

## 👥 Clientes

### Listar Clientes (Paginado)
```
GET /api/movil/clientes?pagina=1&tamanoPagina=20&busqueda=&estado=
```

**Parámetros Query:**
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| pagina | int | 1 | Número de página |
| tamanoPagina | int | 20 | Items por página |
| busqueda | string | null | Buscar por nombre, código, cédula |
| estado | string | null | Filtrar por estado (Activo/Inactivo) |

**Respuesta:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "codigo": "EMS_123456",
      "nombre": "Juan Pérez",
      "telefono": "88881234",
      "email": "juan@email.com",
      "cedula": "001-010190-0001A",
      "activo": true,
      "totalFacturas": 5,
      "fechaCreacion": "2024-01-15T10:00:00"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "totalPages": 5,
    "totalItems": 100,
    "pageSize": 20
  }
}
```

### Obtener Cliente por ID
```
GET /api/movil/clientes/{id}
```

### Buscar Clientes
```
GET /api/movil/clientes/buscar?q={termino}
```

### Crear Cliente
```
POST /api/movil/clientes
Content-Type: application/json

{
  "nombre": "string (requerido)",
  "telefono": "string",
  "email": "string",
  "cedula": "string",
  "activo": true,
  "observaciones": "string"
}
```

### Actualizar Cliente
```
PUT /api/movil/clientes/{id}
Content-Type: application/json

{
  "nombre": "string",
  "telefono": "string",
  "email": "string",
  "cedula": "string",
  "activo": true,
  "observaciones": "string"
}
```

### Eliminar Cliente
```
DELETE /api/movil/clientes/{id}
```

### Estadísticas de Clientes
```
GET /api/movil/clientes/estadisticas
```

---

## 📄 Facturas

### Listar Facturas (Paginado)
```
GET /api/movil/facturas?pagina=1&tamanoPagina=20&estado=&categoria=&mes=&anio=
```

**Parámetros Query:**
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| pagina | int | 1 | Número de página |
| tamanoPagina | int | 20 | Items por página |
| estado | string | null | "Pendiente" o "Pagada" |
| categoria | string | null | "Internet" o "Streaming" |
| mes | int | null | Mes (1-12) |
| anio | int | null | Año (2024, 2025, etc.) |

**Respuesta:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "numero": "0001-JuanPere-122024",
      "cliente": {
        "id": 1,
        "codigo": "EMS_123456",
        "nombre": "Juan Pérez",
        "telefono": "88881234"
      },
      "servicio": {
        "id": 1,
        "nombre": "Internet 10Mbps"
      },
      "monto": 920.00,
      "estado": "Pendiente",
      "categoria": "Internet",
      "fechaCreacion": "2024-12-01T00:00:00",
      "mesFacturacion": "2024-12-01T00:00:00"
    }
  ],
  "pagination": { ... }
}
```

### Obtener Factura por ID
```
GET /api/movil/facturas/{id}
```

### Facturas de un Cliente
```
GET /api/movil/facturas/cliente/{clienteId}?estado=
```

### Facturas Pendientes
```
GET /api/movil/facturas/pendientes?limite=50
```

### Crear Factura
```
POST /api/movil/facturas
Content-Type: application/json

{
  "clienteId": 1,
  "servicioId": 1,
  "monto": 920.00,
  "categoria": "Internet"
}
```

### Descargar PDF
```
GET /api/movil/facturas/{id}/pdf
```
*Retorna archivo PDF binario*

### Eliminar Factura
```
DELETE /api/movil/facturas/{id}
```
*Solo facturas pendientes pueden eliminarse*

### Estadísticas de Facturas
```
GET /api/movil/facturas/estadisticas
```

---

## 💰 Pagos

### Listar Pagos (Paginado con Filtros)
```
GET /api/movil/pagos?pagina=1&tamanoPagina=20&fechaInicio=&fechaFin=&tipoPago=&banco=
```

**Parámetros Query:**
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| pagina | int | 1 | Número de página |
| tamanoPagina | int | 20 | Items por página |
| fechaInicio | DateTime | null | Filtrar desde fecha |
| fechaFin | DateTime | null | Filtrar hasta fecha |
| tipoPago | string | null | "Fisico", "Electronico", "Mixto" |
| banco | string | null | "BANPRO", "LAFISE", "BAC", "FICOHSA", "BDF" |

**Respuesta:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "monto": 920.00,
      "moneda": "NIO",
      "tipoPago": "Fisico",
      "banco": "BAC",
      "tipoCuenta": "Cordoba",
      "fechaPago": "2024-12-17T10:30:00",
      "montoCordobasFisico": 920.00,
      "montoDolaresFisico": null,
      "montoRecibido": 1000.00,
      "vuelto": 80.00,
      "tipoCambio": 36.50,
      "observaciones": "Pago mensual",
      "factura": {
        "id": 1,
        "numero": "0001-JuanPere-122024",
        "monto": 920.00,
        "cliente": {
          "id": 1,
          "codigo": "EMS_123456",
          "nombre": "Juan Pérez",
          "telefono": "88881234"
        }
      },
      "facturas": []
    }
  ],
  "pagination": { ... }
}
```

### Obtener Pago por ID
```
GET /api/movil/pagos/{id}
```

### Buscar Pagos
```
GET /api/movil/pagos/buscar?q={termino}&limite=20
```

**Busca por:** nombre cliente, código cliente, número factura

### Tipo de Cambio Actual (Compra y Venta)
```
GET /api/movil/pagos/tipo-cambio
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "compra": 36.32,
    "venta": 36.80,
    "tipoCambio": 36.80,
    "monedaBase": "USD",
    "monedaDestino": "NIO",
    "simboloBase": "$",
    "simboloDestino": "C$",
    "ultimaActualizacion": "2024-12-17T12:00:00",
    "descripcion": {
      "compra": "Usar cuando el cliente paga en dólares",
      "venta": "Usar para mostrar equivalentes y cálculos generales"
    }
  }
}
```

**Uso en Flutter:**
```dart
// Cuando cliente paga en dólares → usar COMPRA
double montoEnCordobas = montoDolares * tipoCambio.compra;

// Para mostrar equivalente en dólares → usar VENTA  
double equivalenteUSD = montoCordobas / tipoCambio.venta;
```

---

### 📋 Facturas para Pago

### Facturas de un Cliente (para seleccionar y pagar)
```
GET /api/movil/pagos/facturas-cliente/{clienteId}
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "cliente": {
      "id": 1,
      "codigo": "EMS_123456",
      "nombre": "Juan Pérez",
      "telefono": "88881234"
    },
    "facturas": [
      {
        "id": 1,
        "numero": "0001-JuanPere-122024",
        "monto": 920.00,
        "mesFacturacion": "2024-12-01T00:00:00",
        "mesNombre": "diciembre 2024",
        "estado": "Pendiente",
        "categoria": "Internet",
        "totalPagado": 0,
        "saldoPendiente": 920.00,
        "puedePagar": true,
        "servicio": {
          "id": 1,
          "nombre": "Internet 10Mbps"
        }
      }
    ],
    "resumen": {
      "totalFacturas": 5,
      "facturasPendientes": 2,
      "facturasPagadas": 3,
      "saldoTotalPendiente": 1840.00
    }
  }
}
```

### Facturas Pendientes Generales
```
GET /api/movil/pagos/facturas-pendientes?limite=50&busqueda=
```

---

### 💵 Registrar Pagos

### Pago Individual (Una Factura)
```
POST /api/movil/pagos
Content-Type: application/json

{
  "facturaId": 1,
  "monto": 920.00,
  "moneda": "NIO",
  "tipoPago": "Fisico",
  "banco": "BAC",
  "tipoCuenta": "Cordoba",
  "montoCordobasFisico": 920.00,
  "montoDolaresFisico": null,
  "montoCordobasElectronico": null,
  "montoDolaresElectronico": null,
  "montoRecibido": 1000.00,
  "vuelto": 80.00,
  "tipoCambio": 36.50,
  "observaciones": "Pago mensual diciembre"
}
```

**Valores para `tipoPago`:**
| Valor | Descripción |
|-------|-------------|
| `"Fisico"` | Pago en efectivo |
| `"Electronico"` | Transferencia/depósito bancario |
| `"Mixto"` | Combinación efectivo + electrónico |

**Valores para `moneda`:**
| Valor | Descripción |
|-------|-------------|
| `"NIO"` | Córdobas |
| `"USD"` | Dólares |
| `"Ambos"` | Pago en ambas monedas |

**Valores para `banco`:**
- `"BANPRO"`, `"LAFISE"`, `"BAC"`, `"FICOHSA"`, `"BDF"`

**Valores para `tipoCuenta`:**
- `"Dolar"`, `"Cordoba"`, `"Billetera"`

**Respuesta exitosa (201):**
```json
{
  "success": true,
  "message": "Pago registrado exitosamente",
  "data": {
    "id": 1,
    "monto": 920.00,
    "moneda": "NIO",
    "tipoPago": "Fisico",
    "fechaPago": "2024-12-17T10:30:00",
    "facturaNumero": "0001-JuanPere-122024",
    "clienteNombre": "Juan Pérez"
  }
}
```

### Pago Múltiple (Varias Facturas)
```
POST /api/movil/pagos/multiples
Content-Type: application/json

{
  "facturaIds": [1, 2, 3],
  "montoTotal": 2760.00,
  "moneda": "NIO",
  "tipoPago": "Electronico",
  "banco": "BANPRO",
  "tipoCuenta": "Cordoba",
  "montoCordobasElectronico": 2760.00,
  "observaciones": "Pago 3 meses atrasados"
}
```

**Respuesta:**
```json
{
  "success": true,
  "message": "Pago registrado para 3 factura(s)",
  "data": {
    "id": 1,
    "monto": 2760.00,
    "moneda": "NIO",
    "tipoPago": "Electronico",
    "cantidadFacturas": 3,
    "fechaPago": "2024-12-17T10:30:00"
  }
}
```

---

### 🗑️ Eliminar Pagos

### Eliminar Pago Individual
```
DELETE /api/movil/pagos/{id}
```

### Eliminar Múltiples Pagos
```
DELETE /api/movil/pagos/multiples
Content-Type: application/json

{
  "pagoIds": [1, 2, 3]
}
```

**Respuesta:**
```json
{
  "success": true,
  "message": "3 pago(s) eliminado(s)",
  "data": {
    "eliminados": 3,
    "noEncontrados": 0
  }
}
```

---

### 📊 Estadísticas y Reportes

### Resumen del Día
```
GET /api/movil/pagos/resumen-dia?fecha=2024-12-17
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "fecha": "2024-12-17",
    "fechaFormateada": "martes, 17 diciembre 2024",
    "totalPagos": 15,
    "montoTotal": 13800.00,
    "porTipoPago": {
      "fisico": { "cantidad": 10, "monto": 9200.00 },
      "electronico": { "cantidad": 4, "monto": 3680.00 },
      "mixto": { "cantidad": 1, "monto": 920.00 }
    },
    "porBanco": [
      { "banco": "BAC", "cantidad": 3, "monto": 2760.00 },
      { "banco": "BANPRO", "cantidad": 1, "monto": 920.00 }
    ]
  }
}
```

### Resumen por Período
```
GET /api/movil/pagos/resumen-periodo?mes=12&anio=2024
```
*También acepta:* `fechaInicio` y `fechaFin`

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "periodo": {
      "inicio": "2024-12-01",
      "fin": "2024-12-31"
    },
    "resumen": {
      "totalPagos": 150,
      "montoTotal": 138000.00,
      "promedioProPago": 920.00
    },
    "porTipoPago": { ... },
    "porBanco": [ ... ],
    "pagosPorDia": [
      { "fecha": "2024-12-01", "cantidad": 5, "monto": 4600.00 },
      { "fecha": "2024-12-02", "cantidad": 8, "monto": 7360.00 }
    ]
  }
}
```

### Total Ingresos
```
GET /api/movil/pagos/total-ingresos
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "totalIngresos": 1500000.00,
    "ingresosMesActual": 138000.00,
    "ingresosHoy": 13800.00,
    "totalPagos": 1630
  }
}
```

### Estadísticas Completas
```
GET /api/movil/pagos/estadisticas
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "general": {
      "totalPagos": 1630,
      "totalIngresos": 1500000.00,
      "promedioProPago": 920.25
    },
    "porTipoPago": {
      "fisico": {
        "cantidad": 1000,
        "monto": 920000.00,
        "porcentaje": 61.35
      },
      "electronico": {
        "cantidad": 500,
        "monto": 460000.00,
        "porcentaje": 30.67
      },
      "mixto": {
        "cantidad": 130,
        "monto": 120000.00,
        "porcentaje": 7.98
      }
    },
    "mesActual": {
      "mes": "diciembre 2024",
      "totalPagos": 150,
      "totalIngresos": 138000.00
    },
    "porBanco": [
      { "banco": "BAC", "cantidad": 200, "monto": 184000.00 },
      { "banco": "BANPRO", "cantidad": 180, "monto": 165600.00 }
    ],
    "configuracion": {
      "bancos": ["BANPRO", "LAFISE", "BAC", "FICOHSA", "BDF"],
      "tiposPago": ["Fisico", "Electronico", "Mixto"],
      "monedas": ["NIO", "USD", "Ambos"],
      "tiposCuenta": ["Dolar", "Cordoba", "Billetera"]
    }
  }
}
```

---

## 🛠️ Servicios

### Listar Servicios
```
GET /api/movil/servicios?categoria=&activos=true
```

**Respuesta:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "nombre": "Internet 10Mbps",
      "descripcion": "Plan residencial",
      "precio": 920.00,
      "categoria": "Internet",
      "activo": true
    }
  ]
}
```

### Obtener Servicio por ID
```
GET /api/movil/servicios/{id}
```

### Servicios por Categoría
```
GET /api/movil/servicios/categoria/{categoria}
```

### Listar Categorías
```
GET /api/movil/servicios/categorias
```

---

## 📊 Dashboard

### Dashboard General
```
GET /api/movil/dashboard
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "clientes": {
      "total": 150,
      "activos": 140,
      "nuevosEsteMes": 5
    },
    "facturas": {
      "total": 1200,
      "pendientes": 45,
      "pagadas": 1155,
      "montoPendiente": 41400.00,
      "montoPagado": 1062600.00
    },
    "ingresos": {
      "total": 1062600.00
    },
    "egresos": {
      "mesActual": 15000.00,
      "total": 180000.00
    },
    "fecha": "2024-12-17T12:00:00"
  }
}
```

### Estadísticas de Clientes
```
GET /api/movil/dashboard/clientes
```

### Estadísticas de Facturas
```
GET /api/movil/dashboard/facturas?mes=12&anio=2024
```

### Estadísticas de Pagos
```
GET /api/movil/dashboard/pagos?mes=12&anio=2024
```

### Estadísticas de Egresos
```
GET /api/movil/dashboard/egresos
```

### Resumen Rápido
```
GET /api/movil/dashboard/resumen
```

---

## ⚠️ Códigos de Error

| Código | Descripción |
|--------|-------------|
| 200 | OK - Petición exitosa |
| 201 | Created - Recurso creado |
| 400 | Bad Request - Datos inválidos |
| 401 | Unauthorized - Token inválido o expirado |
| 404 | Not Found - Recurso no encontrado |
| 500 | Internal Server Error |

**Formato de Error:**
```json
{
  "success": false,
  "message": "Descripción del error"
}
```

---

## 🔧 Ejemplo de Implementación Flutter

### Modelo de Respuesta Base
```dart
class ApiResponse<T> {
  final bool success;
  final String? message;
  final T? data;
  final Pagination? pagination;

  ApiResponse({
    required this.success,
    this.message,
    this.data,
    this.pagination,
  });
}
```

### Servicio de Autenticación
```dart
class AuthService {
  static const String baseUrl = 'http://tu-servidor:5000/api/movil';
  String? _token;

  Future<bool> login(String usuario, String contrasena) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'usuario': usuario,
        'contrasena': contrasena,
      }),
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      if (data['success']) {
        _token = data['data']['token'];
        // Guardar token en SharedPreferences
        return true;
      }
    }
    return false;
  }

  Map<String, String> get headers => {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer $_token',
  };
}
```

### Servicio de Clientes
```dart
class ClienteService {
  final AuthService _auth;

  Future<List<Cliente>> getClientes({int pagina = 1}) async {
    final response = await http.get(
      Uri.parse('$baseUrl/clientes?pagina=$pagina'),
      headers: _auth.headers,
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return (data['data'] as List)
          .map((e) => Cliente.fromJson(e))
          .toList();
    }
    throw Exception('Error al cargar clientes');
  }
}
```

---

## 📝 Notas Importantes

1. **Token Expira:** El token JWT expira en **7 días**. Implementar refresh o re-login.

2. **Moneda:** Los montos están en **Córdobas (NIO)**. Usar `tipo-cambio` para conversión.

3. **Fechas:** Formato ISO 8601 (`2024-12-17T12:00:00`)

4. **Paginación:** Por defecto 20 items por página.

5. **CORS:** El servidor permite peticiones desde cualquier origen para la API móvil.

---

**Contacto Soporte:** atencion.al.cliente@emsinetsolut.com

