/**
 * Sistema de Registro de Pagos - JavaScript Modular
 * Organizado por funcionalidades
 */

// ============================================
// CONFIGURACIÓN GLOBAL
// ============================================
const PagoSystem = {
    facturasCargadas: [],
    tipoCambio: 36.80,
    clienteSeleccionado: null,
    
    init() {
        this.setupEventListeners();
        this.inicializarCampos();
    },
    
    setupEventListeners() {
        // Event listeners para campos de moneda
        ['MontoCordobasFisico', 'MontoDolaresFisico', 'MontoCordobasElectronico', 'MontoDolaresElectronico'].forEach(id => {
            const campo = document.getElementById(id);
            if (campo) {
                campo.addEventListener('input', () => {
                    // Si es modo Mixto y se cambió un campo físico, calcular automáticamente el electrónico
                    const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
                    if (tipoPago === 'Mixto' && (id === 'MontoCordobasFisico' || id === 'MontoDolaresFisico')) {
                        CamposPagoManager.calcularMontoElectronicoAutomatico();
                    }
                    
                    // Usar directamente PagoManager para evitar problemas de contexto
                    PagoManager.calcularTotalPago();
                    if (id.includes('Fisico')) {
                        PagoManager.calcularVueltoFisico();
                    }
                });
            }
        });
        
        // Event listener para monto recibido
        const montoRecibidoFisico = document.getElementById('MontoRecibidoFisico');
        if (montoRecibidoFisico) {
            montoRecibidoFisico.addEventListener('input', () => {
                PagoManager.calcularVueltoFisico();
            });
        }
    },
    
    inicializarCampos() {
        if (window.mostrarCamposPago) {
            window.mostrarCamposPago();
        }
    }
};

// ============================================
// GESTIÓN DE CLIENTES
// ============================================
const ClienteManager = {
    todosLosClientes: [],
    clienteSeleccionado: null,
    selectedIndex: -1,
    
    init(clientesJson) {
        this.todosLosClientes = clientesJson;
        this.setupEventListeners();
    },
    
    setupEventListeners() {
        const clienteSearch = document.getElementById('clienteSearch');
        const clearClienteBtn = document.getElementById('clearClienteBtn');
        
        if (!clienteSearch) return;
        
        clienteSearch.addEventListener('input', (e) => this.buscarCliente(e.target.value));
        clienteSearch.addEventListener('focus', () => {
            if (clienteSearch.value.trim() && !this.clienteSeleccionado) {
                this.buscarCliente(clienteSearch.value);
            }
        });
        clienteSearch.addEventListener('keydown', (e) => this.manejarTeclado(e));
        
        if (clearClienteBtn) {
            clearClienteBtn.addEventListener('click', () => this.limpiarSeleccion());
        }
        
        // Cerrar dropdown al hacer click fuera
        document.addEventListener('click', (e) => {
            const clienteDropdown = document.getElementById('clienteDropdown');
            if (!clienteSearch.contains(e.target) && !clienteDropdown?.contains(e.target)) {
                clienteDropdown?.classList.add('hidden');
            }
        });
    },
    
    buscarCliente(termino) {
        if (!termino || termino.trim() === '') {
            document.getElementById('clienteDropdown')?.classList.add('hidden');
            return;
        }
        
        const clientesFiltrados = this.filtrarClientes(termino);
        this.mostrarResultados(clientesFiltrados);
    },
    
    filtrarClientes(termino) {
        const terminoLower = termino.toLowerCase().trim();
        return this.todosLosClientes.filter(cliente => {
            const codigo = (cliente.codigo || '').toLowerCase();
            const nombre = (cliente.nombre || '').toLowerCase();
            return codigo.includes(terminoLower) || nombre.includes(terminoLower);
        });
    },
    
    mostrarResultados(clientesFiltrados) {
        const clienteDropdownContent = document.getElementById('clienteDropdownContent');
        const clienteDropdown = document.getElementById('clienteDropdown');
        
        if (!clienteDropdownContent || !clienteDropdown) return;
        
        clienteDropdownContent.innerHTML = '';
        
        if (clientesFiltrados.length === 0) {
            const noResults = document.createElement('div');
            noResults.className = 'px-4 py-3 text-sm text-gray-500 dark:text-gray-400 text-center';
            noResults.textContent = 'No se encontraron clientes';
            clienteDropdownContent.appendChild(noResults);
            clienteDropdown.classList.remove('hidden');
            return;
        }
        
        clientesFiltrados.forEach((cliente, index) => {
            const item = document.createElement('div');
            item.className = 'px-4 py-2 hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer transition-colors cliente-item';
            item.dataset.clienteId = cliente.id;
            item.dataset.index = index;
            
            const codigo = document.createElement('span');
            codigo.className = 'font-medium text-gray-900 dark:text-white';
            codigo.textContent = cliente.codigo + ' - ';
            
            const nombre = document.createElement('span');
            nombre.className = 'text-gray-700 dark:text-gray-300';
            nombre.textContent = cliente.nombre;
            
            item.appendChild(codigo);
            item.appendChild(nombre);
            
            item.addEventListener('click', () => this.seleccionarCliente(cliente));
            item.addEventListener('mouseenter', () => {
                clienteDropdownContent.querySelectorAll('.cliente-item').forEach(el => {
                    el.classList.remove('bg-blue-50', 'dark:bg-blue-900');
                });
                item.classList.add('bg-blue-50', 'dark:bg-blue-900');
                this.selectedIndex = index;
            });
            
            clienteDropdownContent.appendChild(item);
        });
        
        clienteDropdown.classList.remove('hidden');
        this.selectedIndex = -1;
    },
    
    seleccionarCliente(cliente) {
        this.clienteSeleccionado = cliente;
        document.getElementById('clienteId').value = cliente.id;
        document.getElementById('clienteSearch').value = cliente.codigo + ' - ' + cliente.nombre;
        document.getElementById('clienteDropdown')?.classList.add('hidden');
        document.getElementById('clearClienteBtn')?.classList.remove('hidden');
        
        if (window.cargarFacturas) {
            window.cargarFacturas(cliente.id);
        }
    },
    
    limpiarSeleccion() {
        this.clienteSeleccionado = null;
        document.getElementById('clienteId').value = '';
        document.getElementById('clienteSearch').value = '';
        document.getElementById('clienteDropdown')?.classList.add('hidden');
        document.getElementById('clearClienteBtn')?.classList.add('hidden');
        FacturaManager.limpiarFacturas();
    },
    
    manejarTeclado(e) {
        const items = document.querySelectorAll('.cliente-item');
        if (items.length === 0) return;
        
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            this.selectedIndex = (this.selectedIndex + 1) % items.length;
            items[this.selectedIndex].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            items[this.selectedIndex].classList.add('bg-blue-50', 'dark:bg-blue-900');
            items.forEach((item, idx) => {
                if (idx !== this.selectedIndex) {
                    item.classList.remove('bg-blue-50', 'dark:bg-blue-900');
                }
            });
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            this.selectedIndex = this.selectedIndex <= 0 ? items.length - 1 : this.selectedIndex - 1;
            items[this.selectedIndex].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            items[this.selectedIndex].classList.add('bg-blue-50', 'dark:bg-blue-900');
            items.forEach((item, idx) => {
                if (idx !== this.selectedIndex) {
                    item.classList.remove('bg-blue-50', 'dark:bg-blue-900');
                }
            });
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (this.selectedIndex >= 0 && items[this.selectedIndex]) {
                const clienteId = items[this.selectedIndex].dataset.clienteId;
                const cliente = this.todosLosClientes.find(c => c.id == clienteId);
                if (cliente) {
                    this.seleccionarCliente(cliente);
                }
            }
        } else if (e.key === 'Escape') {
            document.getElementById('clienteDropdown')?.classList.add('hidden');
        }
    }
};

