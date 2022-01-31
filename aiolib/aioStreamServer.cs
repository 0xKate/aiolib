// This file is part of aiolib
// See https://github.com/0xKate/aiolib for more information
// Copyright (C) 0xKate <kate@0xkate.net>
// This program is published under a GPLv2 license
// https://github.com/0xKate/aiolib/blob/master/LICENSE

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace aiolib
{

    public class ServerException : Exception
    {
        public ServerException() { }
        public ServerException(string message) : base(message) { }
        public ServerException(string message, Exception inner) : base(message, inner) { }
    }

    public class ServerEventArgs : EventArgs
    {
        public aioStreamServer? Server;
        public String? Message;
        public ServerEventArgs(aioStreamServer? Server, string? Message = null)
        {
            this.Server = Server;
            this.Message = Message;
        }
    }
    public class ServerEvent
    {
        public event EventHandler<ServerEventArgs> OnEvent;
        public void Raise(aioStreamServer? server, string? message = null)
        {
            OnEventRaised(new ServerEventArgs(server, message));
        }
        protected virtual void OnEventRaised(ServerEventArgs e)
        {
            OnEvent?.Invoke(this, e);
        }
    }

    public class aioStreamServer
    {

        #region Properties
        /// <summary>
        /// List to keep track of connected clients.
        /// Note: To obvserve this from WPF, while the server is in a background thread, use the following line of code from the observing thread:  
        /// [private object lockObject = new object();], 
        /// [BindingOperations.EnableCollectionSynchronization(StreamServer.ConnectedClients, lockObject);]
        /// </summary>
        public ObservableCollection<Connection> ConnectedClients { get; }
        public Object ConnectedClientsLock = new Object();
        private Boolean IgnoreHandshake { get; }
        private Boolean EnableSSL { get; }
        private List<IPAddress> Blacklist { get; }
        private Int32 ListenPort { get; }
        private IPAddress ListenIp { get; }
        private X509Certificate? ServerCertificate { get; }
        private TcpListener ServerListener { get; }
        private Task? MainTask { get; set; }
        private CancellationTokenSource ListenTokenSource { get; set; }
        private CancellationToken ListenToken { get; set; }
        private Boolean ServerListening { get; set; }
        private Boolean _Started = false;
        private Boolean _Stopped = true;
        public ServerEvents Events { get; }
        #endregion
        public aioStreamServer(IPAddress listenIp, Int32 listenPort, Boolean ignoreHandshake = false, Boolean enableSSL = true)
        {
            // Create default instances
            this.ServerListener = new TcpListener(listenIp, listenPort);
            this.Blacklist = new List<IPAddress>();
            this.ConnectedClients = new ObservableCollection<Connection>();
            this.Events = new ServerEvents();

            // Initializer arguments
            this.ListenPort = listenPort;
            this.ListenIp = listenIp;
            this.EnableSSL = enableSSL;
            this.IgnoreHandshake = ignoreHandshake;

            if (this.EnableSSL == true)
            {
                // Generate random password out of random Guid
                string pass = Guid.NewGuid().ToString();
                // Combine the output of the extension created self-signed certificate
                X509Certificate ServerCertificate_temp = X509Certificate2.CreateFromPemFile("rsa.crt", "rsa.key");
                // Generate temporary certificate in memory with the random password
                this.ServerCertificate = new X509Certificate2(ServerCertificate_temp.Export(X509ContentType.Pfx, pass), pass);
                ServerCertificate_temp.Dispose();
            }
        }
        public void StartListening()
        {
            if (_Started)
                return;
            else
                _Started = true;

            if (this.MainTask == null || this.MainTask.IsCompleted == true)
            {
                this.MainTask = this.RunAsync();
            }

            _Stopped = false;
        }
        public void StopListening()
        {
            if (_Stopped)
                return;
            else
                _Stopped = true;

            if (this.MainTask != null && ServerListening)
            {
                this.ServerListening = false;
                foreach (Connection ClientConnection in this.ConnectedClients)
                {
                    ClientConnection.Close();
                    ClientConnection.Dispose();
                }
                this.ServerListener.Stop();
                this.ListenTokenSource.Cancel();
            }

            _Started = false;
        }
        public void Run()
        {
            this.StartListening();
        }
        internal async Task RunAsync()
        {
            Events.AcceptLoopReadyEvent.Raise(this, "Server is accepting connections.");
            await AcceptClientLoopAsync();
            Events.AcceptLoopEndEvent.Raise(this, "Server has finished accepting connections.");
        }
        internal bool ClientSecurityCheck(TcpClient remoteClient, IPEndPoint remoteEnd)
        {
            if (remoteClient == null)
                throw new ArgumentNullException(nameof(remoteClient));

            if (Blacklist.Count > 0)
                if (Blacklist.Any(item => item == remoteEnd.Address))
                {
                    return false;
                }
            return true;
        }
        internal async Task AcceptClientLoopAsync()
        {
            this.ListenTokenSource = new CancellationTokenSource();
            this.ListenToken = this.ListenTokenSource.Token;

            try
            {
                ServerListener.Start();
                ServerListening = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start listener: " + ex);
                return;
            }


            while (ServerListening)
            {
                try
                {
                    Events.AwaitAcceptEvent.Raise(this, "Server waiting for more connections.");
                    // await for new clients. The thread may do other things while awaiting.
                    TcpClient tcpClient = await ServerListener.AcceptTcpClientAsync(ListenToken);
                    // Low resource Easy access to IPAddress and port right away.
                    IPEndPoint? remoteEnd = (IPEndPoint?)tcpClient.Client.RemoteEndPoint;
                    Events.TcpAcceptEvent.Raise(tcpClient, $"Client: {remoteEnd} - Opened tcp connection.");

                    if (remoteEnd == null)
                        continue;

                    // Disconnect Blocked IP's
                    bool authorized = this.ClientSecurityCheck(tcpClient, remoteEnd);
                    if (!authorized)
                    {
                        try
                        {
                            Events.UnauthorizedConnectionEvent.Raise(tcpClient, $"Client: {remoteEnd} - Disconnecting Unauthorized Client @ {DateTime.Now}");
                            tcpClient.Close();
                            tcpClient.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error disposing of unauthorized client!: " + ex.ToString());
                        }
                        continue;
                    }

                    // Upgrade tcpClient to connection wrapper
                    Connection connection = new(tcpClient);

                    // Start New task with current client and continue waiting for new clients
                    Task WaitOrTimeoutTask = WaitForHandshakeAsync(connection);
                }
                catch (Exception err)
                {
                    Console.WriteLine($"Unexpected exception during client initialization: " + err.Message);
                    Events.ConnectionExceptionEvent.Raise(null, $"Client: null - Caused Exception:\n{err}");
                    //throw ex;
                }
            }
            this.ListenTokenSource.Dispose();
        }
        private async Task WaitForHandshakeAsync(Connection remoteClient)
        {
            Events.HandshakeBeginEvent.Raise(remoteClient, $"Client: {remoteClient} - Handshake Begin");
            CancellationTokenSource readLineTokenSource = new CancellationTokenSource();
            CancellationToken readLineToken = readLineTokenSource.Token;
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
                Task<string?> readerTask = remoteClient.ReadLineAsync().CancellableTask(readLineToken);
                if (await Task.WhenAny(readerTask, Task.Delay(timeout)) == readerTask)
                {
                    bool handshakeFailed = false;

                    Events.HandshakeReceiveEvent.Raise(remoteClient, $"Client: {remoteClient} - Handshake Received");

                    string digest = await remoteClient._Connection.GetClientDigest();

                    if (readerTask != null)
                        if (digest != readerTask.Result)
                            handshakeFailed = true;
                        else
                            handshakeFailed = false;

                    if (IgnoreHandshake)
                        handshakeFailed = false;

                    if (!handshakeFailed)
                    {
                        Events.HandshakeCompleteEvent.Raise(remoteClient, $"Client: {remoteClient} - Handshake Complete");

                        // Keep track of client (Triggers ObservableCollection update)
                        ConnectedClientsLock = true;
                        ConnectedClients.Add(remoteClient);
                        ConnectedClientsLock = false;

                        // Attempt SSL Upgrade
                        if (EnableSSL)
                        {
                            if (this.ServerCertificate == null)
                            {
                                throw new ServerException("SSL Requires an X509Certificate ! - Null certificate error.");
                            }

                            Task<SslStream?> sslUpgradeTask = remoteClient._Connection.SSLUpgradeAsServerAsync(this.ServerCertificate);
                            Task sendDigestTask = remoteClient.SendDataAsync(digest);
                            await sendDigestTask;
                            SslStream? ssl = await sslUpgradeTask;

                            if (ssl != null)
                            {
                                if (ssl.CanRead)
                                {
                                    if (ssl.CanWrite)
                                    {
                                        if (ssl.IsAuthenticated)
                                        {
                                            Events.SSLReadyEvent.Raise(remoteClient, $"Client: {remoteClient} - SSL Initialized Successfully");
                                            remoteClient._Connection.DisplayConnectedCertInfo();
                                            await HandleClientAsyncTask(remoteClient);
                                            return;
                                        }
                                    }
                                }
                            }

                            Events.SSLFailEvent.Raise(remoteClient, $"Client: {remoteClient} - SSL Failed to Initialize");
                            return;

                        }
                        else
                        {
                            await HandleClientAsyncTask(remoteClient);
                            return;
                        }
                    }
                    else
                    {
                        Events.HandshakeFailedEvent.Raise(remoteClient, $"Client: {remoteClient} - Handshake Failed");

                        remoteClient.Close();
                        remoteClient.Dispose();

                        Events.ConnectionClosedEvent.Raise(remoteClient, $"Client: {remoteClient} - Server Dropped (Handshake Failed)");
                    }
                }
                else
                {
                    Console.WriteLine($"Handshake timeout from cient {remoteClient}");
                    if (readLineTokenSource != null)
                        readLineTokenSource.Cancel();
                }
            }
            catch (OperationCanceledException err)
            {
                Console.WriteLine("Canceled await handshake task.");
                Events.VerboseConnectionExceptionEvent.Raise(remoteClient, $"Client: {remoteClient} - Caused Exception:\n{err}");
            }
            catch (Win32Exception err)
            {
                Console.WriteLine("Caught Win32Exception in WaitForHandshake - Exception: " + err);
                Events.VerboseConnectionExceptionEvent.Raise(remoteClient, $"Client: {remoteClient} - Caused Exception:\n{err}");
            }
            catch (IOException err)
            {
                Console.WriteLine("Caught IOException in WaitForHandshake - Exception: " + err);
                Events.VerboseConnectionExceptionEvent.Raise(remoteClient, $"Client: {remoteClient} - Caused Exception:\n{err}");
            }
            catch (Exception err)
            {
                Console.WriteLine("Unhandled exception in WaitForHandshake: " + err);
                Events.VerboseConnectionExceptionEvent.Raise(remoteClient, $"Client: {remoteClient} - Caused Exception:\n{err}");
                //throw;
            }
            finally
            {
                if (remoteClient != null)
                {
                    remoteClient.Close();
                    remoteClient.Dispose();
                }
            }
            //Console.WriteLine("WaitForHandshake completed without crashing");
        }

        private async Task HandleClientAsyncTask(Connection remoteClient)
        {
            Events.ConnectionReadyEvent.Raise(remoteClient, $"Client: {remoteClient} - Connection Ready");
            try
            {
                remoteClient._Connection.Reading = true;
                while (remoteClient.IsConnected && remoteClient._Connection.Reading)
                {
                    // Waits here for data from the client. The thread will work elsewere until data is received.
                    string? request = await remoteClient.ReadLineAsync().CancellableTask(remoteClient._Connection.ReaderToken);

                    if (request != null)
                    {
                        string payload = request;
                        // Signal a client has sent some data, passes the connected client and the entire payload received
                        Events.ReceiveEvent.Raise(remoteClient, payload);
                    }
                    else
                    {
                        break; // Client closed connection
                    }
                }
            }
            // The StreamReader.ReadLineAsync will throw IO exception when the underlying socket closes.
            catch (IOException err) // Discarding IOException w/ Inner:SocketException only
            // Discarding Win32Exception (0x80090325): The certificate chain was issued by an authority that is not trusted.
            {
                var InnerEx = err.InnerException;
                if (InnerEx == null)
                {
                    Events.ConnectionExceptionEvent.Raise(remoteClient, $"Client: {remoteClient} - Caused Exception:\n{err}");
                    throw;
                }
                else if (InnerEx.GetType() != typeof(SocketException))
                {
                    if (InnerEx.GetType() != typeof(Win32Exception))
                    {
                        Events.ConnectionExceptionEvent.Raise(remoteClient, $"Client: {remoteClient} - Caused Exception:\n{err}");
                        throw;
                    }
                }

                // Prints the discarded exceptions
                //Console.WriteLine(err);
            }
            // The StreamReader.ReadLineAsync inner exception is a SocketException socket closed by remote host.
            catch (Exception err)
            {
                Console.WriteLine($"Unhandled Exception: {err}");
                Events.ConnectionExceptionEvent.Raise(remoteClient, $"Client: {remoteClient} - Caused Exception:\n{err}");
                throw;
            }
            finally
            {
                // Raise a disconnect event.
                Events.ConnectionClosedEvent.Raise(remoteClient, $"Client: {remoteClient} - Connection Closed");
            
                // Cleanup the handles and resources now that we are done with them.                

                ConnectedClientsLock = true;
                _ = ConnectedClients.Remove(remoteClient);
                ConnectedClientsLock = false;

                remoteClient.Close();
                remoteClient.Dispose();

                Console.WriteLine($"Client Disposed. Remaining clients: {ConnectedClients.Count}");
            }
            //Console.WriteLine($"HandleClientAsyncTask finished without crashing");
        }

    }
}



