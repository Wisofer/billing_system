/**
 * Modal Dialog System - Reemplaza alert() y confirm() nativos
 * Uso:
 *   ModalDialog.alert('Mensaje') - Reemplaza alert()
 *   ModalDialog.confirm('Mensaje', () => {}, () => {}) - Reemplaza confirm()
 */

const ModalDialog = {
    modal: null,
    overlay: null,
    header: null,
    icon: null,
    title: null,
    message: null,
    footer: null,

    /**
     * Inicializa el modal (debe llamarse después de que el DOM esté listo)
     */
    init: function() {
        this.modal = document.getElementById('modalDialog');
        this.overlay = document.getElementById('modalDialogOverlay');
        this.header = document.getElementById('modalDialogHeader');
        this.icon = document.getElementById('modalDialogIcon');
        this.title = document.getElementById('modalDialogTitle');
        this.message = document.getElementById('modalDialogMessage');
        this.footer = document.getElementById('modalDialogFooter');

        // Cerrar al hacer clic en el overlay
        if (this.overlay) {
            this.overlay.addEventListener('click', () => {
                this.hide();
            });
        }

        // Cerrar con ESC
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.modal && !this.modal.classList.contains('hidden')) {
                this.hide();
            }
        });
    },

    /**
     * Muestra el modal
     */
    show: function() {
        if (!this.modal) {
            console.error('ModalDialog no está inicializado. Llama a ModalDialog.init() primero.');
            return;
        }
        this.modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden'; // Prevenir scroll del body
    },

    /**
     * Oculta el modal
     */
    hide: function() {
        if (this.modal) {
            this.modal.classList.add('hidden');
            document.body.style.overflow = ''; // Restaurar scroll del body
        }
    },

    /**
     * Configura el icono según el tipo
     */
    setIcon: function(type) {
        if (!this.icon) return;

        let iconHtml = '';
        let iconColor = '';

        switch(type) {
            case 'info':
                iconColor = 'text-blue-500 dark:text-blue-400';
                iconHtml = `
                    <svg class="h-6 w-6 ${iconColor}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                `;
                break;
            case 'success':
                iconColor = 'text-green-500 dark:text-green-400';
                iconHtml = `
                    <svg class="h-6 w-6 ${iconColor}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                `;
                break;
            case 'warning':
                iconColor = 'text-yellow-500 dark:text-yellow-400';
                iconHtml = `
                    <svg class="h-6 w-6 ${iconColor}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                    </svg>
                `;
                break;
            case 'error':
            case 'danger':
                iconColor = 'text-red-500 dark:text-red-400';
                iconHtml = `
                    <svg class="h-6 w-6 ${iconColor}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                `;
                break;
            case 'question':
                iconColor = 'text-blue-500 dark:text-blue-400';
                iconHtml = `
                    <svg class="h-6 w-6 ${iconColor}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                `;
                break;
            default:
                iconColor = 'text-gray-500 dark:text-gray-400';
                iconHtml = `
                    <svg class="h-6 w-6 ${iconColor}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                `;
        }

        this.icon.innerHTML = iconHtml;
    },

    /**
     * Configura el header según el tipo
     */
    setHeader: function(type, customTitle = null) {
        if (!this.header || !this.title) return;

        let title = customTitle || 'Información';
        let bgColor = '';

        switch(type) {
            case 'info':
                bgColor = 'bg-blue-50 dark:bg-blue-900/20';
                title = customTitle || 'Información';
                break;
            case 'success':
                bgColor = 'bg-green-50 dark:bg-green-900/20';
                title = customTitle || 'Éxito';
                break;
            case 'warning':
                bgColor = 'bg-yellow-50 dark:bg-yellow-900/20';
                title = customTitle || 'Advertencia';
                break;
            case 'error':
            case 'danger':
                bgColor = 'bg-red-50 dark:bg-red-900/20';
                title = customTitle || 'Error';
                break;
            case 'question':
                bgColor = 'bg-blue-50 dark:bg-blue-900/20';
                title = customTitle || 'Confirmar';
                break;
            default:
                bgColor = 'bg-gray-50 dark:bg-gray-700';
        }

        this.header.className = `px-4 py-3 sm:px-6 border-b border-gray-200 dark:border-gray-700 ${bgColor}`;
        this.title.textContent = title;
    },

    /**
     * Muestra un alert (reemplaza alert())
     * @param {string} message - Mensaje a mostrar
     * @param {string} type - Tipo: 'info', 'success', 'warning', 'error' (default: 'info')
     * @param {string} title - Título personalizado (opcional)
     */
    alert: function(message, type = 'info', title = null) {
        return new Promise((resolve) => {
            if (!this.modal) {
                console.error('ModalDialog no está inicializado. Llama a ModalDialog.init() primero.');
                resolve();
                return;
            }

            this.setIcon(type);
            this.setHeader(type, title);
            this.message.textContent = message || '';

            // Botón único (Aceptar)
            this.footer.innerHTML = `
                <button type="button" 
                        id="modalDialogOkBtn"
                        class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm dark:bg-blue-500 dark:hover:bg-blue-600">
                    Aceptar
                </button>
            `;

            // Event listener para el botón
            const okBtn = document.getElementById('modalDialogOkBtn');
            if (okBtn) {
                const handleClick = () => {
                    okBtn.removeEventListener('click', handleClick);
                    this.hide();
                    resolve();
                };
                okBtn.addEventListener('click', handleClick);
            }

            this.show();
        });
    },

    /**
     * Muestra un confirm (reemplaza confirm())
     * @param {string} message - Mensaje a mostrar
     * @param {Function} onConfirm - Callback cuando se confirma (opcional)
     * @param {Function} onCancel - Callback cuando se cancela (opcional)
     * @param {string} title - Título personalizado (opcional)
     */
    confirm: function(message, onConfirm = null, onCancel = null, title = null) {
        return new Promise((resolve) => {
            if (!this.modal) {
                console.error('ModalDialog no está inicializado. Llama a ModalDialog.init() primero.');
                resolve(false);
                return;
            }

            this.setIcon('question');
            this.setHeader('question', title);
            this.message.textContent = message || '';

            // Dos botones (Cancelar y Confirmar)
            this.footer.innerHTML = `
                <button type="button" 
                        id="modalDialogCancelBtn"
                        class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-600 shadow-sm px-4 py-2 bg-white dark:bg-gray-700 text-base font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm">
                    Cancelar
                </button>
                <button type="button" 
                        id="modalDialogConfirmBtn"
                        class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-red-600 text-base font-medium text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 sm:ml-3 sm:w-auto sm:text-sm dark:bg-red-500 dark:hover:bg-red-600">
                    Confirmar
                </button>
            `;

            // Event listeners
            const cancelBtn = document.getElementById('modalDialogCancelBtn');
            const confirmBtn = document.getElementById('modalDialogConfirmBtn');

            const handleCancel = () => {
                cancelBtn?.removeEventListener('click', handleCancel);
                confirmBtn?.removeEventListener('click', handleConfirm);
                this.hide();
                if (onCancel) onCancel();
                resolve(false);
            };

            const handleConfirm = () => {
                cancelBtn?.removeEventListener('click', handleCancel);
                confirmBtn?.removeEventListener('click', handleConfirm);
                this.hide();
                if (onConfirm) onConfirm();
                resolve(true);
            };

            cancelBtn?.addEventListener('click', handleCancel);
            confirmBtn?.addEventListener('click', handleConfirm);

            this.show();
        });
    }
};

// Inicializar cuando el DOM esté listo
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => ModalDialog.init());
} else {
    ModalDialog.init();
}
