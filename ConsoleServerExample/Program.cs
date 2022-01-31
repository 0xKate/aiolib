using System;
using System.Net;
using aiolib;

namespace StreamServerAsync
{
    class ConsoleApp
    {

        // If using a background thread trying to update WPF GUI, any calls on the GUI MUST use Dispatcher.BeginInvoke in the callback.
        /*
        static async void GUICallback(object sender, ReceiveEventArgs e)
        {
            // Any kind of background thread code here.
            e.SendResponse(e.Payload)

            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Any kind of GUI-altering code here.
                
            }));
        }
        */

        // Various simple event callbacks to print whats happening.
        /*
        static void OnConnectCallback(object sender, ClientEvents.ConnectEvent.ConnectArgs eventArgs)
        {
            Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has connected.");
            
        }
        static void OnDisconnectCallback(object sender, ClientEvents.DisconnectEvent.DisconnectArgs eventArgs)
        {
            Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has disconnected.");
        }
        static void OnReceiveCallback(object sender, ClientEvents.ReceiveEvent.ReceiveEventArgs eventArgs)
        {
            Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has sent data: {eventArgs.Payload}");
            string requestHeader = ":GETQUEST:";
            if (eventArgs.Payload.StartsWith(requestHeader))
            {
                var requestData = eventArgs.Payload.Substring(requestHeader.Length);
                Console.WriteLine("Received Quest Request.");
                eventArgs.Client.SendData(":QUESTDATA:<ArbitraryData>");
            }            
        }
        //static void OnExceptionCallback(object sender, ClientEvents.ExceptionEvent.ExceptionEventArgs eventArgs)
        //{
        //    Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has caused exception: {eventArgs.Error}");
        //}
        */


        static void Main(string[] args)
        {
            try
            {
                int port = 50000;
                IPAddress ipAddress = IPAddress.Parse("10.0.0.10");

                // Create an instance of the StreamServer
                aioStreamServer server = new aioStreamServer(ipAddress, port);

                // Hook into the OnReceiveEvent and echo back what we received. (Simple echo server in 1 line)
                server.Events.OnReceive += (sender, eventArgs) =>
                {
                    Console.WriteLine(eventArgs.Message);
                    eventArgs.Connection.SendData(eventArgs.Message);
                };
                
                server.Events.OnConnectionReady += (sender, eventArgs) =>
                {
                    Console.WriteLine(eventArgs.Message);
                    eventArgs.Connection.SendData($"Welcome, {eventArgs.Connection}!");
                };
                server.Events.OnConnectionClosed += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
                server.Events.OnException += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
                server.Events.OnListenReady += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
                server.Events.OnListenEnd += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
                server.Events.OnSend += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);

                server.StartListening();

                Console.WriteLine("Echo server is now running  on port " + port);
                Console.WriteLine("Hit <enter> to stop service\n");

                // This is the long running task that prevents the console app from closing. The thread chills here while asyncronously processing clients.
                Console.ReadLine();

                //server.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
