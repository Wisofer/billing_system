# 📱 API para Clientes - Documentación Completa

## ✅ Confirmación de Cambios

**IMPORTANTE**: Esta API es completamente nueva. No se modificó ningún código existente del sistema. El sistema original sigue funcionando normalmente.

### Archivos Nuevos Creados:
- `Controllers/Api/Cliente/ClienteAuthController.cs` - Autenticación
- `Controllers/Api/Cliente/ClienteDashboardController.cs` - Dashboard
- `Controllers/Api/Cliente/ClienteFacturasController.cs` - Facturas
- `Controllers/Api/Cliente/ClientePagosController.cs` - Pagos
- `Controllers/Api/Cliente/ClientePerfilController.cs` - Perfil

**No se modificó:**
- ❌ Ninguna tabla de base de datos
- ❌ Ningún servicio existente
- ❌ Ningún controlador existente
- ❌ Solo se corrigió un pequeño conflicto en `Controllers/Api/Movil/ClientesController.cs` (uso de `Models.Entities.Cliente`)

---

## 🔐 Autenticación

### Base URL
```
http://localhost:5229/api/cliente
```
(Puerto puede variar según configuración)

### Login

**Endpoint:** `POST /api/cliente/auth/login`

**Descripción:** Inicia sesión con el código de cliente (ej: `EMS_617015`)

**Request:**
```json
{
  "codigo": "EMS_617015"
}
```

**Response Exitoso (200):**
```json
{
  "success": true,
  "message": "Inicio de sesión exitoso",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 604800,
    "expiresAt": "2026-01-09T04:54:45.0041035Z",
    "cliente": {
      "id": 35,
      "codigo": "EMS_763870",
      "nombre": "Ervin Dionisio Olivas García",
      "activo": true
    }
  }
}
```

**Response Error (401):**
```json
{
  "success": false,
  "message": "Código de cliente inválido"
}
```
o
```json
{
  "success": false,
  "message": "Cliente inactivo. Contacte a soporte."
}
```

**Ejemplo cURL:**
```bash
curl -X POST http://localhost:5229/api/cliente/auth/login \
  -H "Content-Type: application/json" \
  -d '{"codigo":"EMS_763870"}'
```

---

### Verificar Token

**Endpoint:** `GET /api/cliente/auth/verificar`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Token válido",
  "data": {
    "valido": true
  }
}
```

---

## 📊 Dashboard

**Endpoint:** `GET /api/cliente/dashboard`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Descripción:** Obtiene resumen general del cliente (saldo pendiente, facturas, etc.)

**Response (200):**
```json
{
  "success": true,
  "data": {
    "saldoPendiente": 872.0,
    "facturasPendientes": {
      "cantidad": 2,
      "monto": 872.0
    },
    "ultimaFactura": {
      "id": 260,
      "numero": "0062-AlfonsoAnt-122025-STR",
      "monto": 436.0,
      "estado": "Pendiente",
      "fechaCreacion": "2026-01-01T02:12:49.058801",
      "mesFacturacion": "2025-12-01T00:00:00"
    },
    "resumen": {
      "totalFacturas": 3,
      "facturasPagadas": 1,
      "facturasCanceladas": 0
    },
    "fecha": "2026-01-01T22:55:45.1784816-06:00"
  }
}
```

**Ejemplo cURL:**
```bash
curl -X GET http://localhost:5229/api/cliente/dashboard \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json"
```

---

## 🧾 Facturas

### Listar Facturas

**Endpoint:** `GET /api/cliente/facturas`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Query Parameters (todos opcionales):**
- `estado` - Filtrar por estado: "Pendiente", "Pagada", "Cancelada", "Todas" (default)
- `categoria` - Filtrar por categoría: "Internet", "Streaming"
- `mes` - Mes (1-12)
- `anio` - Año (ej: 2025)
- `pagina` - Número de página (default: 1)
- `tamanoPagina` - Tamaño de página (default: 20)

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 211,
      "numero": "0143-ErvinDioni-122025",
      "monto": 920.0,
      "estado": "Pagada",
      "categoria": "Internet",
      "fechaCreacion": "2026-01-01T02:12:47.603301",
      "mesFacturacion": "2025-12-01T00:00:00",
      "saldoPendiente": 0,
      "totalPagado": 920.0
    }
  ],
  "pagination": {
    "currentPage": 1,
    "totalPages": 1,
    "totalItems": 2,
    "pageSize": 20
  }
}
```

