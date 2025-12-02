# 📋 Escenarios del Sistema de Facturación

## 🔄 Lógica del Sistema: "Primero Consume, Luego Paga"

**Regla fundamental:** El cliente consume el servicio durante un mes y paga en el mes siguiente.

---

## 📅 Escenario 1: Cliente Nuevo - Entra el Día 3 de Noviembre

### Datos del Cliente:
- **Nombre:** Juan Pérez
- **Fecha de Creación:** 3 de noviembre de 2025
- **Servicio:** Plan de hasta 10Mbps (C$ 920.00)
- **Ciclo de facturación:** Del 5 al 5 de cada mes

### Análisis:
- ✅ Cliente entró el día **3** (antes del día 5)
- ✅ Es su **primera factura** (usuario nuevo)
- ✅ Entró en el mes de facturación (noviembre)

### Cálculo:
- **Días facturados:** Mes completo (30 días)
- **Costo por día:** C$ 920.00 ÷ 30 = C$ 30.67
- **Monto a pagar:** 30 × C$ 30.67 = **C$ 920.00** (mes completo)

### Factura Generada:
- **Mes facturado:** NOV/2025
- **Fecha de generación:** 1 de diciembre de 2025 (a las 2am)
- **Días facturados:** 30 días
- **Monto:** C$ 920.00
- **Descuento proporcional:** C$ 0.00
- **Total:** C$ 920.00

---

## 📅 Escenario 2: Cliente Nuevo - Entra el Día 13 de Noviembre

### Datos del Cliente:
- **Nombre:** María González
- **Fecha de Creación:** 13 de noviembre de 2025
- **Servicio:** Plan de hasta 10Mbps (C$ 920.00)
- **Ciclo de facturación:** Del 5 al 5 de cada mes

### Análisis:
- ✅ Cliente entró el día **13** (después del día 5)
- ✅ Es su **primera factura** (usuario nuevo)
- ✅ Entró en el mes de facturación (noviembre)

### Cálculo:
- **Días facturados:** Del 13 al 30 de noviembre = **18 días**
- **Costo por día:** C$ 920.00 ÷ 30 = C$ 30.67
- **Monto proporcional:** 18 × C$ 30.67 = **C$ 552.00**

### Factura Generada:
- **Mes facturado:** NOV/2025
- **Fecha de generación:** 1 de diciembre de 2025 (a las 2am)
- **Días facturados:** 18 días
- **Sub-total:** C$ 920.00
- **Descuento proporcional:** C$ -368.00
- **Total:** C$ 552.00

---

## 📅 Escenario 3: Cliente Nuevo - Entra el Día 19 de Noviembre

### Datos del Cliente:
- **Nombre:** Carlos Ramírez
- **Fecha de Creación:** 19 de noviembre de 2025
- **Servicio:** Plan de hasta 10Mbps (C$ 920.00)
- **Ciclo de facturación:** Del 5 al 5 de cada mes

### Cálculo:
- **Días facturados:** Del 19 al 30 de noviembre = **12 días**
- **Costo por día:** C$ 920.00 ÷ 30 = C$ 30.67
- **Monto proporcional:** 12 × C$ 30.67 = **C$ 368.00**

### Factura Generada:
- **Mes facturado:** NOV/2025
- **Días facturados:** 12 días
- **Sub-total:** C$ 920.00
- **Descuento proporcional:** C$ -552.00
- **Total:** C$ 368.00

---

## 📅 Escenario 4: Cliente Viejo (Ya Tiene Facturas Previas)

### Datos del Cliente:
- **Nombre:** Ana Martínez
- **Fecha de Creación:** 15 de octubre de 2025
- **Servicio:** Plan de hasta 10Mbps (C$ 920.00)
- **Historial:** Ya tiene factura de octubre y noviembre

### Análisis:
- ❌ **NO es su primera factura** (usuario viejo)
- ✅ Ya pagó proporcional en su primera factura (octubre)

### Cálculo:
- **Regla:** Usuarios viejos **SIEMPRE** pagan mes completo
- **Monto a pagar:** **C$ 920.00** (mes completo)

### Factura Generada (Diciembre):
- **Mes facturado:** NOV/2025
- **Fecha de generación:** 1 de diciembre de 2025 (a las 2am)
- **Días facturados:** 30 días (mes completo)
- **Monto:** C$ 920.00
- **Descuento proporcional:** C$ 0.00
- **Total:** C$ 920.00

---

## 📅 Escenario 5: Cliente con Servicio de Streaming

### Datos del Cliente:
- **Nombre:** Luis Fernández
- **Fecha de Creación:** 20 de noviembre de 2025
- **Servicio:** Netflix (C$ 80.00)
- **Categoría:** Streaming

### Análisis:
- ❌ **Streaming NO aplica proporcional** (solo Internet)
- ✅ Streaming **SIEMPRE** paga precio completo