// ============================================
// GESTIÓN DE FACTURAS
// ============================================
const FacturaManager = {
    facturasCargadas: [],
    
    cargarFacturas(clienteId) {
        if (!clienteId) {
            this.limpiarFacturas();
            return;
        }
        
        fetch(`/pagos/facturas-cliente/${clienteId}`)
            .then(response => response.json())
            .then(data => this.procesarFacturas(data))
            .catch(error => {
                console.error('Error al cargar facturas:', error);
                document.getElementById('sinFacturasMensaje').textContent = 'Error al cargar las facturas';
                document.getElementById('sinFacturasMensaje').classList.remove('hidden');
            });
    },
    
    procesarFacturas(data) {
        const facturasList = document.getElementById('facturasList');
        const sinFacturasMensaje = document.getElementById('sinFacturasMensaje');
        const facturasContainer = document.getElementById('facturasContainer');
        
        facturasList.innerHTML = '';
        this.facturasCargadas = [];
        
        const facturasConSaldo = data.filter(f => f.puedePagar);
        
        if (facturasConSaldo.length === 0) {
            facturasList.classList.add('hidden');
            sinFacturasMensaje.classList.remove('hidden');
            sinFacturasMensaje.textContent = 'No hay facturas con saldo pendiente para este cliente';
            PagoManager.actualizarMonto();
            return;
        }
        
        sinFacturasMensaje.classList.add('hidden');
        facturasList.classList.remove('hidden');
        
        // Crear header con botón "Seleccionar todas"
        const headerDiv = document.createElement('div');
        headerDiv.className = 'flex items-center justify-between mb-2 pb-2 border-b border-gray-200 dark:border-gray-600';
        
        const spanFacturas = document.createElement('span');
        spanFacturas.className = 'text-xs font-medium text-gray-700 dark:text-gray-300';
        spanFacturas.textContent = 'Facturas';
        
        const btnSeleccionarTodas = document.createElement('button');
        btnSeleccionarTodas.type = 'button';
        btnSeleccionarTodas.id = 'btnSeleccionarTodas';
        btnSeleccionarTodas.className = 'text-xs px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded hover:bg-blue-200 dark:hover:bg-blue-800';
        btnSeleccionarTodas.textContent = 'Seleccionar todas';
        btnSeleccionarTodas.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            FacturaManager.seleccionarTodas();
        });
        
        headerDiv.appendChild(spanFacturas);
        headerDiv.appendChild(btnSeleccionarTodas);
        facturasList.appendChild(headerDiv);
        
        // Crear contenedor para facturas
        const facturasContainerList = document.createElement('div');
        facturasContainerList.id = 'facturasContainerList';
        facturasContainerList.className = 'space-y-2';
        facturasList.appendChild(facturasContainerList);
        
        facturasConSaldo.forEach(factura => {
            const facturaId = parseInt(factura.id) || factura.id;
            const saldoPendiente = parseFloat(factura.saldoPendiente) || 0;
            const totalPagado = parseFloat(factura.totalPagado) || 0;
            const montoTotal = parseFloat(factura.monto) || 0;
            
            this.facturasCargadas.push({
                id: facturaId,
                numero: factura.numero,
                montoTotal: montoTotal,
                totalPagado: totalPagado,
                saldoPendiente: saldoPendiente
            });
            
            const facturaItem = this.crearItemFactura(factura, facturaId, saldoPendiente, totalPagado, montoTotal);
            facturasContainerList.appendChild(facturaItem);
        });
        
        PagoManager.actualizarMonto();
    },
    
    crearItemFactura(factura, facturaId, saldoPendiente, totalPagado, montoTotal) {
        const facturaItem = document.createElement('div');
        facturaItem.className = 'flex items-start space-x-2 p-2 rounded border border-gray-200 dark:border-gray-600 hover:border-blue-400 dark:hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900 transition-all cursor-pointer';
        
        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.name = 'facturaCheckbox';
        checkbox.value = facturaId;
        checkbox.id = `factura_${facturaId}`;
        checkbox.className = 'mt-0.5 w-4 h-4 text-blue-600 bg-gray-100 border border-gray-300 rounded focus:ring-blue-500 dark:focus:ring-blue-600 focus:ring-1 dark:bg-gray-700 dark:border-gray-600 cursor-pointer';
        checkbox.addEventListener('change', function() {
            // Usar función normal para mantener el contexto
            setTimeout(function() {
                PagoManager.actualizarMonto();
            }, 50);
        });
        
        const label = document.createElement('label');
        label.htmlFor = `factura_${facturaId}`;
        label.className = 'flex-1 cursor-pointer';
        
        let textoFactura = `${factura.numero} - C$ ${montoTotal.toFixed(2)}`;
        if (totalPagado > 0) {
            textoFactura += ` (Pagado: C$ ${totalPagado.toFixed(2)}, Pendiente: C$ ${saldoPendiente.toFixed(2)})`;
        } else {
            textoFactura += ` (Pendiente: C$ ${saldoPendiente.toFixed(2)})`;
        }
        
        label.innerHTML = `
            <div class="text-xs font-medium text-gray-900 dark:text-white">${textoFactura}</div>
            <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Saldo: C$ ${saldoPendiente.toFixed(2)}</div>
        `;
        
        // Hacer que todo el item sea clickeable
        facturaItem.addEventListener('click', (e) => {
            if (e.target !== checkbox && e.target !== label && !label.contains(e.target)) {
                checkbox.checked = !checkbox.checked;
                checkbox.dispatchEvent(new Event('change'));
            }
        });
        
        facturaItem.appendChild(checkbox);
        facturaItem.appendChild(label);
        
        return facturaItem;
    },
    
    seleccionarTodas() {
        const checkboxes = document.querySelectorAll('input[name="facturaCheckbox"]');
        if (checkboxes.length === 0) return;
        
        const todasSeleccionadas = Array.from(checkboxes).every(cb => cb.checked);
        
        checkboxes.forEach(checkbox => {
            checkbox.checked = !todasSeleccionadas;
            // Disparar evento change para actualizar el monto
            const event = new Event('change', { bubbles: true });
            checkbox.dispatchEvent(event);
        });
        
        const btnSeleccionarTodas = document.getElementById('btnSeleccionarTodas');
        if (btnSeleccionarTodas) {
            btnSeleccionarTodas.textContent = todasSeleccionadas ? 'Seleccionar todas' : 'Deseleccionar todas';
            btnSeleccionarTodas.className = todasSeleccionadas 
                ? 'text-xs px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded hover:bg-blue-200 dark:hover:bg-blue-800'
                : 'text-xs px-2 py-1 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300 rounded hover:bg-red-200 dark:hover:bg-red-800';
        }
        
        // Actualizar monto después de un pequeño delay
        setTimeout(() => {
            PagoManager.actualizarMonto();
        }, 50);
    },
    
    limpiarFacturas() {
        document.getElementById('facturasList').innerHTML = '';
        document.getElementById('facturasList').classList.add('hidden');
        document.getElementById('sinFacturasMensaje').classList.remove('hidden');
        document.getElementById('sinFacturasMensaje').textContent = 'Selecciona un cliente para ver las facturas disponibles';
        document.getElementById('facturasSeleccionadasCount').textContent = '(0 seleccionadas)';
        document.getElementById('Monto').value = '';
        document.getElementById('Monto').readOnly = true;
        this.facturasCargadas = [];
    }
};

