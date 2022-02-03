// This file is part of aiolib
// See https://github.com/0xKate/aiolib for more information
// Copyright (C) 0xKate <kate@0xkate.net>
// This program is published under a GPLv2 license
// https://github.com/0xKate/aiolib/blob/master/LICENSE

using System.Net;
using System.Net.Security;

namespace aiolib
{
    public class aioStreamClient
    {   
        /*
        public ConnectionEvent TcpInitdEvent = new();
        public ConnectionEvent SslInitdEvent = new();
        public ConnectionEvent RecvEvent = new();
        public ConnectionEvent RecvWaitEvent = new();
        public ConnectionEvent SendEvent = new();
        public ConnectionEvent HandshakeInitdEvent = new();
        public ConnectionEvent ConnClosedEvent = new();
        public ConnectionEvent ConnErrorEvent = new();
        public ConnectionEvent ConnReadyEvent = new();
        */
        public aioEvents Events;
        public bool EnableSSL { get; set; }
        public Connection? ServerConnection;
        internal int Port { get; }
        internal string HostName { get; }
        private bool _receiving = false;
        public aioStreamClient(int port, string hostname)
        {
            this.Events = new();
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

                connection.SendEvent.OnEvent += (sender, eventArgs) => Events.SendEvent.Raise(eventArgs.Connection, eventArgs.Message);
                
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
                    Events.ConnectionReadyEvent.Raise(this.ServerConnection, $"Connection ready for use with: {this.ServerConnection.RemoteEndPoint}");
                    while (this._receiving)
                    {

                        Events.AwaitReceiveEvent.Raise(this.ServerConnection, $"Waiting for incomming data from server {this.HostName}");
                        string? request = await this.ServerConnection.ReadLineAsync();
                        if (request != null)
                        {

                            Events.ReceiveEvent.Raise(this.ServerConnection, request);
                        }
                        else
                        {
                            Events.ConnectionClosedEvent.Raise(this.ServerConnection, $"Connection with server: {this.ServerConnection.RemoteEndPoint} - Closed by remote host.");
                            break; // Client closed connection
                        }
                    }
                    this.ServerConnection.Close();
                }
                catch (Exception ex)
                {
                    Events.ConnectionExceptionEvent.Raise(this.ServerConnection, ex.Message);
                    Console.WriteLine(ex.Message);
                    if (this.ServerConnection.IsConnected)
                        this.ServerConnection.Close();
                    Events.ConnectionClosedEvent.Raise(this.ServerConnection, $"Connection with server: {this.ServerConnection.RemoteEndPoint} - Closed due to exception.");
                }
            }
        }
        public async Task SendDataAsync(string data)
        {
            if (this.ServerConnection != null)
            {
                await this.ServerConnection.SendDataAsync(data);
                Events.SendEvent.Raise(this.ServerConnection, $"Sent data: {data} to host: {this.ServerConnection.RemoteEndPoint}");
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
                    //Console.WriteLine("Connecting to host: " + HostName);
                    // (Layer 4) TCP Connection Established
                    Events.TcpAcceptEvent.Raise(connection._Connection.ClientSocket, $"Tcp connection initialized with host: {connection.RemoteEndPoint}");
                    // (Layer 7) Begin Handshake
                    string digest = await connection._Connection.GetClientDigest(serverSide: false);
                    Events.HandshakeBeginEvent.Raise(connection, $"Handshake starting with server: {connection.RemoteEndPoint}");
                    Task SendTask = connection.SendDataAsync(digest); // Process the send data in a new task
                    Task<Tuple<bool, string?>> recvTask = connection.WaitForDataAsync(digest); // Wait for the handshake in a new task
                    await SendTask; // Wait for the send to complete in this task now
                    Events.HandshakeReceiveEvent.Raise(connection, $"Waitng for server to confirm handshake: {digest}");
                    Tuple<bool, string?> recv = await recvTask; // Finish waiting in this task now, use the result
                    if (recv.Item1) // True if the data we got back was the digest
                    {
                        // (Layer 7) Handshake complete
                        Events.HandshakeCompleteEvent.Raise(connection, $"Handshake completed with server: {connection.RemoteEndPoint}");

                        // (Layer 5 & 6 Upgrade) SSL Initializing
                        if (this.EnableSSL)
                        {
                            IPAddress? ip = connection._Connection.GetIPV4Address();
                            if (ip == null)
                                throw new ConnectionException("ConnectToHostAsync: ConnectionException - Null IP Exception");

                            SslStream? secureConnection = await connection._Connection.SSLUpgradeAsClientAsync(new IPHostEntry()
                            {
                                HostName = this.HostName,
                                AddressList = new IPAddress[] { ip }
                            });

                            if (secureConnection != null)
                            {   // (Layer 5 & 6 Upgrade) SSL Upgrade complete
                                Events.SSLReadyEvent.Raise(connection, $"SSL Initialized with host: {connection.RemoteEndPoint}");
                                return connection;
                            }
                            else // SSLUpgrade failed
                                Events.ConnectionExceptionEvent.Raise(connection, $"SSL Upgrade Error with host: {connection.RemoteEndPoint}");
                            return null;
                        }
                        else // Dont upgrade to SSL
                            return connection;
                    }
                    else // Received data other than handshake
                        Events.HandshakeFailedEvent.Raise(connection, $"Handshake Error with host {connection.RemoteEndPoint}: Received data other than handshake during connection initialization.");
                    return null;
                }
                else // Failed to connect to host
                    Events.ConnectionExceptionEvent.Raise(connection, $"Failed to connect with host: {connection.RemoteEndPoint}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Events.ConnectionExceptionEvent.Raise(connection, $"Exception with host: {connection.RemoteEndPoint} - Exception: {ex.Message}");
                connection.Close();
                connection.Dispose();
                Events.ConnectionClosedEvent.Raise(connection, $"Connection with host: {connection.RemoteEndPoint} - Closed due to exception.");
                return null;
            }
        }
    }
}

