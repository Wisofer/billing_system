# 🌐 Landing Page Editable - Sistema EMSINET

## 📋 Resumen

Este sistema permite administrar completamente el contenido de la landing page desde el panel de administración, sin necesidad de editar código.

## 🎯 Módulos Editables

### 1. Servicios de Internet 📡
**Administración:** `/servicios-landing`
**API:** `GET /api/landing/servicios`
**Tabla:** `ServiciosLandingPage`

Crea y administra los planes de internet que se muestran en la landing page.

**Campos disponibles:**
- Título del plan
- Descripción
- Precio (C$)
- Velocidad (ej: "10Mbps")
- Etiqueta especial (ej: "OFERTA DICIEMBRE")
- Color de etiqueta
- Icono (emoji)
- Características (JSON array)
- Orden de visualización
- Activo/Inactivo
- Destacado

### 2. Métodos de Pago 💳
**Administración:** `/metodos-pago`
**API:** `GET /api/landing/metodos-pago`
**Tabla:** `MetodosPago`

Administra las cuentas bancarias donde los clientes pueden realizar pagos.

**Campos disponibles:**
- Nombre del banco
- Icono (emoji)
- Tipo de cuenta (Córdobas, Dólares, Billetera Móvil)
- Moneda
- Número de cuenta
- Mensaje adicional
- Orden de visualización
- Activo/Inactivo

## 📡 API para React

### Endpoint unificado
```http
GET /api/landing/info
```
Retorna servicios + métodos de pago en una sola llamada.

### Endpoints individuales
```http
GET /api/landing/servicios
GET /api/landing/metodos-pago
```

## 🔐 Acceso

- Todos los endpoints de administración requieren rol **Administrador**
- Los endpoints de API son **públicos** (no requieren autenticación)

## 🚀 Uso desde React

```javascript
// Obtener toda la información
const response = await fetch('http://tu-dominio.com/api/landing/info');
const { success, data } = await response.json();

if (success) {
  const { servicios, metodosPago } = data;
  // Renderizar en tus componentes
}
```

## ⚠️ IMPORTANTE

Los **Servicios de Internet** de la landing page son **INDEPENDIENTES** de los servicios internos del sistema de facturación.

- **`/servicios`** → Sistema interno de facturación
- **`/servicios-landing`** → Landing page pública

No confundir ambos módulos.

## 📝 Datos de Ejemplo

El sistema inicializa automáticamente con:
- 5 servicios de internet de ejemplo
- 6 métodos de pago (Banpro, Lafise, BAC)

Estos pueden ser editados o eliminados desde el panel de administración.
