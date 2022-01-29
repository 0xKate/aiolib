﻿using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace aiolib
{
    /// A TcpClient wrapper, this is essentially how the server sees and interacts with its clients.
    public class RemoteHost : IDisposable
    {
        /// A reference to the underlying TcpSocket. Use Higher level methods for sending/receiving if possible.
        internal TcpClient ClientSocket { get; }
        /// The remote EndPoint. Can be converted to string to represent the remote connection as  <IP>:<port>.
        public IPEndPoint RemoteEndPoint { get; }
        public IPHostEntry RemoteHostname { get; }
        public bool IsConnected { get { return this.ClientSocket.Connected; } }

        #region Stream
        /// <summary>
        /// A reference to the underlying NetworkStream, one level higher than TcpClient, this is a byte stream. This property will return the SSL Stream if the connection has been upgraded.
        /// </summary>
        public object NetStream 
        {
            get
            {
                if (this._connectionUpgraded)
                    return (SslStream)_SSLStream;
                else
                    return (NetworkStream)_Stream;
            }
        }
        internal NetworkStream _Stream;
        internal SslStream _SSLStream;
        #endregion
        #region StreamReader
        /// <summary>
        /// A reference to the StreamReader. This is a string based stream that handles the auto-conversion between strings, bytes, and network transport. This property will return the SSLReader if the connection has been upgraded.
        /// </summary>
        public StreamReader Reader
        {
            get
            {
                if (this._connectionUpgraded)
                    return _SSLReader;
                else
                    return _Reader;
            }
        }
        internal StreamReader _Reader;
        internal StreamReader _SSLReader;
        #endregion
        #region StreamWriter
        /// <summary>
        /// A reference to the StreamWriter. This is a string based stream that handles the auto-conversion between strings, bytes, and network transport.  This property will return the SSLWriter if the connection has been upgraded.
        /// </summary>
        public StreamWriter Writer
        {
            get
            {
                if (this._connectionUpgraded)
                    return _SSLWriter;
                else
                    return _Writer;
            }
        }
        internal StreamWriter _Writer;
        internal StreamWriter _SSLWriter;
        #endregion
        /// Used internally to cancel the await on the StreamReader to more gracefully terminate clients.
        public CancellationTokenSource ReaderTokenSource { get; }
        /// Child token passed to the await StreamReader.Read instide the receive loop.
        public CancellationToken ReaderToken { get; }
        /// Set to false to cancel the receive loop for this client.
        public bool Reading { get; set; }
        private bool _disposed = false;
        private bool _closed = false;
        public bool ConnectionUpgraded { get { return _connectionUpgraded; } }
        private bool _connectionUpgraded = false;
        /// <summary>
        /// A container/wrapper for TcpClient and multiple handles to Unmanaged resources. As well as high level functions for interacting with the remote client and data stream.
        /// </summary>
        /// <param name="tcpClient">Pass the TcpClient obtained by the TcpListener.AcceptAsync to this to wrap it as a RemoteHost.</param>
        /// <exception cref="ApplicationException"> Will throw an exception if we fail to obtain the EndPoint from the TcpClient.</exception>
        public RemoteHost(TcpClient tcpClient, IPHostEntry RemoteHostname=null)
        {
            this.RemoteHostname = RemoteHostname;

            ClientSocket = tcpClient;
            RemoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            if (RemoteEndPoint == null)
            {
                throw new ApplicationException("Tried to create a RemoteHost, but the underlying socket has closed.");
            }

            ReaderTokenSource = new CancellationTokenSource();
            ReaderToken = ReaderTokenSource.Token;

            // Get the network stream from the tcpClient (Used for reading/writing bytes over the network)
            _Stream = ClientSocket.GetStream();

            // SSL Todo: Add SSL authentication and upgrade network stream to SSLStream
            //SslStream sslStream = new SslStream(networkStream);

            // Get a stream writer from the network stream (Used for writing strings over the network)
            _Writer = new StreamWriter(_Stream);

            // Get a stream reader from the network stream (A stream writer/reader automatically encodes strings into bytes for the underlying stream)
            _Reader = new StreamReader(_Stream);

            // AutoFlush causes the streamWritter buffer to instantly flush, instead of waiting for a manual flush.
            _Writer.AutoFlush = true;
        }
        public IPAddress GetIPV4Address()
        {
            return this.RemoteEndPoint.Address;
        }
        /// <summary>
        /// Used internally or when an sending the data back from an asyncronous enviroment.
        /// </summary>
        /// <param name="data">A string of data to send to the remote end of the connection.</param>
        /// <returns>An awaitable Task</returns>
        internal async Task SendDataAsync(string data)
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
        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            // Do not allow this client to communicate with unauthenticated servers.
             return false;
        }
        public async Task<SslStream?> SSLUpgradeAsClientAsync(IPHostEntry RemoteHostInfo)
        {
            //IsEncrypted and IsSigned properties to determine what security services are used by the SslStream. Check the IsMutuallyAuthenticated property
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            this._SSLStream = new SslStream(this._Stream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            try
            {
                string host = RemoteHostInfo.HostName;
                if (RemoteHostname == null)
                    throw new ArgumentNullException(nameof(RemoteHostInfo));
                else
                {
                    await _SSLStream.AuthenticateAsClientAsync(host, store.Certificates, false);

                    if (!_SSLStream.IsEncrypted)
                        throw new AuthenticationException("Stream not encrypted!");

                    if (!_SSLStream.IsSigned)
                        throw new AuthenticationException("Stream not signed!");

                    if (!_SSLStream.IsMutuallyAuthenticated)
                        throw new AuthenticationException("Stream not Mutually Authenticated!");

                    _SSLWriter = new StreamWriter(_SSLStream);
                    _SSLReader = new StreamReader(_SSLStream);
                    _connectionUpgraded = true;
                    return _SSLStream;
                }
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
            }
            return null;
        }
        public async Task<SslStream> SSLUpgradeAsServerAsync(X509Certificate serverCertificate)
        {
            SslStream sslStream = new SslStream(this._Stream, false);
            // Authenticate the server but don't require the client to authenticate.
            try
            {
                await sslStream.AuthenticateAsServerAsync(serverCertificate, false, true);

                if (!this._SSLStream.IsEncrypted)
                    throw new AuthenticationException("Stream not encrypted!");

                if (!this._SSLStream.IsSigned)
                    throw new AuthenticationException("Stream not signed!");

                if (!this._SSLStream.IsMutuallyAuthenticated)
                    throw new AuthenticationException("Stream not Mutually Authenticated!");

                this._SSLWriter = new StreamWriter(this._SSLStream);
                this._SSLReader = new StreamReader(this._SSLStream);
                this._connectionUpgraded = true;
                return this._SSLStream;
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
            }
            return null;
        }
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

            _Reader.Close();
            _Writer.Close();
            _Stream.Close();

            if (_SSLReader != null)
                _SSLReader.Close();
            if (_SSLWriter != null)
                _SSLWriter.Close();
            if (_SSLStream != null)
                _SSLStream.Close();

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

                _Stream.Dispose();
                _Reader.Dispose();
                _Writer.Dispose();

                if (_SSLWriter != null)
                    _SSLWriter.Dispose();
                if (_SSLStream != null)
                    _SSLStream.Dispose();
                if (_SSLReader != null)
                    _SSLReader.Dispose();

                ReaderTokenSource.Dispose();

                // Free any other managed objects here.
                //Writer.Dispose();
                //Reader.Dispose();
                //NetStream.Dispose();
                //ReaderTokenSource.Dispose();
            }
            // Free any unmanaged objects here.
            if (ClientSocket != null)
                ClientSocket.Dispose();
            _disposed = true;

        }
        /*
        internal async Task DisposeAsync()
        {
            await _Stream.DisposeAsync();
            await _SSLStream.DisposeAsync();
        }
        */
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