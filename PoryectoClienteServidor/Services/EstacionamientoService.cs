using PoryectoClienteServidor.Models.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace PoryectoClienteServidor.Services
{
    public class EstacionamientoService
    {
        private HttpListener servidor;
        public int[] LugaresDeEstacionamiento { get; set; } = new int[10];
        public int LugaresLibres { get; set; } = 10;
        public EstacionDTO? Estacionar { get; set; }
        public event Action? TableroActualizado;
        public bool Activo;
        public EstacionamientoService()
        {
            servidor = new HttpListener();
            servidor.Prefixes.Add("http://localhost:5100/Estacionamiento/");
            Iniciar();
        }
        public void Iniciar()
        {
            servidor.Start();
            Activo = true;

            Thread Hilo = new Thread(EscucharPeticiones) { IsBackground = true };
            Hilo.Start();

        }

        private void EscucharPeticiones()
        {
            while (Activo)
            {
                try
                {
                    var context = servidor.GetContext();

                    Thread hiloPeticion = new Thread(() => ProcesarPeticion(context))
                    {
                        IsBackground = true,
                    };
                    hiloPeticion.Start();

                }
                catch (HttpListenerException)
                {

                    throw new HttpListenerException();
                }

            }
        }

        private void ProcesarPeticion(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (request.HttpMethod == "GET" && request.RawUrl == "/Estacionamiento/")
                {
                    ServirArchivo(response, "index.html", "text/html");
                }
                else if (request.HttpMethod == "POST" && request.RawUrl == "/Estacionamiento/Apartar")
                {
                    byte[] buffer = new byte[request.ContentLength64];
                    request.InputStream.ReadExactly(buffer, 0, buffer.Length);
                    var json = Encoding.UTF8.GetString(buffer);
                    var usuario = JsonSerializer.Deserialize<EstacionDTO>(json);

                    if (usuario != null)
                    {
                        if (usuario.PosicionDeEstacionamiento > 10 || usuario.PosicionDeEstacionamiento < 1)
                        {
                            response.StatusCode = 400;
                            response.Close();
                        }
                        else
                        {
                            while (LugaresLibres == 0 || LugaresDeEstacionamiento[usuario.PosicionDeEstacionamiento - 1] == 1)
                            {
                                Thread.Sleep(500);
                            }
                            ApartarLugar(usuario);
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.Close();
                    }
                }
                else if (request.HttpMethod == "POST" && request.RawUrl == "/Estacionamiento/Desocupar")
                {
                   byte[] buffer = new byte[request.ContentLength64];
                    request.InputStream.ReadExactly(buffer, 0, buffer.Length);
                    var json = Encoding.UTF8.GetString(buffer);
                    var usuario = JsonSerializer.Deserialize<EstacionDTO>(json);
                    if (usuario != null)
                    {
                        if (usuario.PosicionDeEstacionamiento > 10 || usuario.PosicionDeEstacionamiento < 1)
                        {
                            response.StatusCode = 400;
                            response.Close();
                        }
                        else
                        {
                           
                          QuitarLugar(usuario);
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.Close();
                    }
                }

                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        public void ApartarLugar(EstacionDTO estacionDTO)
        {
            if (LugaresLibres > 0 && LugaresDeEstacionamiento[estacionDTO.PosicionDeEstacionamiento - 1] == 0)
            {
                LugaresDeEstacionamiento[estacionDTO.PosicionDeEstacionamiento - 1] = 1;
                LugaresLibres--;
                Estacionar = estacionDTO;
                TableroActualizado?.Invoke();
            }
        }
        public void QuitarLugar(EstacionDTO estacionDTO)
        {
            if (LugaresDeEstacionamiento[estacionDTO.PosicionDeEstacionamiento - 1] == 1 && estacionDTO.Uid == Estacionar?.Uid)
            {
                LugaresDeEstacionamiento[estacionDTO.PosicionDeEstacionamiento - 1] = 0;
                LugaresLibres++;
                TableroActualizado?.Invoke();
            }
        }


        private void ServirArchivo(HttpListenerResponse response, string nombreArchivo, string contentType)
        {
            string ruta = Path.Combine("Assets", nombreArchivo);

            if (File.Exists(ruta))
            {
                byte[] buffer = File.ReadAllBytes(ruta);
                response.ContentLength64 = buffer.Length;
                response.ContentType = contentType;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.StatusCode = 200;
            }
            else
            {
                response.StatusCode = 404;
            }
        }
    }
}
