using PoryectoClienteServidor.Models.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace PoryectoClienteServidor.Services
{
    public class TableroService
    {
        private HttpListener servidor;
        public List<UsuarioDTO> EnEspera { get; set; } = new List<UsuarioDTO>();
        public UsuarioDTO? Atendiendo { get; set; }
        public List<UsuarioDTO> Átendidos { get; set; } = new List<UsuarioDTO>();
        public event Action? TableroActualizado;
        public bool Activo;
        public TableroService()
        {
            servidor = new HttpListener();
            servidor.Prefixes.Add("http://localhost:5100/tablero/");
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
                if (request.HttpMethod == "GET" && request.RawUrl == "/tablero/")
                {
                    ServirArchivo(response, "index.html", "text/html");
                }
                if (request.HttpMethod == "GET" && request.RawUrl == "/tablero/operador")
                {
                    ServirArchivo(response, "operador.html", "text/html");
                }
                else if (request.HttpMethod == "POST" && request.RawUrl == "/tablero/registrar")
                {
                    byte[] buffer = new byte[request.ContentLength64];
                    request.InputStream.ReadExactly(buffer, 0, buffer.Length);
                    var json = Encoding.UTF8.GetString(buffer);
                    var usuario = System.Text.Json.JsonSerializer.Deserialize<UsuarioDTO>(json);
                    

                    if (usuario == null)
                    {
                        response.StatusCode = 400;
                        response.Close();
                    }
                    else
                    {
                        usuario.Turno = usuario.Turno == 0 ? 1 : EnEspera.Max(u => u.Turno) + 1;
                        usuario.Estado = "En espera";
                        EnEspera.Add(usuario);
                        TableroActualizado?.Invoke();
                        response.StatusCode = 200;
                        response.Close();
                    }
                }
                else if (request.HttpMethod== "POST" && request.RawUrl == "/tablero/atender")
                {
                    if (EnEspera.Count > 0)
                    {
                        Atendiendo = EnEspera[0];
                        EnEspera.RemoveAt(0);                      
                        TableroActualizado?.Invoke();
                        response.StatusCode = 200;
                        response.Close();
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

        public void Serealizar()
        {

        }
        public void Deserealizar()
        {

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
