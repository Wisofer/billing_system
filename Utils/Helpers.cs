using System.Text.Json;
using billing_system.Models.Entities;

namespace billing_system.Utils;

public static class Helpers
{
    public static Usuario? ObtenerUsuarioActual(ISession session)
    {
        var usuarioJson = session.GetString("UsuarioActual");
        if (string.IsNullOrEmpty(usuarioJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Usuario>(usuarioJson);
    }

    public static bool EsAdministrador(ISession session)
    {
        var rol = session.GetString("RolUsuario");
        return rol == SD.RolAdministrador;
    }

    public static bool EsUsuarioNormal(ISession session)
    {
        var rol = session.GetString("RolUsuario");
        return rol == SD.RolNormal;
    }
}