// ============================================
// GESTIÓN DE PAGOS
// ============================================
const PagoManager = {
    actualizarMonto() {
        const checkboxes = document.querySelectorAll('input[name="facturaCheckbox"]:checked');
        const montoInput = document.getElementById('Monto');
        const facturasSeleccionadasCount = document.getElementById('facturasSeleccionadasCount');
        
        if (!montoInput) {
            console.error('Monto input no encontrado');
            return;
        }
        
        let montoTotal = 0;
        const facturaIds = [];
        
        checkboxes.forEach(checkbox => {
            const facturaId = parseInt(checkbox.value);
            if (isNaN(facturaId)) return;
            
            const factura = FacturaManager.facturasCargadas.find(f => {
                const fId = typeof f.id === 'number' ? f.id : parseInt(f.id);
                return fId === facturaId;
            });
            
            if (factura) {
                const saldo = parseFloat(factura.saldoPendiente) || 0;
                if (!isNaN(saldo) && saldo > 0) {
                    montoTotal += saldo;
                    facturaIds.push(facturaId);
                }
            }
        });
        
        const count = facturaIds.length;
        
        // Actualizar contador de facturas seleccionadas
        if (facturasSeleccionadasCount) {
            facturasSeleccionadasCount.textContent = `(${count})`;
        }
        
        // Actualizar campos hidden - CRÍTICO para la validación
        const facturaIdHidden = document.getElementById('FacturaId');
        const facturaIdsHidden = document.getElementById('FacturaIds');
        
        if (facturaIdHidden && facturaIdsHidden) {
            if (count === 1) {
                facturaIdHidden.value = facturaIds[0].toString();
                facturaIdsHidden.value = '';
            } else if (count > 1) {
                facturaIdHidden.value = '';
                facturaIdsHidden.value = facturaIds.join(',');
            } else {
                facturaIdHidden.value = '';
                facturaIdsHidden.value = '';
            }
        } else {
            console.error('Campos hidden de facturas no encontrados');
        }
        
        // Actualizar monto
        if (montoTotal > 0 && montoTotal <= 1000000) {
            const montoFormateado = Number(montoTotal.toFixed(2));
            montoInput.value = montoFormateado;
            montoInput.readOnly = true;
            montoInput.setAttribute('data-monto-esperado', montoFormateado);
            
            // Actualizar campos de pago si hay tipo de pago seleccionado
            if (window.mostrarCamposPago) {
                window.mostrarCamposPago();
            }
        } else if (count === 0) {
            montoInput.value = '';
            montoInput.readOnly = true;
            montoInput.removeAttribute('data-monto-esperado');
        } else {
            montoInput.value = '';
            montoInput.removeAttribute('data-monto-esperado');
        }
        
        // Debug: mostrar en consola para verificar
        console.log('actualizarMonto - Facturas:', count, 'IDs:', facturaIds, 'FacturaId:', facturaIdHidden?.value, 'FacturaIds:', facturaIdsHidden?.value);
    },
    
    calcularTotalPago() {
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        const moneda = document.getElementById('Moneda')?.value;
        const tipoCambioValor = PagoSystem.tipoCambio;
        
        let totalFisico = 0;
        let totalElectronico = 0;
        
        // Calcular total físico
        if (tipoPago === 'Fisico' || tipoPago === 'Mixto') {
            const montoCordobasFisicoInput = document.getElementById('MontoCordobasFisico');
            const montoDolaresFisicoInput = document.getElementById('MontoDolaresFisico');
            
            // Función auxiliar para convertir formato español (coma) a formato estándar (punto)
            const parseNumero = (valor) => {
                if (!valor || !valor.trim()) return 0;
                // Reemplazar coma por punto para formato español
                const valorNormalizado = valor.trim().replace(',', '.');
                return parseFloat(valorNormalizado) || 0;
            };
            
            // Leer valores, normalizando formato de coma a punto
            const montoCordobasFisico = parseNumero(montoCordobasFisicoInput?.value);
            const montoDolaresFisico = parseNumero(montoDolaresFisicoInput?.value);
            
            if (moneda === '$') {
                // Solo dólares: convertir a córdobas (ignorar completamente el campo de córdobas)
                totalFisico = montoDolaresFisico * tipoCambioValor;
            } else if (moneda === 'C$') {
                // Solo córdobas (ignorar completamente el campo de dólares)
                totalFisico = montoCordobasFisico;
            } else {
                // Ambos: sumar ambos
                totalFisico = montoCordobasFisico + (montoDolaresFisico * tipoCambioValor);
            }
            
            const totalFisicoValor = document.getElementById('totalFisicoValor');
            if (totalFisicoValor) {
                totalFisicoValor.textContent = `C$ ${totalFisico.toFixed(2)}`;
            }
        }
        
        // Calcular total electrónico
        if (tipoPago === 'Electronico' || tipoPago === 'Mixto') {
            const montoCordobasElectronicoInput = document.getElementById('MontoCordobasElectronico');
            const montoDolaresElectronicoInput = document.getElementById('MontoDolaresElectronico');
            
            // Función auxiliar para convertir formato español (coma) a formato estándar (punto)
            const parseNumero = (valor) => {
                if (!valor || !valor.trim()) return 0;
                // Reemplazar coma por punto para formato español
                const valorNormalizado = valor.trim().replace(',', '.');
                return parseFloat(valorNormalizado) || 0;
            };
            
            // Leer valores, normalizando formato de coma a punto
            const montoCordobasElectronico = parseNumero(montoCordobasElectronicoInput?.value);
            const montoDolaresElectronico = parseNumero(montoDolaresElectronicoInput?.value);
            
            if (moneda === '$') {
                totalElectronico = montoDolaresElectronico * tipoCambioValor;
            } else if (moneda === 'C$') {
                totalElectronico = montoCordobasElectronico;
            } else {
                totalElectronico = montoCordobasElectronico + (montoDolaresElectronico * tipoCambioValor);
            }
            
            const totalElectronicoValor = document.getElementById('totalElectronicoValor');
            if (totalElectronicoValor) {
                totalElectronicoValor.textContent = `C$ ${totalElectronico.toFixed(2)}`;
            }
        }
        
        // Mostrar resumen total para pago mixto
        const totalGeneral = totalFisico + totalElectronico;
        const totalGeneralValor = document.getElementById('totalGeneralValor');
        if (totalGeneralValor) {
            totalGeneralValor.textContent = `C$ ${totalGeneral.toFixed(2)}`;
        }
        
        const resumenTotal = document.getElementById('resumenTotal');
        if (resumenTotal) {
            if (tipoPago === 'Mixto' && totalGeneral > 0) {
                resumenTotal.style.display = 'block';
            } else {
                resumenTotal.style.display = 'none';
            }
        }
    },
    
    calcularVueltoFisico() {
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        if (tipoPago !== 'Fisico' && tipoPago !== 'Mixto') return;
        
        const parseNumero = (valor) => {
            if (!valor || !valor.trim()) return 0;
            const valorNormalizado = valor.trim().replace(',', '.');
            return parseFloat(valorNormalizado) || 0;
        };
        
        const moneda = document.getElementById('Moneda')?.value;
        const montoRecibidoFisico = parseNumero(document.getElementById('MontoRecibidoFisico')?.value);
        const montoCordobasFisicoInput = document.getElementById('MontoCordobasFisico');
        const montoDolaresFisicoInput = document.getElementById('MontoDolaresFisico');
        let montoCordobasFisico = parseNumero(montoCordobasFisicoInput?.value);
        const montoDolaresFisico = parseNumero(document.getElementById('MontoDolaresFisico')?.value);
        const tipoCambioValor = PagoSystem.tipoCambio;
        const montoInput = document.getElementById('Monto');
        const montoDebeTotal = parseNumero(montoInput?.value); // Siempre en C$
        
        let vueltoFisico = 0;
        
        // Si estamos en mixto y el usuario solo escribe "Recibido" pero dejó el Monto físico vacío,
        // asumir que el monto físico es lo que está recibiendo (hasta el máximo que debe).
        if (tipoPago === 'Mixto' && moneda === 'C$' && montoRecibidoFisico > 0 && (!montoCordobasFisico || montoCordobasFisico === 0)) {
            const montoAsignado = Math.min(montoRecibidoFisico, montoDebeTotal);
            montoCordobasFisico = montoAsignado;
            if (montoCordobasFisicoInput) {
                montoCordobasFisicoInput.value = montoAsignado.toFixed(2);
            }
        }
        
        // Determinar contra qué monto se calcula el vuelto:
        // - En Mixto con C$, el vuelto debe tomar como base SOLO el monto físico.
        // - En los demás casos (Físico puro o moneda en $), usa el total de la deuda.
        let baseParaVuelto = montoDebeTotal;
        if (tipoPago === 'Mixto' && moneda === 'C$' && montoCordobasFisico > 0) {
            baseParaVuelto = montoCordobasFisico;
        }
        
        if (montoRecibidoFisico > 0) {
            if (moneda === '$') {
                // Recibido se interpreta en dólares, convertir a C$
                const montoRecibidoEnCordobas = montoRecibidoFisico * tipoCambioValor;
                vueltoFisico = montoRecibidoEnCordobas > baseParaVuelto ? montoRecibidoEnCordobas - baseParaVuelto : 0;
            } else {
                // Recibido en córdobas
                vueltoFisico = montoRecibidoFisico > baseParaVuelto ? montoRecibidoFisico - baseParaVuelto : 0;
            }
        } else {
            // Si no se ingresó "Recibido", no calculamos vuelto (se queda en 0).
            vueltoFisico = 0;
        }
        
        // Redondeo inteligente: si el vuelto es mayor a 5, redondear a valores completos
        let vueltoFinal = vueltoFisico;
        if (vueltoFisico > 5) {
            vueltoFinal = Math.round(vueltoFisico);
        } else {
            vueltoFinal = Math.round(vueltoFisico * 100) / 100; // Mantener 2 decimales si es <= 5
        }
        
        const vueltoFisicoInput = document.getElementById('VueltoFisico');
        if (vueltoFisicoInput) {
            vueltoFisicoInput.value = vueltoFinal.toFixed(2);
        }
    },
    
    actualizarEtiquetaMontoRecibido() {
        const monedaRecibidoLabel = document.getElementById('monedaRecibidoLabel');
        if (!monedaRecibidoLabel) return;
        
        const montoCordobasFisico = parseFloat(document.getElementById('MontoCordobasFisico')?.value || 0);
        const montoDolaresFisico = parseFloat(document.getElementById('MontoDolaresFisico')?.value || 0);
        const monedaSeleccionada = document.getElementById('Moneda')?.value || '';
        
        if ((monedaSeleccionada === '$' && montoDolaresFisico > 0) || (montoDolaresFisico > 0 && montoCordobasFisico === 0)) {
            monedaRecibidoLabel.textContent = '($)';
        } else {
            monedaRecibidoLabel.textContent = '(C$)';
        }
    }
};