**Ejemplo cURL:**
```bash
# Todas las facturas
curl -X GET "http://localhost:5229/api/cliente/facturas" \
  -H "Authorization: Bearer {token}"

# Solo pendientes
curl -X GET "http://localhost:5229/api/cliente/facturas?estado=Pendiente" \
  -H "Authorization: Bearer {token}"

# Filtrar por mes
curl -X GET "http://localhost:5229/api/cliente/facturas?mes=12&anio=2025" \
  -H "Authorization: Bearer {token}"
```

---

### Detalle de Factura

**Endpoint:** `GET /api/cliente/facturas/{id}`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Path Parameters:**
- `id` - ID de la factura

**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": 211,
    "numero": "0143-ErvinDioni-122025",
    "monto": 920.0,
    "estado": "Pagada",
    "categoria": "Internet",
    "fechaCreacion": "2026-01-01T02:12:47.603301",
    "mesFacturacion": "2025-12-01T00:00:00",
    "saldoPendiente": 0,
    "totalPagado": 920.0,
    "servicios": [
      {
        "id": 1,
        "nombre": "Plan 1",
        "descripcion": "Internet Residencial de hasta 10Mbps, ilimitado.",
        "precio": 920.0,
        "cantidad": 1
      }
    ],
    "pagos": [
      {
        "id": 123,
        "monto": 920.0,
        "fechaPago": "2025-12-15T10:30:00",
        "tipoPago": "Fisico",
        "moneda": "NIO"
      }
    ]
  }
}
```

**Response Error (404):**
```json
{
  "success": false,
  "message": "Factura no encontrada"
}
```

**Response Error (403):**
```json
{
  "success": false,
  "message": "Forbidden"
}
```
(Se devuelve si la factura no pertenece al cliente autenticado)

**Ejemplo cURL:**
```bash
curl -X GET "http://localhost:5229/api/cliente/facturas/211" \
  -H "Authorization: Bearer {token}"
```

---

### Facturas Pendientes

**Endpoint:** `GET /api/cliente/facturas/pendientes`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Descripción:** Lista solo las facturas pendientes de pago

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 260,
      "numero": "0062-AlfonsoAnt-122025-STR",
      "monto": 436.0,
      "saldoPendiente": 436.0,
      "fechaCreacion": "2026-01-01T02:12:49.058801",
      "mesFacturacion": "2025-12-01T00:00:00",
      "categoria": "Streaming"
    }
  ]
}
```

**Ejemplo cURL:**
```bash
curl -X GET "http://localhost:5229/api/cliente/facturas/pendientes" \
  -H "Authorization: Bearer {token}"
```

---

### Descargar PDF de Factura

**Endpoint:** `GET /api/cliente/facturas/{id}/pdf`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Path Parameters:**
- `id` - ID de la factura

**Response (200):**
- Content-Type: `application/pdf`
- Body: Archivo PDF binario
- Headers: `Content-Disposition: attachment; filename="Factura_{numero}.pdf"`

**Response Error (404):**
```json
{
  "success": false,
  "message": "Factura no encontrada"
}
```

**Ejemplo cURL:**
```bash
curl -X GET "http://localhost:5229/api/cliente/facturas/211/pdf" \
  -H "Authorization: Bearer {token}" \
  -o factura.pdf
```

---

## 💰 Pagos

### Listar Pagos

