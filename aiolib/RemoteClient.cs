using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace aiolib
{
    /// A TcpClient wrapper, this is essentially how the server sees and interacts with its clients.
    public class RemoteClient : IDisposable
    {
        /// A reference to the underlying TcpSocket. Use Higher level methods for sending/receiving if possible.
        public TcpClient ClientSocket { get; }
        /// The remote EndPoint. Can be converted to string to represent the remote connection as  <IP>:<port>.
        public EndPoint RemoteEndPoint { get; }
        /// A reference to the underlying NetworkStream, one level higher than TcpClient, this is a byte stream.
        public NetworkStream Stream { get; }
        //public NetworkStream sslStream { get; }
        /// A reference to the StreamWriter. This is a string based stream that handles the auto-conversion between strings, bytes, and network transport.
        public StreamWriter Writer { get; }
        /// A reference to the StreamReader. This is a string based stream that handles the auto-conversion between strings, bytes, and network transport.
        public StreamReader Reader { get; }
        /// Used internally to cancel the await on the StreamReader to more gracefully terminate clients.
        public CancellationTokenSource ReaderTokenSource { get; }
        /// Child token passed to the await StreamReader.Read instide the receive loop.
        public CancellationToken ReaderToken { get; }
        /// Set to false to cancel the receive loop for this client.
        public bool Reading { get; set; }
        private bool _disposed = false;
        private bool _closed = false;
        /// <summary>
        /// A container/wrapper for TcpClient and multiple handles to Unmanaged resources. As well as high level functions for interacting with the remote client and data stream.
        /// </summary>
        /// <param name="tcpClient">Pass the TcpClient obtained by the TcpListener.AcceptAsync to this to wrap it as a RemoteClient.</param>
        /// <exception cref="ApplicationException"> Will throw an exception if we fail to obtain the EndPoint from the TcpClient.</exception>
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
        /// <summary>
        /// Used internally or when an sending the data back from an asyncronous enviroment.
        /// </summary>
        /// <param name="data">A string of data to send to the remote end of the connection.</param>
        /// <returns>An awaitable Task</returns>
        public async Task SendDataAsync(string data)
        {
            // Write the data to the socket (remote client), but do so in an asyncronous manner so other tasks may run while the client receives it.
            await Writer.WriteLineAsync(data);
        }
        /// <summary>
        /// The recomended method for sending data to the client. A fire-and-forget method that Can be used from any enviroment.
        /// </summary>
        /// <param name="message"></param>
        public void SendData(string message)
        {
            Task SendTask = SendDataAsync(message);
        }
        /// Returns the <IP>:<Port> of the remote client in string format. --
        public override string ToString()
        {
            return this.RemoteEndPoint.ToString();
        }
        /// <summary>
        /// Close the connection and start to close all handles to assosicated resources.
        /// </summary>
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
        /// <summary>
        ///  Used internally.
        /// </summary>
        /// <param name="disposing"></param>
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
        /// <summary>
        /// Release all remaining resources for garbage collection.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
