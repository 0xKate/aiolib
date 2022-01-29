﻿using System;
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
                client.ConnReadyEvent.OnEvent += (sender, eventArgs) => eventArgs.Conn.SendData($"Hello from {client.ServerConnection.LocalEndPoint}");
                client.RecvEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Received data {eventArgs.Message} from server {client.ServerConnection.RemoteEndPoint}");
                client.ConnReadyEvent.OnEvent += OnConnectCallback;
                client.ConnClosedEvent.OnEvent += OnDisconnectCallback;
                client.RecvEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Received data {eventArgs.Message} from server {client.ServerConnection.RemoteEndPoint}");
                client.ConnErrorEvent.OnEvent += OnExceptionCallback;
                client.SslInitdEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"SSL Initialized with Server {eventArgs.Conn.RemoteEndPoint}");
                client.RecvWaitEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Waiting to receive with Server {eventArgs.Conn.RemoteEndPoint}");
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
