using aiolib;
using aioStreamServerLib;
using System;
using System.Net;

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
        }
        static void OnExceptionCallback(object sender, ClientEvents.ExceptionEvent.ExceptionEventArgs eventArgs)
        {
            Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has caused exception: {eventArgs.Error}");
        }


        static void Main(string[] args)
        {
            try
            {
                int port = 50000;
                IPAddress ipAddress = IPAddress.Parse("10.0.0.10");

                // Create an instance of the StreamServer
                aioStreamServer server = new aioStreamServer(port, ipAddress);

                // Hook into the OnReceiveEvent and echo back what we received. (Simple echo server in 1 line)

                server.ClientEventsPublisher.receiveEvent.OnReceive += (sender, receiveArgs) => receiveArgs.Client.SendData(receiveArgs.Payload);

                server.ClientEventsPublisher.connectEvent.OnConnect += OnConnectCallback;
                server.ClientEventsPublisher.disconnectEvent.OnDisconnect += OnDisconnectCallback;
                server.ClientEventsPublisher.receiveEvent.OnReceive += OnReceiveCallback;
                server.ClientEventsPublisher.exceptionEvent.OnException += OnExceptionCallback;

                // server.ReceiveEventPublisher.OnReceive += GUICallback;

                // Start the server. Note: this is asyncronous, the lack of await here allows the thread to continue to the writeLine below while also processing clients.
                server.Run();

                Console.WriteLine("Echo server is now running  on port " + port);
                Console.WriteLine("Hit <enter> to stop service\n");

                // This is the long running task that prevents the console app from closing. The thread chills here while asyncronously processing clients.
                Console.ReadLine();

                server.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
