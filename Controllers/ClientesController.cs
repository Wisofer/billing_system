using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers;

[Route("[controller]/[action]")]
public class ClientesController : Controller
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet("/clientes/obtener-codigo")]
    public IActionResult ObtenerCodigo()
    {
        var numeroCliente = InMemoryStorage.Clientes.Count + 1;
        var codigo = $"CLI-{numeroCliente:D3}";
        
        // Asegurar que el código sea único
        while (_clienteService.ExisteCodigo(codigo))
        {
            numeroCliente++;
            codigo = $"CLI-{numeroCliente:D3}";
        }
        
        return Json(new { codigo });
    }

    [HttpGet("/clientes")]
    public IActionResult Index(string? busqueda)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        var clientes = string.IsNullOrWhiteSpace(busqueda)
            ? _clienteService.ObtenerTodos()
            : _clienteService.Buscar(busqueda);

        ViewBag.Busqueda = busqueda;
        return View(clientes);
    }


    [HttpPost("/clientes/crear")]
    public IActionResult Crear([FromBody] Cliente cliente)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Json(new { success = false, message = "No autenticado" });
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            return Json(new { success = false, message = "No tienes permisos para crear clientes." });
        }

        // El código se genera automáticamente si no viene o está vacío
        if (string.IsNullOrWhiteSpace(cliente.Codigo))
        {
            // Se generará en el servicio
            cliente.Codigo = string.Empty;
        }
        else if (_clienteService.ExisteCodigo(cliente.Codigo))
        {
            // Si viene un código, validar que no exista
            ModelState.AddModelError("Codigo", "El código ya existe.");
        }

        if (string.IsNullOrWhiteSpace(cliente.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es requerido.");
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
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

    [HttpGet("/clientes/editar/{id}")]
    public IActionResult Editar(int id)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para editar clientes.";
            return Redirect("/clientes");
        }

        var cliente = _clienteService.ObtenerPorId(id);
        if (cliente == null)
        {
            TempData["Error"] = "Cliente no encontrado.";
            return Redirect("/clientes");
        }

        return View(cliente);
    }

    [HttpPost("/clientes/editar/{id}")]
    public IActionResult Editar(int id, Cliente cliente)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para editar clientes.";
            return Redirect("/clientes");
        }

        if (id != cliente.Id)
        {
            TempData["Error"] = "Error en la solicitud.";
            return Redirect("/clientes");
        }

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

        if (!ModelState.IsValid)
        {
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
            return View(cliente);
        }
    }

    [HttpPost("/clientes/eliminar/{id}")]
    public IActionResult Eliminar(int id)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para eliminar clientes.";
            return Redirect("/clientes");
        }

        try
        {
            var eliminado = _clienteService.Eliminar(id);
            if (eliminado)
            {
                TempData["Success"] = "Cliente eliminado exitosamente.";
            }
            else
            {
                TempData["Error"] = "No se puede eliminar el cliente porque tiene facturas asociadas.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar cliente: {ex.Message}";
        }

        return Redirect("/clientes");
    }

    [HttpPost("/clientes/importar")]
    public IActionResult Importar(IFormFile archivoExcel)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        var rolUsuario = HttpContext.Session.GetString("RolUsuario") ?? "";
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
                            var nombre = worksheet.Cells[row, 1]?.Text?.Trim() ?? "";
                            var email = worksheet.Cells[row, 2]?.Text?.Trim() ?? "";
                            var telefono = worksheet.Cells[row, 3]?.Text?.Trim() ?? "";
                            var cedula = worksheet.Cells[row, 4]?.Text?.Trim() ?? "";

                            if (string.IsNullOrWhiteSpace(nombre))
                            {
                                errores.Add($"Fila {row}: El nombre es requerido.");
                                continue;
                            }

                            // Generar código único
                            var codigo = $"CLI{DateTime.Now:yyyyMMddHHmmss}{row}";
                            while (_clienteService.ExisteCodigo(codigo))
                            {
                                codigo = $"CLI{DateTime.Now:yyyyMMddHHmmss}{row}{new Random().Next(1000)}";
                            }

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
}
