using billing_system.Models.Entities;
using billing_system.Models.ViewModels;

namespace billing_system.Services.IServices;

public interface IEgresoService
{
    List<Egreso> ObtenerTodos();
    List<Egreso> ObtenerActivos();
    Egreso? ObtenerPorId(int id);
    Egreso? ObtenerPorCodigo(string codigo);
    PagedResult<Egreso> ObtenerPaginados(int pagina, int tamanoPagina, string? busqueda = null, string? categoria = null, DateTime? fechaInicio = null, DateTime? fechaFin = null);
    void Crear(Egreso egreso);
    void Actualizar(Egreso egreso);
    void Eliminar(int id);
    string GenerarCodigo();
    decimal CalcularTotalEgresos();
    decimal CalcularTotalEgresosPorPeriodo(DateTime fechaInicio, DateTime fechaFin);
    decimal CalcularTotalEgresosMesActual();
    Dictionary<string, decimal> ObtenerEgresosPorCategoria();
    Dictionary<string, decimal> ObtenerEgresosPorCategoriaPeriodo(DateTime fechaInicio, DateTime fechaFin);
    List<Egreso> ObtenerUltimosEgresos(int cantidad);
}

