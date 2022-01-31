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
            StreamServer = new aioStreamServer(ServerIP, ServerPort);
            BindingOperations.EnableCollectionSynchronization(StreamServer.ConnectedClients, StreamServer.ConnectedClientsLock);

            StreamServer.Events.OnReceive += (sender, eventArgs) =>
            {
                Console.WriteLine(eventArgs.Message);
                eventArgs.Connection.SendData(eventArgs.Message);
            };

            StreamServer.Events.OnConnectionReady += (sender, eventArgs) =>
            {
                Console.WriteLine(eventArgs.Message);
                eventArgs.Connection.SendData($"Welcome, {eventArgs.Connection}!");
            };
            StreamServer.Events.OnAwaitAccept += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnConnectionClosed += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnException += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnListenReady += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnListenEnd += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnSend += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnConnectionException += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnSSLFail += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnHandshakeComplete += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnHandshakeFailed += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnHandshakeReceived += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnHandshakePending += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnSSLReady += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnAwaitRecieve += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);
            StreamServer.Events.OnVerboseConnectionException += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);




            //worker.DoWork += worker_DoWork;
            //worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }


        internal void RunServer()
        {
            StreamServer.Run();
        }
    }
}

