using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace aiolib
{
    public class aioStreamServer
    {
        /// List to keep track of connected clients.
        /// Note: To obvserve this from WPF, while the server is in a background thread, use the following line of code from the observing thread.
        /// private object lockObject = new object(); // Note
        /// BindingOperations.EnableCollectionSynchronization(StreamServer.ConnectedClients, lockObject);
        public ObservableCollection<RemoteClient> ConnectedClients { get; }
        /// Used in-case outside insertions or deletions to the ConnectedClients need to be made. If so, please respect the lock.
        public bool ConnectedClientsLock = false;
        public ClientEvents ClientEventsPublisher { get; }
        public ServerEvents ServerEventsPublusher { get; }
        public bool ServerRunning { get; set; }
        private int Port { get; }
        private IPAddress IpAddress { get; }
        //private RemoteClient LastClient { get; set; }
        private CancellationTokenSource ListenTokenSource { get; }
        private CancellationToken ListenToken { get; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public aioStreamServer(int listenPort, IPAddress listenAddress)//, string certificate_loc) // SSL
#pragma warning restore CS8618 // Complains that it must be Non-Nullable when i make it nullable, or complains that It must be Nullable when I make it Non-Nullable.
        {
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

        // Coroutine
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
                    // Upgrade to our remoteClient wrapper.
                    RemoteClient remoteClient = new RemoteClient(tcpClient);
                    //LastClient = remoteClient;

                    ConnectedClientsLock = true;
                    ConnectedClients.Add(remoteClient);
                    ConnectedClientsLock = false;

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

                    // A client has connected, start an asyncronous Task
                    // The program will be context-switching between all tasks and coroutines.
                    Task HandleClientTask = HandleClientAsyncTask(remoteClient);
                    // The task is created instantly, and this loop begins awaiting for a new client.
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

                Console.WriteLine($"Number of Clients: {ConnectedClients.Count}");

                remoteClient.Close();
                remoteClient.Dispose();
                remoteClient = null;

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