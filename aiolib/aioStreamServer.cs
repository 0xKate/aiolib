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
    public class ConnectionException : Exception
    {
        public ConnectionException()
        {
        }

        public ConnectionException(string message)
            : base(message)
        {
        }

        public ConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class aioStreamServer
    {
        /// <summary>
        /// List to keep track of connected clients.
        /// Note: To obvserve this from WPF, while the server is in a background thread, use the following line of code from the observing thread:  
        /// [private object lockObject = new object();], 
        /// [BindingOperations.EnableCollectionSynchronization(StreamServer.ConnectedClients, lockObject);]
        /// </summary>
        public ObservableCollection<RemoteHost> ConnectedClients { get; }
        /// Used in-case outside insertions or deletions to the ConnectedClients need to be made. If so, please respect the lock.
        public bool ConnectedClientsLock = false;
        //public Authorization auth;
        /// <summary>
        /// Set to falseif you only want clients who follow our specific handshake to connect.
        /// </summary>
        public bool EnableSSL { get; set; }
        public bool ignoreHandshake { get; set; }
        private List<IPAddress> _Blacklist;
        public List<IPAddress> Blacklist { get { return _Blacklist; } }
        public ClientEvents ClientEventsPublisher { get; }
        public ServerEvents ServerEventsPublusher { get; }
        public bool ServerRunning { get; set; }
        private int Port { get; }
        private IPAddress IpAddress { get; }
        private CancellationTokenSource ListenTokenSource { get; }
        private CancellationToken ListenToken { get; }
        public X509Certificate ServerCertificate { get; private set; }

        private TcpListener listener;
        public aioStreamServer(int listenPort, IPAddress listenAddress)//, string certificate_loc) // SSL
        {
            //ServerCertificate = X509Certificate.CreateFromCertFile("test.crt");
            string pass = Guid.NewGuid().ToString();
            X509Certificate ServerCertificate_temp = X509Certificate2.CreateFromPemFile("rsa.crt", "rsa.key");
            ServerCertificate = new X509Certificate2(ServerCertificate_temp.Export(X509ContentType.Pfx, pass), pass);
            

            //ServerCertificate = X509Certificate2.create
            EnableSSL = true;
            ignoreHandshake = true;
            _Blacklist = new List<IPAddress>();
            ConnectedClients = new ObservableCollection<RemoteHost>();
            ClientEventsPublisher = new ClientEvents();
            ServerEventsPublusher = new ServerEvents();
            ServerRunning = true;
            Port = listenPort;
            IpAddress = listenAddress;

            ListenTokenSource = new CancellationTokenSource();
            ListenToken = ListenTokenSource.Token;

            //ServerEventsPublusher.initialized.Raise(this);

            // SSL
            //serverCertificate = X509Certificate.CreateFromCertFile(certificate_loc);
        }
        public bool BlacklistIP(IPAddress ipAddress)
        {
            if (this._Blacklist.Contains(ipAddress))
            {
                return false;
            }
            else
            {
                this._Blacklist.Add(ipAddress);
                return true;
            }
        }
        public async Task Run()
        {
            //High level C# api for creating socket server.
            listener = new TcpListener(IpAddress, Port);
            listener.Start();
            ServerRunning = true;

            //ServerEventsPublusher.listening.Raise("Listener", );
            // This loop is running asyncronously, it will await for new clients. The thread may do other things while awaiting.
            while (ServerRunning)
            {
                try
                {
                    // await for new clients. The thread may do other things while awaiting.
                    TcpClient tcpClient = await listener.AcceptTcpClientAsync(ListenToken);
                    // Low resource Easy access to IPAddress and port right away.
                    IPEndPoint remoteEnd = (IPEndPoint)tcpClient.Client.RemoteEndPoint;


                    // The remote end should cause us to use as little resources as possible until we trust it a little more.
                    // We can create a common handshake to ensure we are communicating with our application on the e

                    bool authorized = this.ClientSecurityCheck(tcpClient, remoteEnd);
                    if (!authorized)
                    {
                        try
                        {
                            Console.WriteLine($"Unauthorized connection attempt from {remoteEnd} @ {DateTime.Now}!");
                            tcpClient.Close();
                            tcpClient.Dispose();
                            //tcpClient = null;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error disposing of unauthorized client!: " + ex.ToString());
                        }
                        continue;
                    }


                    // Upgrade to our remoteClient wrapper.
                    RemoteHost remoteClient = new RemoteHost(tcpClient);

                    // Keep track of client (Triggers ObservableCollection update)
                    //ConnectedClientsLock = true;
                    //ConnectedClients.Add(remoteClient);
                    //ConnectedClientsLock = false;

                    Task WaitOrTimeoutTask = WaitForHandshake(remoteClient);
                    //Task HandleClientTask = HandleClientAsyncTask(remoteClient);
                    //await HandleClientAsyncTask(remoteClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected exception during client initialization: " + ex.Message);
                    //throw ex;
                }
            }
        }
        public void Stop()
        {
            this.ServerRunning = false;
            this.ListenTokenSource.Cancel();
            listener.Stop();
            foreach (RemoteHost client in ConnectedClients)
            {
                client.SendData("Server shutting down.");
                client.Close();
                client.Dispose();
            }
        }
        private bool ClientSecurityCheck(TcpClient remoteClient, IPEndPoint remoteEnd)
        {
            if (remoteClient == null)
                throw new ArgumentNullException(nameof(remoteClient));

            //Console.WriteLine(remoteEnd.Address);

            if (Blacklist.Count > 0)
            if (Blacklist.Any(item => item == remoteEnd.Address))
            {
                return false;
            }
            return true;
        }
        #region SSL Notes

        #endregion

        private async Task WaitForHandshake(RemoteHost remoteClient)
        {
            if (remoteClient.Reader == null)
                throw new ConnectionException("WaitForHandshake: ConnectionException - Null reader error.");
            // TODO
            //ClientEventsPublisher.connectPendingEvent.Raise(remoteClient);
            CancellationTokenSource readLineTokenSource = new CancellationTokenSource();
            CancellationToken readLineToken = readLineTokenSource.Token;
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
                //var task = remoteClient.Reader.ReadLineAsync().WaitAsync(timeout ,token).ConfigureAwait(false); // Requires .NET 6
                Task<string?> readerTask = remoteClient.Reader.ReadLineAsync().CancellableTask(readLineToken);
                //readerTask.Dis
                //remoteClient.Reader.ReadLineAsync()
                //Task<string> readerTask = remoteClient.Reader.ReadLineAsync().WaitAsync(timeout, token);
                //ConfiguredTaskAwaitable<string> ConfReadTask = ReadTask.ConfigureAwait(false);
                //var readerTask = remoteClient.Reader.ReadLineAsync();
                if (await Task.WhenAny(readerTask, Task.Delay(timeout)) == readerTask)
                {
                    #region WithinTimeoutBlock
                    // If the task was cancelled, this should send execution down to the finally block.
                    //readLineToken.ThrowIfCancellationRequested();
                    
                    bool handshakeFailed = false;

                    Console.WriteLine($"Received handshake from cient {remoteClient}");                                       

                    string digest = await remoteClient.GetClientDigest();

                    //Console.WriteLine($"{task.Result} == {digest}");

                    if (readerTask != null)
                    if (!readerTask.IsCompletedSuccessfully)
                    {
                        Console.WriteLine("Task failed to complete.");

                    }
                    else if (digest != readerTask.Result)
                    {
                        Console.WriteLine($"{readerTask.Result} != {digest}");
                        handshakeFailed = true;
                    }
                    else
                    {
                        //Console.WriteLine($"{readerTask.Result} == {digest}");
                        handshakeFailed = false;
                    }

                    if (ignoreHandshake)
                    {
                        handshakeFailed = false;
                    }

                    if (!handshakeFailed)
                    {
                        // Keep track of client (Triggers ObservableCollection update)
                        ConnectedClientsLock = true;
                        ConnectedClients.Add(remoteClient);
                        ConnectedClientsLock = false;

                        // A client has been accepted, we can start the asyncronous receive loop Task
                        

                        if (EnableSSL)
                        {
                            Task<SslStream?> sslUpgradeTask = remoteClient.SSLUpgradeAsServerAsync(this.ServerCertificate);
                            //Console.WriteLine("Created sslUpgradeTask");
                            Task sendDigestTask = remoteClient.SendDataAsync(digest);
                            //Console.WriteLine("Created sendDigestTask");
                            await sendDigestTask;
                            //Console.WriteLine("sendDigestTask finished");
                            // Failing here
                            SslStream? ssl = await sslUpgradeTask;
                            
                            //Console.WriteLine("sslUpgradeTask finished");
                            if (ssl != null)
                            {
                                //Console.WriteLine("SSL NOT NULL");

                                await remoteClient.SendDataAsync("SSL UPGRADED");
                                //await remoteClient._SSLWriter.WriteLineAsync("SSL UPGRADED");

                                await HandleClientAsyncTask(remoteClient);
                            }
                            else
                                Console.WriteLine("SSL Init FAILED");
                        }
                        else
                        {
                            await HandleClientAsyncTask(remoteClient);
                        }
                    }
                    else
                    {
                        // Drop Client event
                        if (remoteClient != null)
                        {
                            remoteClient.Close();
                            remoteClient.Dispose();
                        }
                        Console.WriteLine($"Invalid Handshake received from cient {remoteClient}");
                    }
                    #endregion
                }
                else
                {
                    Console.WriteLine($"Handshake timeout from cient {remoteClient}");
                    if (readLineTokenSource != null)
                        readLineTokenSource.Cancel();
                }
            }
            catch (OperationCanceledException) 
            {
                Console.WriteLine("Canceled await handshake task.");
            }
            catch (Win32Exception err)
            {
                Console.WriteLine("Caught Win32Exception in WaitForHandshake - Exception: " + err);
            }
            catch (IOException err)
            {
                Console.WriteLine("Caught IOException in WaitForHandshake - Exception: " + err);
            }
            catch (Exception err)
            {
                Console.WriteLine("Unhandled exception in WaitForHandshake: " + err);
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
            Console.WriteLine("WaitForHandshake completed without crashing");
        }
        private async Task HandleClientAsyncTask(RemoteHost remoteClient)
        {
            if (remoteClient.Reader == null)
                throw new ConnectionException("WaitForHandshake: ConnectionException - Null reader error.");
            // Signal a client has connected, passes the connected client to the event.
            ClientEventsPublisher.connectEvent.Raise(remoteClient);
            try
            {
                // This is the 'HandleClient loop', Its running in the task created by this.
                remoteClient.Reading = true;
                while (remoteClient.ClientSocket.Connected && remoteClient.Reading)
                {
                    // Waits here for data from the client. The thread will work elsewere until data is received.
                    //var request = await remoteClient.Reader.ReadLineAsync();

                    // Same as above but supports a cancellation token.
                    var request = await remoteClient.Reader.ReadLineAsync().WaitAsync(remoteClient.ReaderToken).ConfigureAwait(false); // Requires .NET 6
                    if (remoteClient.ReaderToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Read Canceled");
                        break;
                    }
                    
                    if (request != null)
                    {
                        string payload = request;
                        // Signal a client has sent some data, passes the connected client and the entire payload received
                        ClientEventsPublisher.receiveEvent.Raise(remoteClient, payload);
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
                    Console.WriteLine($"Unhandled IOException: {err}");
                    ClientEventsPublisher.exceptionEvent.Raise(remoteClient, err);
                    throw;
                }
                else if (InnerEx.GetType() != typeof(SocketException))
                {
                    if (InnerEx.GetType() != typeof(Win32Exception))
                    {
                        Console.WriteLine($"Unhandled Inner Exception for IOException: {InnerEx}");
                        ClientEventsPublisher.exceptionEvent.Raise(remoteClient, InnerEx);
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
                ClientEventsPublisher.exceptionEvent.Raise(remoteClient, err);
                throw;
            }
            finally
            {
                // Raise a disconnect event.
                ClientEventsPublisher.disconnectEvent.Raise(remoteClient);
                //Console.WriteLine($"Raised Disconnect Event");
                // Cleanup the handles and resources now that we are done with them.                

                // Remove the client from the connected clents list.
                ConnectedClientsLock = true;
                //Console.WriteLine($"Locked thread object");
                _ = ConnectedClients.Remove(remoteClient);
                //Console.WriteLine($"Removed remote client");
                ConnectedClientsLock = false;
                //Console.WriteLine($"Unlocked thread object");

                if (remoteClient != null)
                {
                    //Console.WriteLine($"remoteClient is not null");
                    remoteClient.Close();
                    remoteClient.Dispose();
                }
                //remoteClient = null;

                Console.WriteLine($"Client Disposed. Remaining clients: {ConnectedClients.Count}");


                // Memory Leak Testing

                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
            }
        Console.WriteLine($"HandleClientAsyncTask finished without crashing");

        }
    }
}
