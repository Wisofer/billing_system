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

### Listar Pagos
```
GET /api/movil/pagos?pagina=1&tamanoPagina=20&fechaInicio=&fechaFin=
```

### Obtener Pago por ID
```
GET /api/movil/pagos/{id}
```

### Tipo de Cambio Actual
```
GET /api/movil/pagos/tipo-cambio
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "tipoCambio": 36.50,
    "monedaBase": "USD",
    "monedaDestino": "NIO"
  }
}
```

### Registrar Pago (Factura Individual)
```
POST /api/movil/pagos
Content-Type: application/json

{
  "facturaId": 1,
  "monto": 920.00,
  "moneda": "NIO",
  "tipoPago": "Efectivo",
  "banco": "BAC",
  "tipoCuenta": "Ahorro",
  "montoRecibido": 1000.00,
  "vuelto": 80.00,
  "tipoCambio": 36.50,
  "observaciones": "string"
}
```

**Valores para `tipoPago`:**
- `"Efectivo"`
- `"Transferencia"`
- `"Deposito"`

**Valores para `moneda`:**
- `"NIO"` (Córdobas)
- `"USD"` (Dólares)

### Pago Múltiple (Varias Facturas)
```
POST /api/movil/pagos/multiples
Content-Type: application/json

{
  "facturaIds": [1, 2, 3],
  "montoTotal": 2760.00,
  "moneda": "NIO",
  "tipoPago": "Transferencia",
  "banco": "BANPRO",
  "observaciones": "Pago 3 meses"
}
```

### Eliminar Pago
```
DELETE /api/movil/pagos/{id}
```

### Resumen del Día
```
GET /api/movil/pagos/resumen-dia?fecha=2024-12-17
```

### Total Ingresos
```
GET /api/movil/pagos/total-ingresos
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