**Endpoint:** `GET /api/cliente/pagos`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Query Parameters (todos opcionales):**
- `fechaInicio` - Fecha inicio (formato: YYYY-MM-DD)
- `fechaFin` - Fecha fin (formato: YYYY-MM-DD)
- `pagina` - Número de página (default: 1)
- `tamanoPagina` - Tamaño de página (default: 20)

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 123,
      "monto": 920.0,
      "moneda": "NIO",
      "tipoPago": "Fisico",
      "fechaPago": "2025-12-15T10:30:00",
      "facturas": [
        {
          "id": 211,
          "numero": "0143-ErvinDioni-122025"
        }
      ]
    }
  ],
  "pagination": {
    "currentPage": 1,
    "totalPages": 1,
    "totalItems": 1,
    "pageSize": 20
  }
}
```

**Ejemplo cURL:**
```bash
# Todos los pagos
curl -X GET "http://localhost:5229/api/cliente/pagos" \
  -H "Authorization: Bearer {token}"

# Filtrar por fecha
curl -X GET "http://localhost:5229/api/cliente/pagos?fechaInicio=2025-12-01&fechaFin=2025-12-31" \
  -H "Authorization: Bearer {token}"
```

---

### Detalle de Pago

**Endpoint:** `GET /api/cliente/pagos/{id}`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Path Parameters:**
- `id` - ID del pago

**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": 123,
    "monto": 920.0,
    "moneda": "NIO",
    "tipoPago": "Fisico",
    "banco": null,
    "fechaPago": "2025-12-15T10:30:00",
    "observaciones": null,
    "factura": {
      "id": 211,
      "numero": "0143-ErvinDioni-122025",
      "monto": 920.0
    },
    "facturas": []
  }
}
```

**Ejemplo cURL:**
```bash
curl -X GET "http://localhost:5229/api/cliente/pagos/123" \
  -H "Authorization: Bearer {token}"
```

---

## 👤 Perfil

**Endpoint:** `GET /api/cliente/perfil`

**Headers requeridos:**
```
Authorization: Bearer {token}
```

**Descripción:** Obtiene información del cliente y servicios activos

**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": 35,
    "codigo": "EMS_763870",
    "nombre": "Ervin Dionisio Olivas García",
    "telefono": "86459386",
    "email": null,
    "cedula": "284-180498-0000W",
    "activo": true,
    "fechaCreacion": "2025-12-05T05:26:00",
    "servicios": [
      {
        "id": 1,
        "nombre": "Plan 1",
        "descripcion": "Internet Residencial de hasta 10Mbps, ilimitado.",
        "categoria": "Internet",
        "fechaInicio": "2025-12-12T07:19:48.365478"
      }
    ]
  }
}
```

**Ejemplo cURL:**
```bash
curl -X GET "http://localhost:5229/api/cliente/perfil" \
  -H "Authorization: Bearer {token}"
```

---

## 🔒 Seguridad

### Autenticación JWT

Todos los endpoints (excepto `/auth/login`) requieren autenticación mediante JWT Bearer Token.

**Formato del Header:**
```
Authorization: Bearer {token}
```

**Duración del Token:**
- Por defecto: 7 días (604800 segundos)
- Configurable en `JwtSettings`

### Filtrado de Datos

**IMPORTANTE**: Todos los endpoints filtran automáticamente los datos por el cliente autenticado. Un cliente solo puede ver sus propios datos:

- ✅ Sus propias facturas
- ✅ Sus propios pagos
- ✅ Su propio perfil
- ❌ NO puede ver datos de otros clientes

Si un cliente intenta acceder a una factura de otro cliente, recibirá un error 403 Forbidden.

---

## 📝 Códigos de Estado HTTP

| Código | Descripción |
|--------|-------------|
| 200 | OK - Solicitud exitosa |
| 400 | Bad Request - Datos inválidos |
| 401 | Unauthorized - Token inválido o falta autenticación |
| 403 | Forbidden - No tiene permiso para acceder al recurso |
| 404 | Not Found - Recurso no encontrado |
| 500 | Internal Server Error - Error del servidor |

---

## 🚀 Flujo Típico de Uso

### 1. Login
```bash
# Cliente ingresa su código
POST /api/cliente/auth/login
Body: { "codigo": "EMS_617015" }

