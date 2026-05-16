using System;
using System.Collections.Generic;
using System.Text;

namespace PoryectoClienteServidor.Models.DTOs
{
    internal class RegistroDTO
    {
        public string Nombre { get; set; } = null!;
        public int Turno { get; set; } = 0;
    }
}
