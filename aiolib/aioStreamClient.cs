using System.Collections.ObjectModel;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;


namespace aiolib
{
    public class aioStreamClient
    {
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
        public ConnectionEvent TcpInitdEvent = new();
        public ConnectionEvent SslInitdEvent = new();
        public ConnectionEvent RecvEvent = new();
        public ConnectionEvent RecvWaitEvent = new();
        public ConnectionEvent SendEvent = new();
        public ConnectionEvent HandshakeInitdEvent = new();
        public ConnectionEvent ConnClosedEvent = new();
        public ConnectionEvent ConnErrorEvent = new();
        public ConnectionEvent ConnReadyEvent = new();
        //public ObservableCollection<Connection> Connections { get { return _Connections; } private set { _Connections = value; } }
        //internal ObservableCollection<Connection> _Connections = new ObservableCollection<Connection>();
        public bool EnableSSL { get; set; }
        public Connection? ServerConnection;
        internal int Port { get; }
        internal string HostName { get; }
        private bool _receiving = false;
        public aioStreamClient(int port, string hostname)
        {
            this.EnableSSL = true;
            this.Port = port;
            this.HostName = hostname;
        }
        public void Run()
        {
            Task RunningTask = this.RunAsync(this.HostName, this.Port);
        }
        public async Task RunAsync(string hostname, int port)
        {
            Connection? connection = await this.ConnectToHostAsync(hostname, port);
            if (connection != null && connection.IsConnected)
            {
                this.ServerConnection = connection;
                this.ConnReadyEvent.Raise(connection, $"Connection ready with host: {connection.RemoteEndPoint}");
                await this.ReceiveLoopAsync();
            }

        }
        internal async Task ReceiveLoopAsync()
        {
            if (this.ServerConnection == null)
            {
                Console.WriteLine("Connection Not initialized but tried to initilalze recive loop!");
                return;
            }

            if (!this._receiving)
            {
                this._receiving = true;
                try
                {
                    while (this._receiving)
                    {
                        this.RecvWaitEvent.Raise(this.ServerConnection, $"Waiting for incomming data from server {this.HostName}");
                        string? request = await this.ServerConnection.ReadLineAsync();
                        if (request != null)
                        {
                            this.RecvEvent.Raise(this.ServerConnection, request);
                        }
                        else
                        {
                            this.ConnClosedEvent.Raise(this.ServerConnection, $"Connection with server: {this.ServerConnection.RemoteEndPoint} - Closed by remote host.");
                            break; // Client closed connection
                        }
                    }
                    this.ServerConnection.Close();
                }
                catch (Exception ex)
                {
                    this.ConnErrorEvent.Raise(this.ServerConnection, ex.Message);
                    Console.WriteLine(ex.Message);
                    if (this.ServerConnection.IsConnected)
                        this.ServerConnection.Close();
                    this.ConnClosedEvent.Raise(this.ServerConnection, $"Connection with server: {this.ServerConnection.RemoteEndPoint} - Closed due to exception.");
                }
            }
        }
        public async Task SendDataAsync(string data)
        {
            if (this.ServerConnection != null)
            {
                await this.ServerConnection.SendDataAsync(data);
                this.SendEvent.Raise(this.ServerConnection, $"Sent data: {data} to host: {this.ServerConnection.RemoteEndPoint}");
            }
            else
                throw new ApplicationException("Tried to send data but connection was null!");
        }

        public async Task<Connection?> ConnectToHostAsync(string hostname, int port)
        {
            // (Layer 4) TCP Connection Initializing 
            Connection connection = new Connection(hostname, port);
            try
            {
                if (connection.IsConnected)
                {
                    // (Layer 4) TCP Connection Established                 
                    this.TcpInitdEvent.Raise(connection, $"Tcp connection initialized with host: {connection.RemoteEndPoint}");
                    // (Layer 7) Begin Handshake
                    string digest = await connection._Connection.GetClientDigest(serverSide: false);
                    Task SendTask = connection.SendDataAsync(digest);
                    Task<Tuple<bool, string?>> recvTask = connection.WaitForDataAsync(digest);
                    await SendTask;
                    Tuple<bool, string?> recv = await recvTask;
                    if (recv.Item1) // True if the data we got back was the digest
                    {
                        // (Layer 7) Handshake complete
                        this.HandshakeInitdEvent.Raise(connection, $"Handshake completed with host: {connection.RemoteEndPoint}");

                        // (Layer 5 & 6 Upgrade) SSL Initializing
                        if (this.EnableSSL)
                        {
                            SslStream? secureConnection = await connection._Connection.SSLUpgradeAsClientAsync(new IPHostEntry()
                            {
                                HostName = this.HostName,
                                AddressList = new IPAddress[] { connection._Connection.GetIPV4Address() }
                            });

                            if (secureConnection != null)
                            {
                                //this._Connections.Add(connection);
                                // (Layer 5 & 6 Upgrade) SSL Upgrade complete
                                this.SslInitdEvent.Raise(connection, $"SSL Initialized with host: {connection.RemoteEndPoint}");
                                return connection;
                            }
                            else // SSLUpgrade failed
                                this.ConnErrorEvent.Raise(connection, $"SSL Upgrade Error with host: {connection.RemoteEndPoint}");
                            return null;
                        }
                        else // Dont upgrade to SSL
                        {
                            return connection;
                        }
                    }
                    else // Received data other than handshake
                        this.ConnErrorEvent.Raise(connection, $"Handshake Error with host {connection.RemoteEndPoint}: Received data other than handshake during connection initialization.");
                    return null;
                }
                else // Failed to connect to host
                    this.ConnErrorEvent.Raise(connection, $"Failed to connect with host: {connection.RemoteEndPoint}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                this.ConnErrorEvent.Raise(connection, $"Exception with host: {connection.RemoteEndPoint} - Exception: {ex.Message}");
                connection.Close();
                connection.Dispose();
                this.ConnClosedEvent.Raise(connection, $"Connection with host: {connection.RemoteEndPoint} - Closed due to exception.");
                return null;
            }
        }
    }
}

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


