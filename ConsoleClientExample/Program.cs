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
            if (eventArgs.Connection != null)
                Console.WriteLine($"Client: Connected to server {eventArgs.Connection.RemoteEndPoint}");
        }
        static void OnDisconnectCallback(object? sender, ConnectionEventArgs eventArgs)
        {
            if (eventArgs.Connection != null)
                Console.WriteLine($"Client: Disconnected from server {eventArgs.Connection.RemoteEndPoint}");
        }
        static void OnReceiveCallback(object? sender, ConnectionEventArgs eventArgs)
        {
            if (eventArgs.Connection != null)
                Console.WriteLine($"Server {eventArgs.Connection.RemoteEndPoint} has sent data: {eventArgs.Message}");
        }
        static void OnExceptionCallback(object? sender, ConnectionEventArgs eventArgs)
        {
            if (eventArgs.Connection != null)
                Console.WriteLine($"Server {eventArgs.Connection.RemoteEndPoint} has caused exception: {eventArgs.Message}");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("You may type dirrectly into this window to send commands.");

            try
            {
                int port = 1025;
                string hostname = "gamesys.kfuji.net";
                bool running = true;

                aioStreamClient client = new aioStreamClient(port, hostname);
                client.ConnReadyEvent.OnEvent += (sender, eventArgs) => eventArgs.Connection.SendData($"Hello from {client.ServerConnection.LocalEndPoint}");
                client.RecvEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Server: Received data <{eventArgs.Message}' from server <{client.ServerConnection}>");
                client.ConnReadyEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Client: Connection with <{client.ServerConnection}> is ready.");
                client.ConnClosedEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Client: Connection with <{client.ServerConnection}> has closed.");
                client.ConnErrorEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Client: Connection with <{client.ServerConnection}> has errors ready.");
                client.SslInitdEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Client: SSL Initialized with Server <{eventArgs.Connection}>");
                client.RecvWaitEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Client: Waiting to receive with Server <{eventArgs.Connection}>");
                client.SendEvent.OnEvent += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
                //client.SslInitdEvent.OnEvent += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
                //client.TcpInitdEvent.OnEvent += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
                client.HandshakeInitdEvent.OnEvent += (sender, eventArgs) => Console.WriteLine("Client: " + eventArgs.Message);
                client.Run();

                while (running)
                {
                    Console.Write(String.Empty);
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
                    if (userInput == "test")
                    {
                        client.ServerConnection.SendData("Test Data");
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