# Recibe token
Response: { "token": "eyJhbGci...", ... }
```

### 2. Usar Token en Requests
```bash
# Guardar token
TOKEN="eyJhbGci..."

# Usar en todas las peticiones
GET /api/cliente/dashboard
Headers: Authorization: Bearer {TOKEN}
```

### 3. Ver Facturas
```bash
# Listar todas
GET /api/cliente/facturas
Headers: Authorization: Bearer {TOKEN}

# Ver detalle
GET /api/cliente/facturas/211
Headers: Authorization: Bearer {TOKEN}

# Descargar PDF
GET /api/cliente/facturas/211/pdf
Headers: Authorization: Bearer {TOKEN}
```

### 4. Ver Pagos
```bash
GET /api/cliente/pagos
Headers: Authorization: Bearer {TOKEN}
```

### 5. Ver Perfil
```bash
GET /api/cliente/perfil
Headers: Authorization: Bearer {TOKEN}
```

---

## 🧪 Ejemplos de Prueba

### Ejemplo Completo en JavaScript/TypeScript

```javascript
const API_BASE = 'http://localhost:5229/api/cliente';

// 1. Login
async function login(codigo) {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ codigo })
  });
  const data = await response.json();
  return data.data.token;
}

// 2. Obtener Dashboard
async function getDashboard(token) {
  const response = await fetch(`${API_BASE}/dashboard`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
  return await response.json();
}

// 3. Obtener Facturas
async function getFacturas(token, estado = null) {
  const url = estado 
    ? `${API_BASE}/facturas?estado=${estado}`
    : `${API_BASE}/facturas`;
    
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
  return await response.json();
}

// Uso
(async () => {
  const token = await login('EMS_763870');
  const dashboard = await getDashboard(token);
  const facturas = await getFacturas(token, 'Pendiente');
  console.log(dashboard, facturas);
})();
```

---

## ✅ Endpoints Disponibles

| Método | Endpoint | Descripción | Auth Requerida |
|--------|----------|-------------|----------------|
| POST | `/api/cliente/auth/login` | Iniciar sesión | ❌ |
| GET | `/api/cliente/auth/verificar` | Verificar token | ✅ |
| GET | `/api/cliente/dashboard` | Dashboard/Resumen | ✅ |
| GET | `/api/cliente/facturas` | Listar facturas | ✅ |
| GET | `/api/cliente/facturas/{id}` | Detalle factura | ✅ |
| GET | `/api/cliente/facturas/pendientes` | Facturas pendientes | ✅ |
| GET | `/api/cliente/facturas/{id}/pdf` | Descargar PDF | ✅ |
| GET | `/api/cliente/pagos` | Listar pagos | ✅ |
| GET | `/api/cliente/pagos/{id}` | Detalle pago | ✅ |
| GET | `/api/cliente/perfil` | Perfil del cliente | ✅ |

---

## 📌 Notas Importantes

1. **Base de Datos**: No se crearon nuevas tablas. Se utiliza la estructura existente.

2. **Servicios**: Se reutilizan los servicios existentes (`IFacturaService`, `IPagoService`, `IClienteService`, etc.)

3. **JWT**: Usa la misma configuración JWT que la API móvil interna, pero con claims específicos para clientes.

4. **CORS**: Si necesitas usar esta API desde una aplicación web, asegúrate de configurar CORS en `Program.cs`.

5. **Producción**: Recuerda cambiar la URL base cuando despliegues a producción.

---

## 🔧 Configuración

No requiere configuración adicional. La API está lista para usar con la configuración actual del sistema.

**Puerto por defecto (desarrollo):** `5229`  
**Puerto HTTPS:** `7121`

---

**Última actualización:** 2026-01-01  
**Versión API:** 1.0.0

