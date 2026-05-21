using CommunityToolkit.Mvvm.Input;
using PoryectoClienteServidor.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace PoryectoClienteServidor.ViewModels
{
    public class TableroViewModel
    { 
        public ICommand InciarCommand {  get; set; }
        public readonly EstacionamientoService service;
        public TableroViewModel(EstacionamientoService service) { 
        
            InciarCommand = new RelayCommand(Iniciar);
            this.service = service;
            
        }
        public void Iniciar()
        {
            service.Iniciar();
        }
    }
}
