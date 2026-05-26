using PoryectoClienteServidor.Models.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace PoryectoClienteServidor.Services
{
    public class EstacionamientoService
    {
        private HttpListener servidor;
        private readonly object bloqueo = new object();
        public List<EstacionDTO> LugaresDeEstacionamiento { get; set; } = new List<EstacionDTO>();
        public int LugaresLibres { get; set; } = 10;
        public event Action<int>? TableroActualizado;
        public bool Activo { get; private set; }

        public EstacionamientoService()
        {
            servidor = new HttpListener();
            servidor.Prefixes.Add("http://localhost:5100/Estacionamiento/");
            Iniciar();
        }

        public void Iniciar()
        {
            Deserializar();
            servidor.Start();
            Activo = true;

            Thread hilo = new Thread(EscucharPeticiones) { IsBackground = true };
            hilo.Start();
        }

        public void Detener()
        {
            Activo = false;
            servidor.Stop();
        }

        private void EscucharPeticiones()
        {
            while (Activo)
            {
                try
                {
                    var context = servidor.GetContext();
                    Thread hiloPeticion = new Thread(() => ProcesarPeticion(context)) { IsBackground = true };
                    hiloPeticion.Start();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
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

                    if (usuario == null || usuario.PosicionDeEstacionamiento < 1 || usuario.PosicionDeEstacionamiento > 10)
                    {
                        response.StatusCode = 400;
                        response.Close();
                        return;
                    }


                    while (true)
                    {
                        bool libre;
                        lock (bloqueo)
                        {
                            libre = LugaresLibres > 0 && !LugaresDeEstacionamiento.Any(l => l.PosicionDeEstacionamiento == usuario.PosicionDeEstacionamiento);
                        }
                        if (libre) break;
                        Thread.Sleep(500);
                    }


                    lock (bloqueo)
                    {
                        if (LugaresLibres > 0 && !LugaresDeEstacionamiento.Any(l => l.PosicionDeEstacionamiento == usuario.PosicionDeEstacionamiento))
                        {
                            LugaresDeEstacionamiento.Add(usuario);
                            LugaresLibres--;
                            TableroActualizado?.Invoke(usuario.PosicionDeEstacionamiento);
                        }
                    }

                    response.StatusCode = 200;
                    response.Close();
                }
                else if (request.HttpMethod == "POST" && request.RawUrl == "/Estacionamiento/Desocupar")
                {
                    byte[] buffer = new byte[request.ContentLength64];
                    request.InputStream.ReadExactly(buffer, 0, buffer.Length);
                    var json = Encoding.UTF8.GetString(buffer);
                    var usuario = JsonSerializer.Deserialize<EstacionDTO>(json);

                    if (usuario == null || usuario.PosicionDeEstacionamiento < 1 || usuario.PosicionDeEstacionamiento > 10)
                    {
                        response.StatusCode = 400;
                        response.Close();
                        return;
                    }

                    lock (bloqueo)
                    {
                        var lugar = LugaresDeEstacionamiento.FirstOrDefault(l => l.PosicionDeEstacionamiento == usuario.PosicionDeEstacionamiento);
                        if (lugar != null && lugar.Uid == usuario.Uid)
                        {
                            LugaresDeEstacionamiento.Remove(lugar);
                            LugaresLibres++;
                            TableroActualizado?.Invoke(usuario.PosicionDeEstacionamiento);
                            response.StatusCode = 200;
                        }
                        else
                        {
                            response.StatusCode = 404;
                        }
                    }
                    response.Close();
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando petición: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        public void Serializar()
        {
            string json = JsonSerializer.Serialize(LugaresDeEstacionamiento);
            File.WriteAllText("estacionamiento.json", json);
        }
        public void Deserializar()
        {
            if (File.Exists("estacionamiento.json"))
            {
                string json = File.ReadAllText("estacionamiento.json");
                var lugares = JsonSerializer.Deserialize<List<EstacionDTO>>(json);
                if (lugares != null)
                {
                    LugaresDeEstacionamiento = lugares;
                    LugaresLibres = 10 - LugaresDeEstacionamiento.Count;
                }
            }
        }

        private void ServirArchivo(HttpListenerResponse response, string nombreArchivo, string contentType)
        {
            string ruta = Path.Combine("Assets", nombreArchivo);
            try
            {
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
            finally
            {
                response.Close();
            }
        }
    }
}