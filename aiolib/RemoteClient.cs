using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace aioStreamServerLib
{
    public class RemoteClient : IDisposable
    {
        public TcpClient ClientSocket { get; }
        public EndPoint RemoteEndPoint { get; }
        public NetworkStream Stream { get; }
        //public NetworkStream sslStream { get; }
        public StreamWriter Writer { get; }
        public StreamReader Reader { get; }
        public CancellationTokenSource ReaderTokenSource { get; }
        public CancellationToken ReaderToken { get; }
        public bool Reading { get; set; }
        private bool _disposed = false;
        private bool _closed = false;
        public RemoteClient(TcpClient tcpClient)
        {
            ClientSocket = tcpClient;
#pragma warning disable CS8601 // Possible null reference assignment.
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint;
#pragma warning restore CS8601 // I throw an exception so not sure why IDE is complaining.
            if (RemoteEndPoint == null)
            {
                throw new ApplicationException("Tried to create a RemoteClient, but the underlying socket has closed.");
            }

            ReaderTokenSource = new CancellationTokenSource();
            ReaderToken = ReaderTokenSource.Token;

            // Get the network stream from the tcpClient (Used for reading/writing bytes over the network)
            Stream = ClientSocket.GetStream();

            // SSL Todo: Add SSL authentication and upgrade network stream to SSLStream
            //SslStream sslStream = new SslStream(networkStream);

            // Get a stream writer from the network stream (Used for writing strings over the network)
            Writer = new StreamWriter(Stream);

            // Get a stream reader from the network stream (A stream writer/reader automatically encodes strings into bytes for the underlying stream)
            Reader = new StreamReader(Stream);

            // AutoFlush causes the streamWritter buffer to instantly flush, instead of waiting for a manual flush.
            Writer.AutoFlush = true;
        }
        public async Task SendDataAsync(string data)
        {
            // Write the data to the socket (remote client), but do so in an asyncronous manner so other tasks may run while the client receives it.
            await Writer.WriteLineAsync(data);
        }
        public void SendData(string message)
        {
            Task SendTask = SendDataAsync(message);
        }
        public void Close()
        {
            if (_closed)
                return;

            // Cleanup Resources that are done with.
            Reading = false;
            ReaderTokenSource.Cancel();
            Writer.Flush();
            Writer.Close();
            Reader.Close();
            Stream.Close();
            ClientSocket.Close();
            _closed = true;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                Writer.Dispose();
                Reader.Dispose();
                Stream.Dispose();                
            }
            // Free any unmanaged objects here.
            ClientSocket.Dispose();
            _disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
