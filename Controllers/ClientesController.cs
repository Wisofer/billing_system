using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class ClientesController : Controller
{
    private readonly IClienteService _clienteService;
    private readonly IServicioService _servicioService;

    public ClientesController(IClienteService clienteService, IServicioService servicioService)
    {
        _clienteService = clienteService;
        _servicioService = servicioService;
    }

    [HttpGet("/clientes/obtener-codigo")]
    public IActionResult ObtenerCodigo()
    {
        var codigo = CodigoHelper.GenerarCodigoClienteUnico(c => _clienteService.ExisteCodigo(c));
        return Json(new { codigo });
    }

    [HttpGet("/clientes")]
    public IActionResult Index(string? busqueda, int pagina = 1, int tamanoPagina = 10)
    {
        // Validar parámetros de paginación
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 5) tamanoPagina = 5;
        if (tamanoPagina > 50) tamanoPagina = 50;

        var resultado = _clienteService.ObtenerPaginados(pagina, tamanoPagina, busqueda);
        var esAdministrador = SecurityHelper.IsAdministrator(User);
        
        // Estadísticas desde la base de datos (optimizado)
        var totalClientes = _clienteService.ObtenerTotal();
        var clientesActivos = _clienteService.ObtenerTotalActivos();
        var nuevosEsteMes = _clienteService.ObtenerNuevosEsteMes();

        ViewBag.Busqueda = busqueda;
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalItems = resultado.TotalItems;
        ViewBag.TotalPages = resultado.TotalPages;
        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.ClientesActivos = clientesActivos;
        ViewBag.NuevosEsteMes = nuevosEsteMes;
        ViewBag.Servicios = _servicioService.ObtenerActivos();

        return View(resultado.Items);
    }


    [Authorize(Policy = "Administrador")]
    [HttpPost("/clientes/crear")]
    public IActionResult Crear([FromForm] Cliente cliente)
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(cliente.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es requerido.");
        }

        // Validar código si se proporciona manualmente
        if (!string.IsNullOrWhiteSpace(cliente.Codigo) && _clienteService.ExisteCodigo(cliente.Codigo))
        {
            ModelState.AddModelError("Codigo", "El código ya existe.");
        }

        // Validar duplicados
        if (!string.IsNullOrWhiteSpace(cliente.Cedula) && _clienteService.ExisteCedula(cliente.Cedula))
        {
            ModelState.AddModelError("Cedula", "Ya existe un cliente con esta cédula.");
        }

        if (!string.IsNullOrWhiteSpace(cliente.Email) && _clienteService.ExisteEmail(cliente.Email))
        {
            ModelState.AddModelError("Email", "Ya existe un cliente con este email.");
        }

        if (!string.IsNullOrWhiteSpace(cliente.Nombre) && _clienteService.ExisteNombreYCedula(cliente.Nombre, cliente.Cedula))
        {
            ModelState.AddModelError("Nombre", "Ya existe un cliente con este nombre" + 
                (string.IsNullOrWhiteSpace(cliente.Cedula) ? "." : $" y cédula '{cliente.Cedula}'."));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return Json(new { success = false, errors });
        }

        try
        {
            var clienteCreado = _clienteService.Crear(cliente);
            
            // Asignar servicios si vienen en el request (desde FormData o JSON)
            List<int> servicioIds = new List<int>();
            
            // Intentar obtener desde FormData (múltiples valores con el mismo nombre)
            if (Request.Form.ContainsKey("ServicioIds"))
            {
                servicioIds = Request.Form["ServicioIds"]
                    .Where(id => int.TryParse(id.ToString(), out _))
                    .Select(id => int.Parse(id.ToString()))
                    .ToList();
                
                // Procesar todos los servicios (Internet y Streaming)
                var serviciosConCantidad = new Dictionary<int, int>();
                
                foreach (var servicioId in servicioIds)
                {
                    var servicio = _servicioService.ObtenerPorId(servicioId);
                    if (servicio == null) continue;
                    
                    // Si es Streaming, obtener cantidad del formulario o usar default
                    if (servicio.Categoria == SD.CategoriaStreaming)
                    {
                        var cantidadKey = $"Cantidad_{servicioId}";
                        int cantidad = 1;
                        
                        if (Request.Form.ContainsKey(cantidadKey))
                        {
                            if (int.TryParse(Request.Form[cantidadKey].ToString(), out int cant) && cant > 0)
                            {
                                cantidad = cant;
                            }
                        }
                        
                        serviciosConCantidad[servicioId] = cantidad;
                    }
                    else
                    {
                        // Para Internet, cantidad siempre es 1
                        serviciosConCantidad[servicioId] = 1;
                    }
                }
                
                // Usar AsignarServiciosConCantidad para todos los servicios
                if (serviciosConCantidad.Any())
                {
                    _clienteService.AsignarServiciosConCantidad(clienteCreado.Id, serviciosConCantidad);
                }
            }
            
            // Siempre devolver JSON para peticiones AJAX (verificar Accept header)
            var acceptHeader = Request.Headers["Accept"].ToString();
            if (acceptHeader.Contains("application/json"))
            {
                return Json(new { success = true, message = "Cliente creado exitosamente." });
            }
            // Si no, redirigir (fallback)
            TempData["Success"] = "Cliente creado exitosamente.";
            return Redirect("/clientes");
        }
        catch (Exception ex)
        {
            var acceptHeader = Request.Headers["Accept"].ToString();
            if (acceptHeader.Contains("application/json"))
            {
                return Json(new { success = false, message = $"Error al crear cliente: {ex.Message}" });
            }
            TempData["Error"] = $"Error al crear cliente: {ex.Message}";
            return Redirect("/clientes");
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpGet("/clientes/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var cliente = _clienteService.ObtenerPorId(id);
        if (cliente == null)
        {
            TempData["Error"] = "Cliente no encontrado.";
            return Redirect("/clientes");
        }

        ViewBag.Servicios = _servicioService.ObtenerActivos();
        return View(cliente);
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/clientes/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, [FromForm] Cliente cliente)
    {
        // Asegurar que el ID del cliente coincida con el de la ruta
        cliente.Id = id;

        // Validar que el cliente exista
        var clienteExistente = _clienteService.ObtenerPorId(id);
        if (clienteExistente == null)
        {
            TempData["Error"] = "Cliente no encontrado.";
            return Redirect("/clientes");
        }

        // Manejar el checkbox Activo
        cliente.Activo = FormHelper.GetCheckboxValue(Request.Form, "Activo");

        // Validaciones
        if (string.IsNullOrWhiteSpace(cliente.Codigo))
        {
            ModelState.AddModelError("Codigo", "El código es requerido.");
        }
        else if (_clienteService.ExisteCodigo(cliente.Codigo, id))
        {
            ModelState.AddModelError("Codigo", "El código ya existe.");
        }

        if (string.IsNullOrWhiteSpace(cliente.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es requerido.");
        }

        // Validar duplicados (excluyendo el cliente actual)
        if (!string.IsNullOrWhiteSpace(cliente.Cedula) && _clienteService.ExisteCedula(cliente.Cedula, id))
        {
            ModelState.AddModelError("Cedula", "Ya existe otro cliente con esta cédula.");
        }

        if (!string.IsNullOrWhiteSpace(cliente.Email) && _clienteService.ExisteEmail(cliente.Email, id))
        {
            ModelState.AddModelError("Email", "Ya existe otro cliente con este email.");
        }

        if (_clienteService.ExisteNombreYCedula(cliente.Nombre, cliente.Cedula, id))
        {
            ModelState.AddModelError("Nombre", "Ya existe otro cliente con este nombre" + 
                (string.IsNullOrWhiteSpace(cliente.Cedula) ? "." : $" y cédula '{cliente.Cedula}'."));
        }

        // Si FechaCreacion no viene del formulario o está vacía, mantener la existente
        if (cliente.FechaCreacion == default(DateTime))
        {
            cliente.FechaCreacion = clienteExistente.FechaCreacion;
        }

        if (!ModelState.IsValid)
        {
            // Si hay errores, mantener los datos del formulario pero asegurar que el cliente tenga todos los campos
            cliente.FechaCreacion = clienteExistente.FechaCreacion;
            ViewBag.Servicios = _servicioService.ObtenerActivos();
            return View(cliente);
        }

        try
        {
            _clienteService.Actualizar(cliente);
            
            // Asignar servicios si vienen en el request
            List<int> servicioIds = new List<int>();
            
            if (Request.Form.ContainsKey("ServicioIds"))
            {
                servicioIds = Request.Form["ServicioIds"]
                    .Where(id => int.TryParse(id.ToString(), out _))
                    .Select(id => int.Parse(id.ToString()))
                    .ToList();
                
                // Verificar si hay servicios de Streaming (que requieren cantidad)
                var serviciosConCantidad = new Dictionary<int, int>();
                bool tieneServiciosStreaming = false;
                
                foreach (var servicioId in servicioIds)
                {
                    var servicio = _servicioService.ObtenerPorId(servicioId);
                    if (servicio == null) continue;
                    
                    // Si es Streaming, obtener cantidad del formulario o usar default
                    if (servicio.Categoria == SD.CategoriaStreaming)
                    {
                        tieneServiciosStreaming = true;
                        var cantidadKey = $"Cantidad_{servicioId}";
                        int cantidad = 1;
                        
                        if (Request.Form.ContainsKey(cantidadKey))
                        {
                            if (int.TryParse(Request.Form[cantidadKey].ToString(), out int cant) && cant > 0)
                            {
                                cantidad = cant;
                            }
                        }
                        
                        serviciosConCantidad[servicioId] = cantidad;
                    }
                    else
                    {
                        // Para Internet, cantidad siempre es 1
                        serviciosConCantidad[servicioId] = 1;
                    }
                }
                
                // Usar AsignarServiciosConCantidad si hay servicios (todos se manejan igual ahora)
                if (serviciosConCantidad.Any())
                {
                    _clienteService.AsignarServiciosConCantidad(id, serviciosConCantidad);
                }
            }
            
            TempData["Success"] = "Cliente actualizado exitosamente.";
            return Redirect("/clientes");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar cliente: {ex.Message}";
            // Recargar el cliente original en caso de error
            cliente.FechaCreacion = clienteExistente.FechaCreacion;
            ViewBag.Servicios = _servicioService.ObtenerActivos();
            return View(cliente);
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/clientes/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            // Verificar que el cliente existe
            var cliente = _clienteService.ObtenerPorId(id);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return Redirect("/clientes");
            }

            // Intentar eliminar
            var eliminado = _clienteService.Eliminar(id);
            if (eliminado)
            {
                TempData["Success"] = $"Cliente '{cliente.Nombre}' eliminado exitosamente.";
            }
            else
            {
                TempData["Error"] = $"No se puede eliminar el cliente '{cliente.Nombre}' porque tiene facturas asociadas. Primero debe eliminar o cancelar las facturas relacionadas.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar cliente: {ex.Message}";
        }

        return Redirect("/clientes");
    }

    [Authorize(Policy = "Administrador")]
    [HttpGet("/clientes/exportar-excel")]
    public IActionResult ExportarExcel()
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Obtener todos los clientes con sus servicios
            var clientes = _clienteService.ObtenerTodos()
                .OrderBy(c => c.Nombre)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Clientes");

                // Encabezados
                worksheet.Cells[1, 1].Value = "Código";
                worksheet.Cells[1, 2].Value = "Nombre";
                worksheet.Cells[1, 3].Value = "Email";
                worksheet.Cells[1, 4].Value = "Teléfono";
                worksheet.Cells[1, 5].Value = "Cédula";
                worksheet.Cells[1, 6].Value = "Servicios";
                worksheet.Cells[1, 7].Value = "Total Facturas";
                worksheet.Cells[1, 8].Value = "Estado";
                worksheet.Cells[1, 9].Value = "Fecha Creación";

                // Formatear encabezados
                using (var range = worksheet.Cells[1, 1, 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(59, 130, 246)); // Azul
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // Datos
                int row = 2;
                foreach (var cliente in clientes)
                {
                    worksheet.Cells[row, 1].Value = cliente.Codigo;
                    worksheet.Cells[row, 2].Value = cliente.Nombre;
                    worksheet.Cells[row, 3].Value = cliente.Email ?? "";
                    worksheet.Cells[row, 4].Value = cliente.Telefono ?? "";
                    worksheet.Cells[row, 5].Value = cliente.Cedula ?? "";
                    
                    // Determinar servicios
                    var serviciosActivos = cliente.ClienteServicios?.Where(cs => cs.Activo).ToList() ?? new List<ClienteServicio>();
                    var tieneInternet = serviciosActivos.Any(cs => cs.Servicio?.Categoria == SD.CategoriaInternet);
                    var tieneStreaming = serviciosActivos.Any(cs => cs.Servicio?.Categoria == SD.CategoriaStreaming);
                    
                    string servicios = "";
                    if (tieneInternet && tieneStreaming)
                        servicios = "Ambos";
                    else if (tieneInternet)
                        servicios = "Internet";
                    else if (tieneStreaming)
                        servicios = "Streaming";
                    else
                        servicios = "-";
                    
                    worksheet.Cells[row, 6].Value = servicios;
                    worksheet.Cells[row, 7].Value = cliente.TotalFacturas;
                    worksheet.Cells[row, 8].Value = cliente.Activo ? "Activo" : "Inactivo";
                    worksheet.Cells[row, 9].Value = cliente.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
                    
                    row++;
                }

                // Ajustar ancho de columnas
                worksheet.Column(1).Width = 15; // Código
                worksheet.Column(2).Width = 30; // Nombre
                worksheet.Column(3).Width = 30; // Email
                worksheet.Column(4).Width = 15; // Teléfono
                worksheet.Column(5).Width = 15; // Cédula
                worksheet.Column(6).Width = 15; // Servicios
                worksheet.Column(7).Width = 15; // Total Facturas
                worksheet.Column(8).Width = 12; // Estado
                worksheet.Column(9).Width = 18; // Fecha Creación

                // Aplicar bordes a todas las celdas con datos
                if (row > 2)
                {
                    using (var range = worksheet.Cells[1, 1, row - 1, 9])
                    {
                        range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }
                }

                // Generar nombre del archivo con fecha
                var nombreArchivo = $"Clientes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(package.GetAsByteArray(), contentType, nombreArchivo);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al exportar clientes a Excel: {ex.Message}";
            return Redirect("/clientes");
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/clientes/importar")]
    [ValidateAntiForgeryToken]
    public IActionResult Importar(IFormFile archivoExcel)
    {
        var rolUsuario = SecurityHelper.GetUserRole(User);
        if (rolUsuario != SD.RolAdministrador)
        {
            TempData["Error"] = "No tienes permisos para importar clientes.";
            return Redirect("/clientes");
        }

        if (archivoExcel == null || archivoExcel.Length == 0)
        {
            TempData["Error"] = "Por favor selecciona un archivo Excel.";
            return Redirect("/clientes");
        }

        var extension = Path.GetExtension(archivoExcel.FileName).ToLower();
        if (extension != ".xlsx" && extension != ".xls")
        {
            TempData["Error"] = "El archivo debe ser un Excel (.xlsx o .xls).";
            return Redirect("/clientes");
        }

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var clientesImportados = 0;
            var errores = new List<string>();

            using (var stream = new MemoryStream())
            {
                archivoExcel.CopyTo(stream);
                stream.Position = 0;

                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                    {
                        TempData["Error"] = "El archivo Excel está vacío o no tiene datos.";
                        return Redirect("/clientes");
                    }

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var nombre = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "";
                            var email = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "";
                            var telefono = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
                            var cedula = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "";

                            if (string.IsNullOrWhiteSpace(nombre))
                            {
                                errores.Add($"Fila {row}: El nombre es requerido.");
                                continue;
                            }

                            // Validar duplicados antes de crear
                            if (_clienteService.ExisteNombreYCedula(nombre, cedula))
                            {
                                errores.Add($"Fila {row}: Ya existe un cliente con el nombre '{nombre}'" + 
                                          (string.IsNullOrWhiteSpace(cedula) ? "" : $" y cédula '{cedula}'") + ". Se omite.");
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(cedula) && _clienteService.ExisteCedula(cedula))
                            {
                                errores.Add($"Fila {row}: Ya existe un cliente con la cédula '{cedula}'. Se omite.");
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(email) && _clienteService.ExisteEmail(email))
                            {
                                errores.Add($"Fila {row}: Ya existe un cliente con el email '{email}'. Se omite.");
                                continue;
                            }

                            // Generar código único
                            var codigo = CodigoHelper.GenerarCodigoClienteUnico(c => _clienteService.ExisteCodigo(c));

                            var cliente = new Cliente
                            {
                                Codigo = codigo,
                                Nombre = nombre,
                                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                                Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono,
                                Cedula = string.IsNullOrWhiteSpace(cedula) ? null : cedula
                            };

                            _clienteService.Crear(cliente);
                            clientesImportados++;
                        }
                        catch (Exception ex)
                        {
                            errores.Add($"Fila {row}: Error al procesar - {ex.Message}");
                        }
                    }
                }
            }

            if (clientesImportados > 0)
            {
                TempData["Success"] = $"Se importaron exitosamente {clientesImportados} cliente(s).";
            }

            if (errores.Any())
            {
                TempData["Warning"] = $"Se importaron {clientesImportados} cliente(s), pero hubo {errores.Count} error(es).";
            }

            return Redirect("/clientes");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al procesar el archivo: {ex.Message}";
            return Redirect("/clientes");
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/clientes/actualizar-codigos")]
    [ValidateAntiForgeryToken]
    public IActionResult ActualizarCodigos()
    {
        try
        {
            var actualizados = _clienteService.ActualizarCodigosExistentes();
            TempData["Success"] = $"Se actualizaron {actualizados} código(s) de cliente(s) al nuevo formato EMS_XXXXXX.";
            return Redirect("/clientes");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar códigos: {ex.Message}";
            return Redirect("/clientes");
        }
    }
}
