using billing_system.Models.Entities;
using billing_system.Models.ViewModels;

namespace billing_system.Services.IServices;

public interface IEquipoService
{
    List<Equipo> ObtenerTodos();
    PagedResult<Equipo> ObtenerPaginados(int pagina = 1, int tamanoPagina = 10, string? busqueda = null, string? estado = null, int? categoriaId = null, int? ubicacionId = null);
    Equipo? ObtenerPorId(int id);
    Equipo? ObtenerPorCodigo(string codigo);
    Equipo? ObtenerPorNumeroSerie(string numeroSerie);
    List<Equipo> Buscar(string termino);
    Equipo Crear(Equipo equipo);
    Equipo Actualizar(Equipo equipo);
    bool Eliminar(int id);
    bool CambiarEstado(int equipoId, string nuevoEstado, int usuarioId, string? motivo = null);
    bool ExisteCodigo(string codigo, int? idExcluir = null);
    bool ExisteNumeroSerie(string numeroSerie, int? idExcluir = null);
    int ObtenerTotal();
    int ObtenerTotalPorEstado(string estado);
    List<Equipo> ObtenerConStockMinimo(); // Alertas de stock bajo
    decimal ObtenerValorTotalInventario();
}

