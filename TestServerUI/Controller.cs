using aiolib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace TestServerUI
{

    public class Controller
    {
        public int ServerPort = 1025;
        public IPAddress ServerIP = IPAddress.Parse("10.0.0.10");
        public aioStreamServer StreamServer;
        private BackgroundWorker worker;


        internal Controller()
        {
            worker = new BackgroundWorker();
            StreamServer = new aioStreamServer(ServerPort, ServerIP);
            StreamServer.ClientEventsPublisher.connectEvent.OnConnect += OnConnectCallback;
            StreamServer.ClientEventsPublisher.disconnectEvent.OnDisconnect += OnDisconnectCallback;
            StreamServer.ClientEventsPublisher.receiveEvent.OnReceive += OnReceiveCallback;
            StreamServer.ClientEventsPublisher.exceptionEvent.OnException += OnExceptionCallback;
            //StreamServer.ClientEventsPublisher.receiveEvent.OnReceive += (sender, receiveArgs) => receiveArgs.Client.SendData("Echo: " + receiveArgs.Payload);
            StreamServer.ignoreHandshake = false;
            BindingOperations.EnableCollectionSynchronization(StreamServer.ConnectedClients, StreamServer.ConnectedClientsLock);

            worker.DoWork += worker_DoWork;
            //worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        internal void RunServer()
        {
            Task ServerTask = StreamServer.Run();
            //worker.RunWorkerAsync();
        }

        private async void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Background worker starting...");
            await StreamServer.Run();
            Console.WriteLine("StreamServer.Run() ended");
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.WriteLine("Background worker ended.");
        }
        private void OnConnectCallback(object sender, ClientEvents.ConnectEvent.ConnectArgs eventArgs)
        {
            eventArgs.Client.SendData($"Welcome {eventArgs.Client}");
            Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has connected.");
        }
        private void OnDisconnectCallback(object sender, ClientEvents.DisconnectEvent.DisconnectArgs eventArgs)
        {
            Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has disconnected.");
        }
        private void OnReceiveCallback(object sender, ClientEvents.ReceiveEvent.ReceiveEventArgs eventArgs)
        {
            Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has sent data: {eventArgs.Payload}");
        }
        private void OnExceptionCallback(object sender, ClientEvents.ExceptionEvent.ExceptionEventArgs eventArgs)
        {
            Console.WriteLine($"Client {eventArgs.Client.RemoteEndPoint} has caused exception: {eventArgs.Error}");
        }

    }
}

