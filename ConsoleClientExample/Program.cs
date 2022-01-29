using System;
using System.Net;
using System.Threading.Tasks;
using aiolib;

namespace ConsoleProgram
{
    class ConsoleApp
    {
        static void OnConnectCallback(object? sender, ConnectionEventArgs eventArgs)
        {
            if (eventArgs.Conn != null)
                Console.WriteLine($"Connected to server {eventArgs.Conn.RemoteEndPoint}");
        }
        static void OnDisconnectCallback(object? sender, ConnectionEventArgs eventArgs)
        {
            if (eventArgs.Conn != null)
                Console.WriteLine($"Disconnected from server {eventArgs.Conn.RemoteEndPoint}");
        }
        static void OnReceiveCallback(object? sender, ConnectionEventArgs eventArgs)
        {
            if (eventArgs.Conn != null)
                Console.WriteLine($"Server {eventArgs.Conn.RemoteEndPoint} has sent data: {eventArgs.Message}");
        }
        static void OnExceptionCallback(object? sender, ConnectionEventArgs eventArgs)
        {
            if (eventArgs.Conn != null)
                Console.WriteLine($"Server {eventArgs.Conn.RemoteEndPoint} has caused exception: {eventArgs.Message}");
        }

        static void Main(string[] args)
        {
            try
            {
                int port = 1025;
                string hostname = "gamesys.kfuji.net";
                bool running = true;

                aioStreamClient client = new aioStreamClient(port, hostname);
                client.ConnReadyEvent.OnEvent += OnConnectCallback;
                client.ConnClosedEvent.OnEvent += OnDisconnectCallback;
                client.RecvEvent.OnEvent += OnReceiveCallback;
                client.ConnErrorEvent.OnEvent += OnExceptionCallback;
                client.Run();

                while (running)
                {
                    Console.Write("Enter Message: ");
                    var userInput = Console.ReadLine();

                    if (userInput == "Exit")
                    {
                        running = false;
                        break;
                    }
                    if (userInput == "restart")
                    {
                        client.Run();
                    }
                    else
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        Task SendDataTask = client.SendDataAsync(userInput);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                }
                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
