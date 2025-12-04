using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface ICategoriaEquipoService
{
    List<CategoriaEquipo> ObtenerTodas();
    List<CategoriaEquipo> ObtenerActivas();
    CategoriaEquipo? ObtenerPorId(int id);
    CategoriaEquipo Crear(CategoriaEquipo categoria);
    CategoriaEquipo Actualizar(CategoriaEquipo categoria);
    bool Eliminar(int id);
    bool ExisteNombre(string nombre, int? idExcluir = null);
}

