# 🌐 API para Landing Page

Este documento describe los endpoints de la API para la landing page en React.

## ⚠️ IMPORTANTE

Los servicios de la landing page son **INDEPENDIENTES** de los servicios internos del sistema de facturación.

- **Servicios del Sistema** (`/servicios`): Para facturación interna
- **Servicios Landing** (`/servicios-landing`): Para mostrar en la landing page pública

## 📡 Endpoints Disponibles

### 1. Obtener Servicios de Internet

```http
GET /api/landing/servicios
```

**Respuesta:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "titulo": "Plan de hasta 10Mbps",
      "descripcion": "Servicio de Internet Residencial hasta 10mbps.",
      "precio": 920.00,
      "velocidad": "10Mbps",
      "etiqueta": null,
      "colorEtiqueta": null,
      "icono": "📡",
      "caracteristicas": null,
      "orden": 1,
      "destacado": false,
      "activo": true
    }
  ]
}
```

### 2. Obtener Métodos de Pago

```http
GET /api/landing/metodos-pago
```

**Respuesta:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "nombreBanco": "Banpro",
      "icono": "🏦",
      "tipoCuenta": "Córdobas",
      "moneda": "C$",
      "numeroCuenta": "10020200333635",
      "mensaje": null,
      "orden": 1,
      "activo": true
    }
  ]
}
```

### 3. Obtener Todo (Servicios + Métodos de Pago)

```http
GET /api/landing/info
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "servicios": [...],
    "metodosPago": [...]
  }
}
```

## 🔧 Administración desde el Sistema

### Servicios de Internet (Landing Page)

Los servicios de la landing page se administran desde:
- **Ruta:** `/servicios-landing`
- **Controlador:** `ServiciosLandingPageController` (Web)
- **Tabla:** `ServiciosLandingPage` (independiente)
- CRUD completo disponible para administradores

### Métodos de Pago

Los métodos de pago se administran desde:
- **Ruta:** `/metodos-pago`
- **Controlador:** `MetodosPagoController` (Web)
- **Tabla:** `MetodosPago`
- CRUD completo disponible para administradores

## 🎨 Campos Editables

### Servicios de Internet
- ✅ Título
- ✅ Descripción
- ✅ Precio
- ✅ Velocidad (ej: "10Mbps", "20Mbps")
- ✅ Etiqueta (ej: "OFERTA DICIEMBRE")
- ✅ Color de Etiqueta (Tailwind CSS)
- ✅ Icono (emoji)
- ✅ Características (JSON array)
- ✅ Orden
- ✅ Destacado
- ✅ Estado (Activo/Inactivo)

### Métodos de Pago
- ✅ Nombre del Banco
- ✅ Icono (emoji)
- ✅ Tipo de Cuenta
- ✅ Moneda
- ✅ Número de Cuenta
- ✅ Mensaje Adicional
- ✅ Orden de Visualización
- ✅ Estado (Activo/Inactivo)

## 🔐 Seguridad

- **API Pública:** Los endpoints de `/api/landing/*` son públicos (no requieren autenticación)
- **Administración:** Los endpoints de administración requieren rol de Administrador
- **CORS:** Configurar CORS en `Program.cs` si la landing page está en otro dominio

## 📝 Ejemplo de Uso en React

```javascript
// Obtener servicios y métodos de pago
const response = await fetch('http://tu-dominio.com/api/landing/info');
const { success, data } = await response.json();

if (success) {
  const { servicios, metodosPago } = data;
  // Renderizar en tu componente React
}
```

## 🚀 Próximos Pasos

Si necesitas agregar más funcionalidades editables:
1. Crear nueva entidad en `Models/Entities/`
2. Crear servicio en `Services/`
3. Agregar endpoint en `Controllers/Api/LandingPageController.cs`
4. Crear controlador Web para CRUD en `Controllers/Web/`
5. Crear vistas en `Views/`

