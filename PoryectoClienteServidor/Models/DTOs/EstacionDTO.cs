using System;
using System.Collections.Generic;
using System.Text;

namespace PoryectoClienteServidor.Models.DTOs
{
    public class EstacionDTO
    {
        public string? Uid { get; set; }
        public int PosicionDeEstacionamiento { get; set; }
        public string? Accion { get; set; } 
    }
}