// ============================================
// GESTIÓN DE CAMPOS DE PAGO
// ============================================
const CamposPagoManager = {
    mostrarCamposPago() {
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        const moneda = document.getElementById('Moneda')?.value;
        const montoInput = document.getElementById('Monto');
        const montoTotal = parseFloat(montoInput?.value || 0);
        const tipoCambioValor = PagoSystem.tipoCambio;
        const detallesPago = document.getElementById('detallesPago');
        
        const camposFisico = document.getElementById('camposFisico');
        const camposElectronico = document.getElementById('camposElectronico');
        const camposVueltoFisico = document.getElementById('camposVueltoFisico');
        const banco = document.getElementById('Banco');
        const tipoCuenta = document.getElementById('TipoCuenta');
        const resumenTotal = document.getElementById('resumenTotal');
        
        // Mostrar sección de detalles si hay tipo de pago seleccionado
        if (tipoPago && detallesPago) {
            detallesPago.classList.remove('hidden');
        }
        
        // Mostrar/ocultar secciones según tipo de pago
        if (tipoPago === 'Fisico') {
            if (camposFisico) camposFisico.style.display = 'block';
            if (camposElectronico) camposElectronico.style.display = 'none';
            if (camposVueltoFisico) camposVueltoFisico.style.display = 'grid';
            if (banco) banco.removeAttribute('required');
            if (tipoCuenta) tipoCuenta.removeAttribute('required');
            if (resumenTotal) resumenTotal.style.display = 'none';
            
            this.preLlenarCamposFisicos(moneda, montoTotal, tipoCambioValor);
        } else if (tipoPago === 'Electronico') {
            if (camposFisico) camposFisico.style.display = 'none';
            if (camposElectronico) camposElectronico.style.display = 'block';
            if (banco) banco.setAttribute('required', 'required');
            if (tipoCuenta) tipoCuenta.setAttribute('required', 'required');
            if (resumenTotal) resumenTotal.style.display = 'none';
            
            this.preLlenarCamposElectronicos(moneda, montoTotal, tipoCambioValor);
        } else if (tipoPago === 'Mixto') {
            if (camposFisico) camposFisico.style.display = 'block';
            if (camposElectronico) camposElectronico.style.display = 'block';
            if (camposVueltoFisico) camposVueltoFisico.style.display = 'grid';
            if (banco) banco.setAttribute('required', 'required');
            if (tipoCuenta) tipoCuenta.setAttribute('required', 'required');
            if (resumenTotal) resumenTotal.style.display = 'block';
            
            // Para pago mixto, NO pre-llenar automáticamente
            // El usuario debe ingresar manualmente cuánto paga en físico y cuánto en electrónico
            // Limpiar campos si ambos tienen el monto total completo (fueron pre-llenados automáticamente)
            this.limpiarCamposMixtoSiNecesario(moneda, montoTotal, tipoCambioValor);
            
            // Si ya hay un monto físico ingresado, calcular automáticamente el electrónico
            setTimeout(() => {
                this.calcularMontoElectronicoAutomatico();
            }, 100);
        } else {
            if (detallesPago) detallesPago.classList.add('hidden');
            if (camposFisico) camposFisico.style.display = 'none';
            if (camposElectronico) camposElectronico.style.display = 'none';
            if (banco) banco.removeAttribute('required');
            if (tipoCuenta) tipoCuenta.removeAttribute('required');
            if (resumenTotal) resumenTotal.style.display = 'none';
        }
        
        PagoManager.calcularTotalPago();
        PagoManager.actualizarEtiquetaMontoRecibido();
    },
    
    preLlenarCamposFisicos(moneda, montoTotal, tipoCambioValor) {
        if (montoTotal <= 0) return;
        
        const montoCordobasFisico = document.getElementById('MontoCordobasFisico');
        const montoDolaresFisico = document.getElementById('MontoDolaresFisico');
        
        if (moneda === '$') {
            // Si la moneda es dólares, SIEMPRE limpiar córdobas y pre-llenar dólares
            if (montoCordobasFisico) {
                montoCordobasFisico.value = '';
            }
            if (montoDolaresFisico && (!montoDolaresFisico.value || parseFloat(montoDolaresFisico.value) === 0)) {
                montoDolaresFisico.value = (montoTotal / tipoCambioValor).toFixed(2);
            }
        } else if (moneda === 'C$') {
            // Si la moneda es córdobas, SIEMPRE limpiar dólares y pre-llenar córdobas
            if (montoDolaresFisico) {
                montoDolaresFisico.value = '';
            }
            if (montoCordobasFisico && (!montoCordobasFisico.value || parseFloat(montoCordobasFisico.value) === 0)) {
                montoCordobasFisico.value = montoTotal.toFixed(2);
            }
        }
    },
    
    preLlenarCamposElectronicos(moneda, montoTotal, tipoCambioValor) {
        if (montoTotal <= 0) return;
        
        const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
        const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
        
        if (moneda === '$') {
            // Si la moneda es dólares, SIEMPRE limpiar córdobas y pre-llenar dólares
            if (montoCordobasElectronico) {
                montoCordobasElectronico.value = '';
            }
            if (montoDolaresElectronico && (!montoDolaresElectronico.value || parseFloat(montoDolaresElectronico.value) === 0)) {
                montoDolaresElectronico.value = (montoTotal / tipoCambioValor).toFixed(2);
            }
        } else if (moneda === 'C$') {
            // Si la moneda es córdobas, SIEMPRE limpiar dólares y pre-llenar córdobas
            if (montoDolaresElectronico) {
                montoDolaresElectronico.value = '';
            }
            if (montoCordobasElectronico && (!montoCordobasElectronico.value || parseFloat(montoCordobasElectronico.value) === 0)) {
                montoCordobasElectronico.value = montoTotal.toFixed(2);
            }
        }
    },
    
    limpiarCamposMixtoSiNecesario(moneda, montoTotal, tipoCambioValor) {
        // Para pago mixto, si ambos campos tienen el monto total completo, limpiarlos
        // para que el usuario pueda ingresar la distribución manualmente
        
        const montoCordobasFisico = document.getElementById('MontoCordobasFisico');
        const montoDolaresFisico = document.getElementById('MontoDolaresFisico');
        const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
        const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
        
        // Función auxiliar para normalizar y comparar valores
        const parseNumero = (valor) => {
            if (!valor || !valor.trim()) return 0;
            const valorNormalizado = valor.trim().replace(',', '.');
            return parseFloat(valorNormalizado) || 0;
        };
        
        const montoTotalRedondeado = Math.round(montoTotal * 100) / 100;
        
        // Si el campo físico tiene el monto total completo y el electrónico también, limpiar ambos
        if (moneda === 'C$') {
            const montoFisico = parseNumero(montoCordobasFisico?.value);
            const montoElectronico = parseNumero(montoCordobasElectronico?.value);
            
            // Si ambos tienen el monto total (o muy cercano), limpiar ambos para que el usuario distribuya
            if (Math.abs(montoFisico - montoTotalRedondeado) < 0.01 && 
                Math.abs(montoElectronico - montoTotalRedondeado) < 0.01) {
                if (montoCordobasFisico) montoCordobasFisico.value = '';
                if (montoCordobasElectronico) montoCordobasElectronico.value = '';
            }
        } else if (moneda === '$') {
            const montoFisico = parseNumero(montoDolaresFisico?.value);
            const montoElectronico = parseNumero(montoDolaresElectronico?.value);
            const montoTotalEnDolares = montoTotal / tipoCambioValor;
            const montoTotalEnDolaresRedondeado = Math.round(montoTotalEnDolares * 100) / 100;
            
            // Si ambos tienen el monto total en dólares (o muy cercano), limpiar ambos
            if (Math.abs(montoFisico - montoTotalEnDolaresRedondeado) < 0.01 && 
                Math.abs(montoElectronico - montoTotalEnDolaresRedondeado) < 0.01) {
                if (montoDolaresFisico) montoDolaresFisico.value = '';
                if (montoDolaresElectronico) montoDolaresElectronico.value = '';
            }
        }
    },
    
    calcularMontoElectronicoAutomatico() {
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        if (tipoPago !== 'Mixto') return;
        
        const moneda = document.getElementById('Moneda')?.value;
        const montoInput = document.getElementById('Monto');
        if (!montoInput || !montoInput.value) return;
        
        const montoTotal = parseFloat(montoInput.value) || 0;
        const tipoCambioValor = PagoSystem.tipoCambio;
        
        // Función auxiliar para parsear números
        const parseNumero = (valor) => {
            if (!valor || !valor.trim()) return 0;
            const valorNormalizado = valor.trim().replace(',', '.');
            return parseFloat(valorNormalizado) || 0;
        };
        
        // Calcular monto físico actual
        let montoFisico = 0;
        if (moneda === 'C$') {
            montoFisico = parseNumero(document.getElementById('MontoCordobasFisico')?.value);
        } else if (moneda === '$') {
            const montoDolaresFisico = parseNumero(document.getElementById('MontoDolaresFisico')?.value);
            montoFisico = montoDolaresFisico * tipoCambioValor; // Convertir a córdobas
        }
        
        // Calcular monto electrónico automáticamente: Total - Físico
        const montoElectronico = Math.max(0, montoTotal - montoFisico);
        
        // Actualizar campo electrónico según la moneda
        if (moneda === 'C$') {
            const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
            if (montoCordobasElectronico && montoElectronico > 0) {
                // Solo actualizar si el campo está vacío o si el usuario no lo ha editado manualmente
                // Para detectar si fue editado manualmente, verificamos si la suma actual no coincide
                const montoElectronicoActual = parseNumero(montoCordobasElectronico.value);
                const montoFisicoActual = parseNumero(document.getElementById('MontoCordobasFisico')?.value);
                const sumaActual = montoFisicoActual + montoElectronicoActual;
                
                // Si la suma actual no coincide con el total, significa que el usuario editó manualmente
                // En ese caso, solo actualizar si el campo electrónico está vacío o muy cerca del cálculo
                if (montoElectronicoActual === 0 || Math.abs(sumaActual - montoTotal) > 0.01) {
                    montoCordobasElectronico.value = montoElectronico.toFixed(2);
                }
            }
        } else if (moneda === '$') {
            const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
            if (montoDolaresElectronico && montoElectronico > 0) {
                const montoElectronicoEnDolares = montoElectronico / tipoCambioValor;
                const montoElectronicoActual = parseNumero(montoDolaresElectronico.value);
                const montoFisicoActual = parseNumero(document.getElementById('MontoDolaresFisico')?.value);
                const sumaActual = (montoFisicoActual + montoElectronicoActual) * tipoCambioValor;
                
                if (montoElectronicoActual === 0 || Math.abs(sumaActual - montoTotal) > 0.01) {
                    montoDolaresElectronico.value = montoElectronicoEnDolares.toFixed(2);
                }
            }
        }
    },
    
    actualizarTipoCambio() {
        const moneda = document.getElementById('Moneda')?.value;
        const montoInput = document.getElementById('Monto');
        const tipoPago = document.querySelector('input[name="TipoPago"]:checked')?.value;
        
        if (!montoInput || !montoInput.value || parseFloat(montoInput.value) <= 0) return;
        
        const montoTotal = parseFloat(montoInput.value);
        const tipoCambioValor = PagoSystem.tipoCambio;
        const tipoPagoParaUsar = tipoPago || 'Fisico';
        
        // Cambiar TODOS los campos según la moneda seleccionada
        if (moneda === '$') {
            // Convertir todo a dólares
            const montoDolaresFisico = document.getElementById('MontoDolaresFisico');
            const montoCordobasFisico = document.getElementById('MontoCordobasFisico');
            const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
            const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
            
            // Físico
            if (montoDolaresFisico && (!montoDolaresFisico.value || parseFloat(montoDolaresFisico.value) === 0)) {
                montoDolaresFisico.value = (montoTotal / tipoCambioValor).toFixed(2);
            }
            if (montoCordobasFisico) {
                montoCordobasFisico.value = '';
            }
            
            // Electrónico
            if (montoDolaresElectronico && (!montoDolaresElectronico.value || parseFloat(montoDolaresElectronico.value) === 0)) {
                montoDolaresElectronico.value = (montoTotal / tipoCambioValor).toFixed(2);
            }
            if (montoCordobasElectronico) {
                montoCordobasElectronico.value = '';
            }
        } else if (moneda === 'C$') {
            // Convertir todo a córdobas
            const montoCordobasFisico = document.getElementById('MontoCordobasFisico');
            const montoDolaresFisico = document.getElementById('MontoDolaresFisico');
            const montoCordobasElectronico = document.getElementById('MontoCordobasElectronico');
            const montoDolaresElectronico = document.getElementById('MontoDolaresElectronico');
            
            // Físico
            if (montoCordobasFisico && (!montoCordobasFisico.value || parseFloat(montoCordobasFisico.value) === 0)) {
                montoCordobasFisico.value = montoTotal.toFixed(2);
            }
            if (montoDolaresFisico) {
                montoDolaresFisico.value = '';
            }
            
            // Electrónico
            if (montoCordobasElectronico && (!montoCordobasElectronico.value || parseFloat(montoCordobasElectronico.value) === 0)) {
                montoCordobasElectronico.value = montoTotal.toFixed(2);
            }
            if (montoDolaresElectronico) {
                montoDolaresElectronico.value = '';
            }
        }
        
        // Si es modo Mixto, recalcular el electrónico automáticamente
        if (tipoPago === 'Mixto') {
            this.calcularMontoElectronicoAutomatico();
        }
        
        PagoManager.actualizarEtiquetaMontoRecibido();
        
        if (!tipoPago && moneda) {
            const radioFisico = document.querySelector('input[name="TipoPago"][value="Fisico"]');
            if (radioFisico) {
                radioFisico.checked = true;
                this.mostrarCamposPago();
            }
        } else {
            PagoManager.calcularTotalPago();
        }
    }
};

