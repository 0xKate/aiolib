using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace StreamClientAsync
{
    public class ReceiveEventArgs : EventArgs
    {
        public TcpClient RemoteSocket { get; set; }
        public string Payload { get; set; }

        public ReceiveEventArgs(TcpClient remoteSocket, string payload)
        {
            RemoteSocket = remoteSocket;
            Payload = payload;
        }
        private async Task SendDataAsync(string data)
        {
            NetworkStream networkStream = this.RemoteSocket.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            writer.AutoFlush = true;
            await writer.WriteLineAsync(data);
        }
        public void SendResponse(string message)
        {
            Task SendTask = SendDataAsync(message);
        }
    }

    public class ReceiveEvent
    {
        public event EventHandler<ReceiveEventArgs> OnReceive = delegate { };

        public void Raise(TcpClient remoteSocket, string payload)
        {
            ReceiveEventArgs eventArgs = new ReceiveEventArgs(remoteSocket, payload);
            List<Exception> exceptions = new List<Exception>();
            foreach (Delegate handler in OnReceive.GetInvocationList())
            {
                try
                {
                    handler.DynamicInvoke(this, eventArgs);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }
    }

    public class aioStreamClient
    {
        public ReceiveEvent ReceiveEventPublisher;
        private int port;
        private IPAddress ipAddress;
        private TcpClient remoteSocket;
        private NetworkStream networkStream;
        private string remoteEndPoint;
        public aioStreamClient(int port, IPAddress ipAddress)
        {
            this.port = port;
            this.ipAddress = ipAddress;

            ReceiveEventPublisher = new ReceiveEvent();

            remoteSocket = new TcpClient(this.ipAddress.ToString(), this.port);
            remoteEndPoint = remoteSocket.Client.RemoteEndPoint.ToString();
            networkStream = remoteSocket.GetStream();
        }
        public void Run()
        {
            if (remoteSocket.Connected)
            {
                Console.WriteLine("Connected to " + remoteEndPoint);
                Task ReceiveLoopTask = ReceiveLoopAsync();
                Task SendDataTask = SendDataAsync("Hello from " + remoteEndPoint);
            }
        }

        public async Task SendDataAsync(string data)
        {
            StreamWriter writer = new StreamWriter(networkStream);
            writer.AutoFlush = true;
            await writer.WriteLineAsync(data);
        }

        public async Task ReceiveLoopAsync()
        {
            try
            {
                StreamReader reader = new StreamReader(networkStream);
                while (true)
                {
                    string request = await reader.ReadLineAsync();
                    if (request != null)
                    {
                        Console.WriteLine($"Received data from server {remoteEndPoint}: " + request);
                        ReceiveEventPublisher.Raise(remoteSocket, request);
                    }
                    else
                        break; // Client closed connection
                }
                remoteSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (remoteSocket.Connected)
                    remoteSocket.Close();
            }
        }

    }
}