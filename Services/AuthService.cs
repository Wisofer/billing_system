using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Services;

public class AuthService : IAuthService
{
    public Usuario? ValidarUsuario(string nombreUsuario, string contrasena)
    {
        var usuarios = SD.UsuariosEstaticos.ObtenerUsuarios();
        
        var usuario = usuarios.FirstOrDefault(u => 
            u.NombreUsuario.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase) &&
            u.Contrasena == contrasena &&
            u.Activo);

        return usuario;
    }

    public bool EsAdministrador(Usuario usuario)
    {
        return usuario.Rol == SD.RolAdministrador;
    }

    public bool EsUsuarioNormal(Usuario usuario)
    {
        return usuario.Rol == SD.RolNormal;
    }
}

