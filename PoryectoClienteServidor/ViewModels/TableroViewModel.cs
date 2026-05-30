using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoryectoClienteServidor.Models;
using PoryectoClienteServidor.Models.DTOs;
using PoryectoClienteServidor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace PoryectoClienteServidor.ViewModels
{
    public partial class TableroViewModel : ObservableObject
    {
        public readonly EstacionamientoService service;
        public ObservableCollection<Estacionamiento> Estacionamientos { get; set; } = new();

        [ObservableProperty]
        int lugaresOcupados=0;
        [ObservableProperty]
        int lugaresDesocupados=10;
        public TableroViewModel(EstacionamientoService service)
        {

            this.service = service;
            service.LugarApartado += Service_LugarApartado;
            service.LugarDesocupado += Service_LugarDesocupado;

            for (int i = 1; i <= 10; i++)
            {
               var estacion = new Estacionamiento() { 
                   Numero = i,
                   Estado = "libre"
               };
                Estacionamientos.Add(estacion);
            }       
        }             

        private void Service_LugarDesocupado(int num)
        {
            Estacionamientos[num - 1].Estado = "libre";
            LugaresOcupados--;
            LugaresDesocupados++;
        }

        private void Service_LugarApartado(int num)
        {            
            Estacionamientos[num-1].Estado = "ocupado";
            LugaresOcupados++;
            LugaresDesocupados--; 
        }

        public void Iniciar()
        {
            service.Iniciar();
        }
        //Colores:
        //#5A2127 Ocupado
        //#222A37 Libre

    }
}
