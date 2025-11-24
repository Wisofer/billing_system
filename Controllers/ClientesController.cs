using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers;

[Authorize]
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
    public IActionResult Crear([FromBody] Cliente cliente)
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
            _clienteService.Crear(cliente);
            // Siempre devolver JSON para peticiones AJAX
            var contentType = Request.Headers["Content-Type"].ToString();
            if (contentType.Contains("application/json") || Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Json(new { success = true, message = "Cliente creado exitosamente." });
            }
            // Si no, redirigir (fallback)
            TempData["Success"] = "Cliente creado exitosamente.";
            return Redirect("/clientes");
        }
        catch (Exception ex)
        {
            var contentType = Request.Headers["Content-Type"].ToString();
            if (contentType.Contains("application/json") || Request.Headers["Accept"].ToString().Contains("application/json"))
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