// ============================================
// UTILIDADES
// ============================================
const Utilidades = {
    habilitarEdicionMonto() {
        const montoInput = document.getElementById('Monto');
        const btnEditar = document.getElementById('btnEditarMonto');
        
        if (montoInput.readOnly) {
            montoInput.readOnly = false;
            montoInput.focus();
            btnEditar.innerHTML = '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg>';
            btnEditar.title = 'Guardar (se llenará automáticamente)';
            document.getElementById('montoInfo').innerHTML = '<span class="text-amber-600 dark:text-amber-400 font-medium">⚠️ Edición manual habilitada. Usa punto (.) para decimales.</span>';
        } else {
            montoInput.readOnly = true;
            btnEditar.innerHTML = '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>';
            btnEditar.title = 'Hacer editable';
        }
    },
    
    validarMonto(input) {
        let valor = input.value.toString();
        if (valor.includes(',')) {
            valor = valor.replace(',', '.');
            input.value = valor;
        }
        
        valor = valor.replace(/[^\d.]/g, '');
        const partes = valor.split('.');
        if (partes.length > 2) {
            valor = partes[0] + '.' + partes.slice(1).join('');
        }
        
        if (partes.length === 2 && partes[1].length > 2) {
            valor = partes[0] + '.' + partes[1].substring(0, 2);
        }
        
        input.value = valor;
    },
    
    validarFormulario(e) {
        const clienteId = document.getElementById('clienteId').value;
        const montoInput = document.getElementById('Monto');
        const monto = parseFloat(montoInput.value) || 0;
        
        if (!clienteId) {
            e.preventDefault();
            const clienteSearch = document.getElementById('clienteSearch');
            clienteSearch.focus();
            clienteSearch.classList.add('border-red-500', 'ring-2', 'ring-red-500');
            setTimeout(() => {
                clienteSearch.classList.remove('border-red-500', 'ring-2', 'ring-red-500');
            }, 3000);
            alert('Debe seleccionar un cliente');
            return false;
        }
        
        const facturaId = document.getElementById('FacturaId')?.value;
        const facturaIds = document.getElementById('FacturaIds')?.value;
        
        if (!facturaId && !facturaIds) {
            e.preventDefault();
            alert('Debe seleccionar al menos una factura');
            return false;
        }
        
        const montoEsperado = parseFloat(montoInput.getAttribute('data-monto-esperado')) || 0;
        if (monto > montoEsperado * 2) {
            e.preventDefault();
            const mensaje = `⚠️ ERROR: El monto ingresado (C$ ${monto.toLocaleString('es-NI', {minimumFractionDigits: 2, maximumFractionDigits: 2})}) es demasiado alto.\n\n` +
                          `El monto esperado es: C$ ${montoEsperado.toLocaleString('es-NI', {minimumFractionDigits: 2, maximumFractionDigits: 2})}\n\n` +
                          `¿Estás seguro de que quieres continuar?`;
            
            if (!confirm(mensaje)) {
                montoInput.value = montoEsperado.toFixed(2).replace(',', '.');
                montoInput.readOnly = true;
                return false;
            }
        }
        
        montoInput.value = monto.toFixed(2).replace(',', '.');
    }
};

// ============================================
// EXPORTAR FUNCIONES GLOBALES
// ============================================
window.cargarFacturas = (clienteId) => FacturaManager.cargarFacturas(clienteId);
window.mostrarCamposPago = () => CamposPagoManager.mostrarCamposPago();
window.actualizarTipoCambio = () => CamposPagoManager.actualizarTipoCambio();
window.calcularTotalPago = () => PagoManager.calcularTotalPago();
window.calcularVueltoFisico = () => PagoManager.calcularVueltoFisico();
window.actualizarEtiquetaMontoRecibido = () => PagoManager.actualizarEtiquetaMontoRecibido();
window.actualizarMonto = () => PagoManager.actualizarMonto();
window.habilitarEdicionMonto = () => Utilidades.habilitarEdicionMonto();
window.validarMonto = (input) => Utilidades.validarMonto(input);

// ============================================
// INICIALIZACIÓN
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    // Inicializar sistema de pagos
    PagoSystem.init();
    
    // Validar formulario antes de enviar
    document.querySelector('#formPago')?.addEventListener('submit', Utilidades.validarFormulario);
});

