using System;
using System.Collections.Generic;
using System.Text;

namespace PoryectoClienteServidor.Models.DTOs
{
    public class UsuarioDTO
    {
        public string Nombre { get; set; } = null!;
        public int Turno { get; set; } = 0;
        public string Estado { get; set; } = "";
        public string Id { get; set; } = null!;
    }
}
