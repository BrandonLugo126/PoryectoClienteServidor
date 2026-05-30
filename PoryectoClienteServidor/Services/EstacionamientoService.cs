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
        private int contadorCambios = 0;

        public List<EstacionDTO> LugaresOcupados { get; set; } = new List<EstacionDTO>();

        public bool Activo { get; private set; }

        public event Action<int>? LugarApartado, LugarDesocupado;

        public EstacionamientoService()
        {
            servidor = new HttpListener();
            servidor.Prefixes.Add("http://localhost:5100/Estacionamiento/");
            Iniciar();
        }

        public void Iniciar()
        {
            try
            {
                servidor.Start();
                Activo = true;

                Thread hilo = new Thread(EscucharPeticiones)
                {
                    IsBackground = true
                };
                hilo.Start();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
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
                    Thread hiloPeticion = new Thread(() => ProcesarPeticion(context))
                    {
                        IsBackground = true
                    };
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
                string rawUrl = request.RawUrl ?? "";
                string absolutePath = request.Url?.AbsolutePath ?? "";

                if (request.HttpMethod == "GET" && rawUrl == "/Estacionamiento/")
                {
                    ServirArchivo(response, "index.html", "text/html");
                }
                else if (request.HttpMethod == "GET" && absolutePath == "/Estacionamiento/Estado")
                {
                    string uid = request.QueryString["uid"] ?? "";
                    EnviarEstado(response, uid);
                }
                else if (request.HttpMethod == "GET" && absolutePath == "/Estacionamiento/EsperarCambio")
                {
                    string uid = request.QueryString["uid"] ?? "";
                    int ultimoContador;
                    lock (bloqueo) { ultimoContador = contadorCambios; }

                    while (Activo)
                    {
                        int actual;
                        lock (bloqueo) { actual = contadorCambios; }
                        if (actual != ultimoContador) break;
                        Thread.Sleep(500);
                    }
                    EnviarEstado(response, uid);
                }
                else if (request.HttpMethod == "POST" && absolutePath == "/Estacionamiento/Apartar")
                {
                    byte[] buffer = new byte[request.ContentLength64];
                    request.InputStream.ReadExactly(buffer, 0, buffer.Length);
                    string json = Encoding.UTF8.GetString(buffer);
                    var solicitud = JsonSerializer.Deserialize<ApartarRequestDTO>(json);

                    if (solicitud == null || solicitud.PosicionDeEstacionamiento < 1 || solicitud.PosicionDeEstacionamiento > 10)
                    {
                        response.StatusCode = 400;
                        response.Close();
                        return;
                    }

                    bool reservado = false;
                    lock (bloqueo)
                    {
                        bool Ocupado = LugaresOcupados.Any(l => l.PosicionDeEstacionamiento == solicitud.PosicionDeEstacionamiento);
                        if (!Ocupado)
                        {
                            LugaresOcupados.Add(new EstacionDTO
                            {
                                Uid = solicitud.Uid,
                                PosicionDeEstacionamiento = solicitud.PosicionDeEstacionamiento
                            });
                            contadorCambios++;
                            LugarApartado?.Invoke(solicitud.PosicionDeEstacionamiento);
                            reservado = true;
                        }
                    }

                    if (!reservado)
                    {
                        response.StatusCode = 409;
                        response.Close();
                        return;
                    }

                    EnviarEstado(response, solicitud.Uid);
                }
                else if (request.HttpMethod == "POST" && absolutePath == "/Estacionamiento/Desocupar")
                {
                    byte[] buffer = new byte[request.ContentLength64];
                    request.InputStream.ReadExactly(buffer, 0, buffer.Length);
                    string json = Encoding.UTF8.GetString(buffer);
                    var solicitud = JsonSerializer.Deserialize<ApartarRequestDTO>(json);

                    if (solicitud == null || solicitud.PosicionDeEstacionamiento < 1 || solicitud.PosicionDeEstacionamiento > 10)
                    {
                        response.StatusCode = 400;
                        response.Close();
                        return;
                    }

                    bool liberado = false;
                    lock (bloqueo)
                    {
                        var lugar = LugaresOcupados.FirstOrDefault(l => l.PosicionDeEstacionamiento == solicitud.PosicionDeEstacionamiento);
                        if (lugar != null && lugar.Uid == solicitud.Uid)
                        {
                            LugaresOcupados.Remove(lugar);
                            LugarDesocupado?.Invoke(solicitud.PosicionDeEstacionamiento);
                            contadorCambios++;
                            liberado = true;
                        }
                    }

                    if (!liberado)
                    {
                        response.StatusCode = 404;
                        response.Close();
                        return;
                    }

                    EnviarEstado(response, solicitud.Uid);
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {

                response.StatusCode = 500;
                response.Close();
                throw new Exception(ex.Message);
            }
        }

        private void EnviarEstado(HttpListenerResponse response, string uidCliente)
        {
            var estado = new EstadoGeneralDTO();
            lock (bloqueo)
            {
                for (int i = 1; i <= 10; i++)
                {
                    var ocupante = LugaresOcupados.FirstOrDefault(l => l.PosicionDeEstacionamiento == i);
                    if (ocupante == null)
                        estado.Lugares[i - 1] = "libre";
                    else if (ocupante.Uid == uidCliente)
                        estado.Lugares[i - 1] = "tuyo";
                    else
                        estado.Lugares[i - 1] = "ocupado";
                }
                estado.Libres = estado.Lugares.Count(s => s == "libre");
                estado.Ocupados = estado.Lugares.Count(s => s == "ocupado" || s == "tuyo");
            }

            string json = JsonSerializer.Serialize(estado);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
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