### Cálculo:
- **Monto a pagar:** **C$ 80.00** (precio completo, sin importar cuándo entró)

### Factura Generada:
- **Mes facturado:** NOV/2025
- **Monto:** C$ 80.00
- **Descuento proporcional:** C$ 0.00 (no aplica)
- **Total:** C$ 80.00

---

## 📅 Escenario 6: Generación Automática de Facturas

### Fecha Actual: 1 de Diciembre de 2025, 2:00 AM

### Proceso Automático:
1. **Sistema detecta:** Es día 1 del mes a las 2am
2. **Calcula mes de facturación:** Diciembre - 1 = **Noviembre 2025**
3. **Busca clientes activos:** Todos los clientes con servicios activos
4. **Genera facturas:**
   - Cliente A (entró el 3 de nov): C$ 920.00 (mes completo)
   - Cliente B (entró el 13 de nov): C$ 552.00 (18 días)
   - Cliente C (entró el 19 de nov): C$ 368.00 (12 días)
   - Cliente D (usuario viejo): C$ 920.00 (mes completo)
   - Cliente E (Streaming): C$ 80.00 (precio completo)

### Resultado:
- ✅ Todas las facturas se generan para **noviembre 2025**
- ✅ Los clientes pagarán en **diciembre 2025**
- ✅ El sistema aplica proporcional solo a usuarios nuevos de Internet

---

## 📅 Escenario 7: Filtro de Facturas en la Vista

### Fecha Actual: 1 de Diciembre de 2025

### Comportamiento del Filtro:
- **Mes por defecto:** Noviembre 2025 (mes anterior)
- **Año por defecto:** 2025
- **Razón:** Primero consume (noviembre), luego paga (diciembre)

### Ejemplo de Uso:
1. Usuario entra a la vista de facturas
2. El filtro muestra automáticamente: **"noviembre 2025"**
3. Se muestran todas las facturas de noviembre
4. El usuario puede cambiar el filtro si necesita ver otros meses

---

## 📅 Escenario 8: Cliente que Entra el Último Día del Mes

### Datos del Cliente:
- **Nombre:** Pedro Sánchez
- **Fecha de Creación:** 30 de noviembre de 2025
- **Servicio:** Plan de hasta 10Mbps (C$ 920.00)

### Cálculo:
- **Días facturados:** Del 30 al 30 de noviembre = **1 día**
- **Costo por día:** C$ 920.00 ÷ 30 = C$ 30.67
- **Monto proporcional:** 1 × C$ 30.67 = **C$ 30.67**

### Factura Generada:
- **Mes facturado:** NOV/2025
- **Días facturados:** 1 día
- **Sub-total:** C$ 920.00
- **Descuento proporcional:** C$ -889.33
- **Total:** C$ 30.67

---

## 📊 Resumen de Reglas

### ✅ Se Aplica Proporcional:
1. Cliente **nuevo** (primera factura)
2. Servicio de **Internet** (no Streaming)
3. Cliente entró **después del día 5** del mes de facturación
4. Cliente entró **dentro del mes** de facturación

### ❌ NO Se Aplica Proporcional:
1. Cliente **viejo** (ya tiene facturas previas) → Paga mes completo
2. Servicio de **Streaming** → Siempre precio completo
3. Cliente entró el día **5 o antes** → Paga mes completo
4. Cliente entró **antes del mes** de facturación → Paga mes completo

### 🔄 Ciclo de Facturación:
- **Ciclo:** Del día 5 al día 5 (30 días)
- **Costo por día:** Precio del servicio ÷ 30 días
- **Días facturados:** Solo días dentro del mes de facturación

---

## 💡 Ejemplos de Cálculo Rápido

### Fórmula:
```
Costo por Día = Precio del Servicio ÷ 30 días
Monto Proporcional = Días Facturados × Costo por Día
Descuento = Precio Completo - Monto Proporcional
```

### Ejemplos:
- **Precio:** C$ 920.00
- **Costo por día:** C$ 30.67

| Día de Entrada | Días Facturados | Monto a Pagar |
|----------------|-----------------|---------------|
| 1-5            | 30 días         | C$ 920.00     |
| 6              | 25 días         | C$ 766.75     |
| 10             | 21 días         | C$ 644.07     |
| 13             | 18 días         | C$ 552.06     |
| 19             | 12 días         | C$ 368.04     |
| 25             | 6 días          | C$ 184.02     |
| 30             | 1 día           | C$ 30.67      |

---

## 🎯 Conclusión

El sistema funciona con la lógica de **"primero consume, luego paga"**:
- En **diciembre** se factura lo consumido en **noviembre**
- El filtro muestra el **mes anterior** por defecto
- El proporcional solo aplica a **usuarios nuevos de Internet** que entraron después del día 5

