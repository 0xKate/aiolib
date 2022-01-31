    /// -------- OLD CODE ------------

    /*
    public class ReceiveEventArgs : EventArgs
    {
        public TcpClient RemoteSocket { get; set; }
        public string Payload { get; set; }

        public ReceiveEventArgs(TcpClient remoteSocket, string payload)
        {
            this.RemoteSocket = remoteSocket;
            this.Payload = payload;
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

    public class aioStreamClient__OLD
    {
        public ReceiveEvent ReceiveEventPublisher;
        private bool SSLEnabled = true;
        private int port;
        private IPAddress ipAddress;
        private TcpClient remoteSocket;
        private NetworkStream networkStream;
        private string remoteEndPoint;
        private IPEndPoint localEndPoint;
        public aioStreamClient__OLD(int port, IPAddress ipAddress)
        {
            this.port = port;
            this.ipAddress = ipAddress;

            ReceiveEventPublisher = new ReceiveEvent();

            remoteSocket = new TcpClient(this.ipAddress.ToString(), this.port);
            remoteEndPoint = remoteSocket.Client.RemoteEndPoint.ToString();
            localEndPoint = (IPEndPoint)remoteSocket.Client.LocalEndPoint;
            networkStream = remoteSocket.GetStream();
        }
        public void Run()
        {
            if (remoteSocket.Connected)
            {
                Console.WriteLine("Connected to " + remoteEndPoint);
                Task ReceiveLoopTask = ReceiveLoopAsync();
                Task HandshakeTask = SendHandshake();
                Task SendDataTask = SendDataAsync("Hello from " + localEndPoint);
            }
        }

        public async Task SendHandshake()
        {
            RemoteHost remoteClient = new RemoteHost(remoteSocket);
            string digest = await remoteClient.GetClientDigest(false);
            Console.WriteLine(digest);
            await SendDataAsync(digest);
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
                        this.ReceiveEventPublisher.Raise(this.remoteSocket, request);
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
*/


        // Server awaits for TCP init
        // Client initiatates TCP Connection (SYN)
        // Server socket responds (SYN-ACK)
        // Client socket repsonds (ACK)

        // TcpSocket is now open

        // Server is awaiting for handshake (Does not notify client)
        // Client sends handshake
        // Client awaits final handshake within 5 seconds
        // Server verifies and responds if verified

        // Handshake has completed

        // Server awaits for SSL init
        // Client validates the server and has its callback check the certificate
        // Server does not validate the client but checks the SSL stream to validate security
        
        
        
        // This file is part of aiolib
