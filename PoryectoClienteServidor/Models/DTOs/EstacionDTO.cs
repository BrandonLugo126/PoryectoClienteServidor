using System;
using System.Collections.Generic;
using System.Text;

namespace PoryectoClienteServidor.Models.DTOs
{
    public class EstacionDTO
    {
        public string Uid { get; set; } = null!;
        public int PosicionDeEstacionamiento { get; set; }
    }

    public class ApartarRequestDTO
    {
        public string Uid { get; set; } = null!;
        public int PosicionDeEstacionamiento { get; set; }
    }

    public class EstadoGeneralDTO
    {
        public int Libres { get; set; }
        public int Ocupados { get; set; }
        public string[] Lugares { get; set; } = new string[10]; // "libre", "ocupado", "tuyo"
    }


}
