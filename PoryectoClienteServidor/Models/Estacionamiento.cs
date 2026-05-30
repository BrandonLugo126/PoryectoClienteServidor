using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace PoryectoClienteServidor.Models
{
    public partial class Estacionamiento:ObservableObject
    {
        [ObservableProperty]
        private string estado ="libre";

        public int Numero { get; set; }
        
    }
}