// See https://github.com/0xKate/aiolib for more information
// Copyright (C) 0xKate <kate@0xkate.net>
// This program is published under a GPLv2 license
// https://github.com/0xKate/aiolib/blob/master/LICENSE


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace aiolib
{
    public class ServerEvents
    {
        /// <summary>
        /// Event executes when an instance of server has been generated.
        /// </summary>
        //public InitializedEvent initialized;
        /// <summary>
        /// Event executes when the server has entered the listen loop.
        /// </summary>
        public ListeningEvent listening;
        /// <summary>
        /// Executes when the server has begun shutting down or a shutdown was requested.
        /// </summary>
        //public ShuttingDownEvent shuttingDown;
        /// <summary>
        /// Executes when the server has finished shutting down completly.
        /// </summary>
        //public ShutdownCompleteEvent shutDownComplete;
        public ServerEvents()
        {
            //this.initialized = new InitializedEvent();
            this.listening = new ListeningEvent();
            //this.shuttingDown = new ShuttingDownEvent();
            //this.shutDownComplete = new ShutdownCompleteEvent();
        }

        public class BaseEventArgs : EventArgs
        {
            public string Message { get; set; }
            public object TcpListener { get; set; }
            public BaseEventArgs(string message, TcpListener additionalObject)
            {
                Message = message;
                TcpListener = additionalObject;
            }
        }

        public class SimpleEventArgs : EventArgs
        {
            public string Message { get; }
            public dynamic AdditionalObject { get; }
            public SimpleEventArgs(string Message, dynamic AdditionalObject=null)
            {
                this.Message = Message;
                this.AdditionalObject = AdditionalObject;
            }
        }

        public abstract class BaseEvent
        {

            public event EventHandler<BaseEventArgs> OnEvent;
            /// <summary>
            /// Raise an event and invoke all registered callbacks.
            /// </summary>
            /// <param name="server"></param>
            /// <exception cref="AggregateException"></exception>
            public void Raise(string message)
            {
                SimpleEventArgs eventArgs = new SimpleEventArgs(message);
                List<Exception> exceptions = new List<Exception>();
                foreach (Delegate handler in OnEvent.GetInvocationList())
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

        //public class InitializedEvent : BaseEvent { }
        public class ListeningEvent
        {
        
            public event EventHandler<BaseEventArgs> OnEvent;
            public void Raise(string message, TcpListener additionalObject)
            {
                BaseEventArgs eventArgs = new BaseEventArgs(message, additionalObject);
                List<Exception> exceptions = new List<Exception>();
                foreach (Delegate handler in OnEvent.GetInvocationList())
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
        //public class ShuttingDownEvent : BaseEvent { }
        //public class ShutdownCompleteEvent : BaseEvent { }
    }
}



// This file is part of aiolib
// See https://github.com/0xKate/aiolib for more information
// Copyright (C) 0xKate <kate@0xkate.net>
// This program is published under a GPLv2 license
// https://github.com/0xKate/aiolib/blob/master/LICENSE

#pragma warning disable CS8618 // The IDE is being really weird about this entire .cs file, it works, and also did not display errors in older versions of .NET
namespace aiolib
{
    // Not quite sure how to inherit this and overwrite the constructor with additional properties.
    public abstract class BaseEvent
    {
        public class Args : EventArgs
        {
            public RemoteHost Client { get; set; }
            public Args(RemoteHost client)
            {
                Client = client;
            }
        }
        public event EventHandler<Args> OnEvent;
        public void Raise(RemoteHost client)
        {
            Args eventArgs = new Args(client);
            List<Exception> exceptions = new List<Exception>();
            foreach (Delegate handler in OnEvent.GetInvocationList())
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

    public class Example
    {
        public GenericEvent genericEvent = new GenericEvent();
        public void HowToRaiseEvent()
        {

            var someObject = new Object();
            genericEvent.Raise("My Event Message", someObject);
        }
        public void EventCallback(object sender, SimpleEventArgs eventArgs)
        {
            Console.WriteLine($"Event message: {eventArgs.Message} received object: {eventArgs.AdditionalObject}");
        }
        public void HowToSubscribeToEvent()
        {
            genericEvent.OnEvent += EventCallback;
        }
    }

    public class SimpleEventArgs : EventArgs
    {
        public string Message { get; }
        public dynamic AdditionalObject { get; }
        public SimpleEventArgs(string Message, dynamic AdditionalObject = null)
        {
            this.Message = Message;
            this.AdditionalObject = AdditionalObject;
        }
    }
    public class GenericEvent
    {
        // Make sure the EventArgs type here also matches the one you created. The OnEvent variable may be renamed if you want.
        public event EventHandler<SimpleEventArgs> OnEvent;
        // Make sure the signature of the Raise() method matches the initializer of your EventArgs class.
        public void Raise(string message, dynamic additionalObject)
        {
            // Make sure the EventArgs type here is the one you created.
            SimpleEventArgs eventArgs = new SimpleEventArgs(message, additionalObject);
            // Code below here shouldnt change.
            List<Exception> exceptions = new List<Exception>();
            foreach (Delegate handler in OnEvent.GetInvocationList())
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




    public class ClientEvents
    {
        public ConnectEvent connectEvent;
        public DisconnectEvent disconnectEvent;
        public ReceiveEvent receiveEvent;
        public ExceptionEvent exceptionEvent;
        public ClientEvents()
        {
            this.connectEvent = new ConnectEvent();
            this.disconnectEvent = new DisconnectEvent();
            this.receiveEvent = new ReceiveEvent();
            this.exceptionEvent = new ExceptionEvent();
        }
        public class ConnectEvent
        {
            public class ConnectArgs : EventArgs
            {
                public RemoteHost Client { get; set; }

                public ConnectArgs(RemoteHost client)
                {
                    Client = client;
                }

            }

            public event EventHandler<ConnectArgs> OnConnect;
            public void Raise(RemoteHost client)
            {
                ConnectArgs eventArgs = new ConnectArgs(client);
                List<Exception> exceptions = new List<Exception>();
                foreach (Delegate handler in OnConnect.GetInvocationList())
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
        public class DisconnectEvent
        {
            public class DisconnectArgs : EventArgs
            {
                public RemoteHost Client { get; set; }

                public DisconnectArgs(RemoteHost client)
                {
                    Client = client;
                }

            }
            public event EventHandler<DisconnectArgs> OnDisconnect;
            public void Raise(RemoteHost client)
            {
                DisconnectArgs eventArgs = new DisconnectArgs(client);
                List<Exception> exceptions = new List<Exception>();
                foreach (Delegate handler in OnDisconnect.GetInvocationList())
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
        public class ReceiveEvent
        {
            public class ReceiveEventArgs : EventArgs
            {
                public RemoteHost Client { get; set; }
                public string Payload { get; set; }

                public ReceiveEventArgs(RemoteHost client, string payload)
                {
                    Client = client;
                    Payload = payload;
                }

            }

            public event EventHandler<ReceiveEventArgs> OnReceive = delegate { };

            public void Raise(RemoteHost client, string payload)
            {
                ReceiveEventArgs eventArgs = new ReceiveEventArgs(client, payload);
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
        public class ExceptionEvent
        {
            public class ExceptionEventArgs : EventArgs
            {
                public RemoteHost Client { get; set; }
                public Exception Error { get; set; }

                public ExceptionEventArgs(RemoteHost client, Exception exception)
                {
                    Client = client;
                    Error = exception;
                }

            }

            public event EventHandler<ExceptionEventArgs> OnException = delegate { };

            public void Raise(RemoteHost client, Exception exception)
            {
                ExceptionEventArgs eventArgs = new ExceptionEventArgs(client, exception);
                List<Exception> exceptions = new List<Exception>();
                foreach (Delegate handler in OnException.GetInvocationList())
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
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.



--------------------------------------------------


    // ---------------- OLD CODE -------------------



    /*

    public class aioStreamServer__OLD
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
        public bool ServerRunning { get; set; }
        private int Port { get; }
        private IPAddress IpAddress { get; }
        private CancellationTokenSource ListenTokenSource { get; }
        private CancellationToken ListenToken { get; }
        public X509Certificate ServerCertificate { get; private set; }

        private TcpListener listener;
        public aioStreamServer__OLD(int listenPort, IPAddress listenAddress)//, string certificate_loc) // SSL
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
            //ClientEventsPublisher = new ClientEvents();
            //ServerEventsPublusher = new ServerEvents();
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
            //Console.WriteLine("WaitForHandshake completed without crashing");
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
        //Console.WriteLine($"HandleClientAsyncTask finished without crashing");
        }
    }
}


*/



--------------------------------------
