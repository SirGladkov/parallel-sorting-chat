using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer
{
    class Program
    {
        private static readonly List<TcpClient> _clients = new List<TcpClient>();
        private static readonly object _lock = new object();
        private static int _clientCount = 0;

        static void Main(string[] args)
        {
            var server = new TcpListener(IPAddress.Loopback, 8888);
            server.Start();
            Console.Title = "Чат-сервер";
            Console.WriteLine("Сервер запущен. Подключено клиентов: 0");
            UpdateStatus();

            while (true)
            {
                try
                {
                    var client = server.AcceptTcpClient();
                    Console.WriteLine("[+] Клиент подключился.");

                    lock (_lock)
                    {
                        _clients.Add(client);
                        _clientCount++;
                    }

                    UpdateStatus();

                    var thread = new Thread(() => HandleClient(client));
                    thread.IsBackground = true;
                    thread.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Сервер: ошибка при подключении клиента - {ex.Message}");
                }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            try
            {
                string message;
                while ((message = reader.ReadLine()) != null)
                {
                    Console.WriteLine($"Получено: {message}");
                    Broadcast(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Клиент отключился: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _clients.Remove(client);
                    _clientCount--;
                }

                client.Close();
                Console.WriteLine("[-] Клиент отключился.");
                UpdateStatus();
            }
        }

        private static void Broadcast(string message)
        {
            lock (_lock)
            {
                var deadClients = new List<TcpClient>();
                foreach (var client in _clients)
                {
                    try
                    {
                        var w = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };
                        w.WriteLine(message);
                    }
                    catch
                    {
                        deadClients.Add(client);
                    }
                }

                foreach (var dead in deadClients)
                {
                    _clients.Remove(dead);
                    dead.Close();
                }
            }
        }

        private static void UpdateStatus()
        {
            var top = Console.CursorTop;
            var left = Console.CursorLeft;

            Console.SetCursorPosition(0, 0);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);

            Console.WriteLine($"Сервер запущен. Подключено клиентов: {_clientCount}");

            Console.SetCursorPosition(left, top);
        }
    }
}