// This file is part of aiolib
// See https://github.com/0xKate/aiolib for more information
// Copyright (C) 0xKate <kate@0xkate.net>
// This program is published under a GPLv2 license
// https://github.com/0xKate/aiolib/blob/master/LICENSE

using System.IO;
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
        /// <summary>A reference to the underlying <typeparamref name="TcpClient"/></summary>>
        internal TcpClient ClientSocket { get; }
        ///<summary> The remote EndPoint. Can be converted to string to represent the remote connection as  IP:port .</summary>
        public IPEndPoint? RemoteEndPoint { get; }
        /// <summary>An <typeparamref name="IPHostEntry"/> that contains the IP address and hostname of the remote socket.</summary>>
        public IPHostEntry? RemoteHostname { get; }
        /// <summary>Returns true if the socket is still connected.</summary>>
        public bool IsConnected { get { return this.ClientSocket.Connected; } }
        #region Stream
        /// <summary>If connection is using SSL this will return a <typeparamref name="SslStream"/> otherwise it will return a <typeparamref name="NetworkStream"/>.</summary>>
        public object? NetStream 
        {
            get
            {
                if (this._connectionUpgraded)
                    return _SSLStream;
                else
                    return _Stream;
            }
        }
        internal NetworkStream _Stream;
        internal SslStream? _SSLStream;
        #endregion
        #region StreamReader
        /// <summary>
        /// A reference to the StreamReader. This is a string based stream that handles the auto-conversion between strings, bytes, and network transport. This property will return the SSLReader if the connection has been upgraded.
        /// </summary>
        public StreamReader? Reader
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
        internal StreamReader? _SSLReader;
        #endregion
        #region StreamWriter
        /// <summary>
        /// A reference to the StreamWriter. This is a string based stream that handles the auto-conversion between strings, bytes, and network transport.  This property will return the SSLWriter if the connection has been upgraded.
        /// </summary>
        public StreamWriter? Writer
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
        internal StreamWriter? _SSLWriter;
        #endregion
        /// <summary>Used internally to cancel the await on the StreamReader to more gracefully terminate clients.</summary> 
        public CancellationTokenSource ReaderTokenSource { get; }
        /// <summary>Child token passed to the await StreamReader.Read instide the receive loop. </summary>
        public CancellationToken ReaderToken { get; }
        /// <summary>Set to false to cancel the receive loop for this client.</summary>
        public bool Reading { get; set; }
        private bool _disposed = false;
        private bool _closed = false;
        /// <summary>Returns true if the connection has been upgraded to SSL.</summary>
        public bool ConnectionUpgraded { get { return _connectionUpgraded; } }
        private bool _connectionUpgraded = false;
        private bool _sslCappable;
        /// <summary>
        /// A container/wrapper for TcpClient and multiple handles to Unmanaged resources. As well as high level functions for interacting with the remote client and data stream.
        /// </summary>
        /// <param name="tcpClient">Pass the TcpClient obtained by the TcpListener.AcceptAsync to this to wrap it as a RemoteHost.</param>
        /// <param name="Hostname"> Some Text</param>
        /// <exception cref="ApplicationException"> Will throw an exception if we fail to obtain the EndPoint from the TcpClient.</exception>
        public RemoteHost(TcpClient tcpClient, IPHostEntry? Hostname=null)
        {
            if (Hostname != null)
                this._sslCappable = true;
            else
                this._sslCappable = false;

            this.ClientSocket = tcpClient;
            this.RemoteHostname = Hostname;            

            var ep = (IPEndPoint?)tcpClient.Client.RemoteEndPoint;
            if (ep == null)
                throw new ApplicationException("Tried to create a RemoteHost, but the underlying socket has closed.");
            RemoteEndPoint = ep;

            ReaderTokenSource = new CancellationTokenSource();
            ReaderToken = ReaderTokenSource.Token;

            // Get the network stream from the tcpClient (Used for reading/writing bytes over the network)
            _Stream = ClientSocket.GetStream();

            // Get a stream writer from the network stream (Used for writing strings over the network)
            _Writer = new StreamWriter(_Stream);

            // Get a stream reader from the network stream (A stream writer/reader automatically encodes strings into bytes for the underlying stream)
            _Reader = new StreamReader(_Stream);

            // AutoFlush causes the streamWritter buffer to instantly flush, instead of waiting for a manual flush.
            _Writer.AutoFlush = true;
        }
        /// <summary>Get the IPAddress of the RemoteHost</summary>
        public IPAddress? GetIPV4Address()
        {
            if (this.RemoteEndPoint == null)
                return null;
            else
                return this.RemoteEndPoint.Address;
        }
        /// <summary>
        /// Used internally or when an sending the data back from an asyncronous enviroment.
        /// </summary>
        /// <param name="data">A UTF-8 encoded string of data to send to the remote end of the connection.</param>
        /// <returns>An awaitable Task</returns>
        internal async Task SendDataAsync(string data)
        {
            // Write the data to the socket (remote client), but do so in an asyncronous manner so other tasks may run while the client receives it.
            if (Writer != null)
                await Writer.WriteLineAsync(data);
        }
        /// <summary>
        /// The recomended method for sending data to the client. A fire-and-forget method that Can be used from any enviroment.
        /// </summary>
        /// <param name="message"></param>
        public void SendData(string message)
        {
            _ = SendDataAsync(message);
        }
        private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null)
                return false;

            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            else // Do not allow this client to communicate with unauthenticated servers.
            {
                Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

                if (this._SSLStream != null)
                    if (this._SSLStream.IsAuthenticated)
                    {
                        X509Certificate? remoteCertificate = this._SSLStream.RemoteCertificate;
                        if (remoteCertificate != null)
                        {
                            Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                                remoteCertificate.Subject,
                                remoteCertificate.GetEffectiveDateString(),
                                remoteCertificate.GetExpirationDateString());
                        }

                        X509Certificate? localCertificate = this._SSLStream.LocalCertificate;
                        if (localCertificate != null)
                        {
                            Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                                localCertificate.Subject,
                                localCertificate.GetEffectiveDateString(),
                                localCertificate.GetExpirationDateString());
                        }

                        Console.WriteLine("Certificate revocation list checked: {0}", this._SSLStream.CheckCertRevocationStatus);
                    }                
                return false;
            }
        }
        /// <summary> Authenticates the SSL connection as a client, verifies the server certificate but not client. </summary>
        /// <param name="RemoteHostInfo">Contains the IPAddress and hostname of the remote host.</param>
        /// <returns>An awaitable task that may return a SslStream or null</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<SslStream?> SSLUpgradeAsClientAsync(IPHostEntry RemoteHostInfo)
        {
            //IsEncrypted and IsSigned properties to determine what security services are used by the SslStream. Check the IsMutuallyAuthenticated property
            X509Store store = new(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            this._SSLStream = new SslStream(this._Stream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            try
            {
                string host = RemoteHostInfo.HostName;
                if (host == null)
                    throw new ArgumentNullException(nameof(RemoteHostInfo));
                else
                {
                    await _SSLStream.AuthenticateAsClientAsync(host, store.Certificates, false);

                    if (!_SSLStream.IsEncrypted)
                        throw new AuthenticationException("Stream not encrypted!");

                    if (!_SSLStream.IsSigned)
                        throw new AuthenticationException("Stream not signed!");

                    //if (!_SSLStream.IsMutuallyAuthenticated)
                    //    throw new AuthenticationException("Stream not Mutually Authenticated!");

                    _SSLWriter = new StreamWriter(_SSLStream) { AutoFlush = true };
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
        /// <param name="serverCertificate">The X509Certificate being used to verify the server.</param>
        /// <returns></returns>
        /// <inheritdoc cref="SslStream.AuthenticateAsServerAsync(X509Certificate, bool, bool)"/>
        public async Task<SslStream?> SSLUpgradeAsServerAsync(X509Certificate serverCertificate)
        {
            //Console.WriteLine("Trying to create sslStream");
            this._SSLStream = new SslStream(this._Stream, false);
            //Console.WriteLine("Created sslStream instance");
            // Authenticate the server but don't require the client to authenticate.
            try
            {
                if (this._SSLStream != null)
                {
                    await this._SSLStream.AuthenticateAsServerAsync(serverCertificate, false, true);

                    if (!this._SSLStream.IsEncrypted)
                        throw new AuthenticationException("Stream not encrypted!");

                    if (!this._SSLStream.IsSigned)
                        throw new AuthenticationException("Stream not signed!");

                    //if (!this._SSLStream.IsMutuallyAuthenticated)
                    //    throw new AuthenticationException("Stream not Mutually Authenticated!");

                    this._SSLWriter = new StreamWriter(this._SSLStream);
                    _SSLWriter.AutoFlush = true;
                    this._SSLReader = new StreamReader(this._SSLStream);
                    this._connectionUpgraded = true;
                    return this._SSLStream;
                }
                return null;

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
        /// <returns>The IP:Port string representation of the object</returns>
        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            if (this.RemoteEndPoint == null)
                return string.Empty;
            else
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

            if (_SSLReader != null)
                _SSLReader.Close();
            if (_SSLWriter != null)
                _SSLWriter.Close();            

            _Reader.Close();
            _Writer.Close();

            if (_SSLStream != null)
                _SSLStream.Close();
            _Stream.Close();                

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
                ReaderTokenSource.Cancel();

                _SSLReader.DiscardBufferedData();
                _Reader.DiscardBufferedData();

                if (_SSLReader != null)
                    _SSLReader.Dispose();
                if (_Reader != null)
                    _Reader.Dispose();

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
        internal async Task DisposeAsync()
        {
            if (_disposed)
                return;

            await _SSLWriter.DisposeAsync();
            await _Writer.DisposeAsync();
            await _SSLStream.DisposeAsync();
            await _Stream.DisposeAsync();            
            Dispose(true);
        }
        /// <summary>
        /// Release all remaining resources for garbage collection.
        /// </summary>
        public void Dispose()
        {
            Task DisposeTask = DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
