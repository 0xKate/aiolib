using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aiolib
{
    public static class aioExtensions
    {
        public static void Clear(this byte[] array)
        {
            Array.Clear(array, 0, array.Length);
            array = null;
        }

        public static async Task<string> GetClientDigest(this RemoteClient client)
        {
            SHA1 sha1 = SHA1.Create();
            string digest = String.Empty;

            IPEndPoint remoteEnd = (IPEndPoint)client.ClientSocket.Client.RemoteEndPoint;
            IPEndPoint localEnd = (IPEndPoint)client.ClientSocket.Client.LocalEndPoint;

            byte[] localbytes = Encoding.UTF8.GetBytes(localEnd.ToString());
            byte[] remotebytes = Encoding.UTF8.GetBytes(remoteEnd.ToString());

            using (MemoryStream localstream = new MemoryStream(localbytes))
            {
                using (MemoryStream remotestream = new MemoryStream(remotebytes))
                {
                    byte[] localHash = await sha1.ComputeHashAsync(localstream);
                    byte[] remoteHash = await sha1.ComputeHashAsync(remotestream);

                    byte[] bothHash = new byte[localHash.Length + remoteHash.Length];

                    Buffer.BlockCopy(localHash, 0, bothHash, 0, localHash.Length);
                    Buffer.BlockCopy(remoteHash, 0, bothHash, localHash.Length, remoteHash.Length);

                    using (MemoryStream bothstream = new MemoryStream(bothHash))
                    {
                        byte[] hex = await sha1.ComputeHashAsync(bothstream);

                        localbytes.Clear();
                        remotebytes.Clear();
                        localHash.Clear();
                        remoteHash.Clear(); 
                        bothHash.Clear(); 
                        digest = Convert.ToHexString(hex);
                    }                    
                }
            }
            return digest;
        }
    }

    public class aioStreamServer
    {
        /// List to keep track of connected clients.
        /// Note: To obvserve this from WPF, while the server is in a background thread, use the following line of code from the observing thread.
        /// private object lockObject = new object(); // Note
        /// BindingOperations.EnableCollectionSynchronization(StreamServer.ConnectedClients, lockObject);
        public ObservableCollection<RemoteClient> ConnectedClients { get; }
        /// Used in-case outside insertions or deletions to the ConnectedClients need to be made. If so, please respect the lock.
        public bool ConnectedClientsLock = false;
        //public Authorization auth;
        /// <summary>
        /// Set to falseif you only want clients who follow our specific handshake to connect.
        /// </summary>
        public bool ignoreHandshake { get; set; }
        public List<IPAddress> Blacklist { get ;}
        public ClientEvents ClientEventsPublisher { get; }
        public ServerEvents ServerEventsPublusher { get; }
        public bool ServerRunning { get; set; }
        private int Port { get; }
        private IPAddress IpAddress { get; }
        private CancellationTokenSource ListenTokenSource { get; }
        private CancellationToken ListenToken { get; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public aioStreamServer(int listenPort, IPAddress listenAddress)//, string certificate_loc) // SSL
#pragma warning restore CS8618 // Complains that it must be Non-Nullable when i make it nullable, or complains that It must be Nullable when I make it Non-Nullable.
        {

            ignoreHandshake = true; 
            Blacklist = new List<IPAddress>();
            ConnectedClients = new ObservableCollection<RemoteClient>();
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

        public async Task Run()
        {
            //High level C# api for creating socket server.
            TcpListener listener = new TcpListener(IpAddress, Port);
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
                            tcpClient = null;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error disposing of unauthorized client!: " + ex.ToString());
                        }
                        continue;
                    }


                    // Upgrade to our remoteClient wrapper.
                    RemoteClient remoteClient = new RemoteClient(tcpClient);


                    // Keep track of client (Triggers ObservableCollection update)
                    //ConnectedClientsLock = true;
                    //ConnectedClients.Add(remoteClient);
                    //ConnectedClientsLock = false;

                    Task WaitOrTimeoutTask = WaitForHandshake(remoteClient);
                    //Task HandleClientTask = HandleClientAsyncTask(remoteClient);
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
            foreach (RemoteClient client in ConnectedClients)
            {
                client.SendData("Server shutting down.");
                client.Close();
                client.Dispose();
            }
        }


        private bool ClientSecurityCheck(TcpClient remoteClient, IPEndPoint remoteEnd)
        {
            if (remoteClient == null)
                throw new ArgumentNullException("RemoteClient is NULL!");

            //Console.WriteLine(remoteEnd.Address);

            if (Blacklist.Count > 0)
            if (Blacklist.Any(item => item == remoteEnd.Address))
            {
                return false;
            }
            return true;
        }

        //private async Task SSLInit()
        //{
            // SSL ToDO: Upgrade to SSL here. Implement another function between HandleClient and this loop to authenticate the client first via a handshake. Once we know we are talking
            // To our application and not something else then upgrade the network stream to SSL and handle the client.
            /*
            // https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?redirectedfrom=MSDN&view=net-6.0
            using (SslStream sslStream = new SslStream(networkStream, false))
            {
                try { }
                catch (AuthenticationException e)
                {
                }

                await sslStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);
                // Display the properties and settings for the authenticated stream.
                DisplaySecurityLevel(sslStream);
                DisplaySecurityServices(sslStream);
                DisplayCertificateInformation(sslStream);
                DisplayStreamProperties(sslStream);
            }
            */
            // END SSL
        //}

        private async Task WaitForHandshake(RemoteClient remoteClient)
        {   
            // TODO
            //ClientEventsPublisher.connectPendingEvent.Raise(remoteClient);
            try
            {
                CancellationTokenSource readLine = new CancellationTokenSource();
                CancellationToken token = readLine.Token;
                TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
                //var task = remoteClient.Reader.ReadLineAsync().WaitAsync(timeout ,token).ConfigureAwait(false); // Requires .NET 6
                var task = remoteClient.Reader.ReadLineAsync();
                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                {
                    // task completed within timeout
                    bool handshakeFailed = false;

                    Console.WriteLine($"Received handshake from cient {remoteClient}");                                       

                    string digest = await remoteClient.GetClientDigest();

                    Console.WriteLine($"{task.Result} == {digest}");

                    if (!task.IsCompletedSuccessfully)
                    {
                        Console.WriteLine("Task failed to complete.");

                    }
                    else if (digest != task.Result)
                    {
                        Console.WriteLine($"{task.Result} != {digest}");
                        Console.WriteLine($"Invalid Handshake received from cient {remoteClient}");
                        handshakeFailed = true;
                    }
                    else
                    {
                        Console.WriteLine($"{task.Result} == {digest}");
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
                        // The program will be context-switching between all tasks and coroutines.
                        await HandleClientAsyncTask(remoteClient);
                        // The task is created instantly, and this loop begins awaiting for a new client.
                    }
                    else
                    {
                        // Drop Client event
                        remoteClient.Close();
                        remoteClient.Dispose();
                        Console.WriteLine($"Invalid Handshake received from cient {remoteClient}");
                    }
                }
                else
                {
                    Console.WriteLine($"Handshake timeout from cient {remoteClient}");
                    readLine.Cancel();
                    try
                    {
                        remoteClient.Close();
                        remoteClient.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error disposing of unauthorized client!: " + ex.ToString());
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    remoteClient.Close();
                    remoteClient.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error disposing of unauthorized client!: " + ex.ToString());
                }
            }
        }

        private async Task HandleClientAsyncTask(RemoteClient remoteClient)
        {
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
            catch (IOException err)
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
                    Console.WriteLine($"Unhandled Inner Exception for IOException: {InnerEx}");
                    ClientEventsPublisher.exceptionEvent.Raise(remoteClient, InnerEx);
                    throw;
                }
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
                // Cleanup the handles and resources now that we are done with them.                

                // Remove the client from the connected clents list.
                ConnectedClientsLock = true;
                _ = ConnectedClients.Remove(remoteClient);
                ConnectedClientsLock = false;

                remoteClient.Close();
                remoteClient.Dispose();
                remoteClient = null;

                Console.WriteLine($"Client Disposed. Remaining clients: {ConnectedClients.Count}");


                // Memory Leak Testing

                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
            }
        }

        // SSL
        /*
        static X509Certificate serverCertificate = null;
        private void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
        }
        private void DisplaySecurityServices(SslStream stream)
        {
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);
        }
        private void DisplayStreamProperties(SslStream stream)
        {
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);
        }
        private void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }
        */
    }
}