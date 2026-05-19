using System;
using System.Collections.Generic;
using System.Text;

namespace PoryectoClienteServidor.Models.DTOs
{
    public class Tablero
    {
        public int Turno { get; set; }
        public string TurnoNombre { get; set; } = null!;

        public string EstadoTurno { get; set; } = null!;
    }
}
