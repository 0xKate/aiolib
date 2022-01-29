// This file is part of aiolib
// See https://github.com/0xKate/aiolib for more information
// Copyright (C) 0xKate <kate@0xkate.net>
// This program is published under a GPLv2 license
// https://github.com/0xKate/aiolib/blob/master/LICENSE

using System.Net;
using System.Net.Sockets;


namespace aiolib
{
    public interface IConnectionEvent
    {
        public event EventHandler<ConnectionEventArgs> OnEvent;
    }
    public class ConnectionEventArgs : EventArgs
    {
        public Connection? Conn;
        public String? Message;
        public ConnectionEventArgs(Connection? connection, string? message = null)
        {
            this.Conn = connection;
            this.Message = message;
        }
    }
    public class ConnectionEvent : IConnectionEvent
    {
        public event EventHandler<ConnectionEventArgs> OnEvent;
        public void Raise(Connection? connection, string? message = null)
        {
            // Do something here before the event…  

            OnEventRaised(new ConnectionEventArgs(connection, message));

            // or do something here after the event.
        }
        protected virtual void OnEventRaised(ConnectionEventArgs e)
        {
            OnEvent?.Invoke(this, e);
        }
    }

    public class Connection : IDisposable
    {
        public bool EnableSSL { get; }
        public IPEndPoint RemoteEndPoint { get { return _Connection.RemoteEndPoint; } }
        public IPEndPoint? LocalEndPoint { get { return (IPEndPoint?)_Connection.ClientSocket.Client.LocalEndPoint; } }
        public bool IsConnected { get { return this._Connection.ClientSocket.Connected; } }
        internal RemoteHost _Connection;
        private bool _closed;
        private bool _disposed;
        public bool _blockSend = false;
        public bool BlockSend { get { return _blockSend; } set { _blockSend = false; } }
        public override string ToString()
        {
            return this.RemoteEndPoint.ToString();
        }
        public Connection(string hostname, int port)
        {
            _Connection = new RemoteHost(new TcpClient(hostname, port));
        }
        public Connection(RemoteHost connection)
        {
            _Connection = connection;
        }
        public Connection(TcpClient tcpClient)
        {
            this._Connection = new RemoteHost(tcpClient);
        }
        public Connection(TcpClient tcpClient, IPHostEntry hostname)
        {
            EnableSSL = true;
            if (hostname == null)
                throw new ArgumentNullException("SSL Requires the hostname of the remote machine.");
            this._Connection = new RemoteHost(tcpClient, hostname);
        }
        public async Task<Tuple<bool, string?>> WaitForDataAsync(string expectedData, int timeout = 0, CancellationToken? token = null)
        {
            string? receivedData = null;
            if (timeout == 0)
            {
                receivedData = await this._Connection.Reader.ReadLineAsync();
            }
            else
            {
                TimeSpan timespan = TimeSpan.FromMilliseconds(timeout);
                Task<string?> readerTask = this._Connection.Reader.ReadLineAsync();
                if (await Task.WhenAny(readerTask, Task.Delay(timespan)) == readerTask)
                {
                    // Reader Got Data within 5s
                    receivedData = readerTask.Result;
                }
            }
            if (receivedData == expectedData)
                return new Tuple<bool, string?>(true, receivedData);
            else
                return new Tuple<bool, string?>(false, receivedData);
        }
        public async Task<string?> ReadLineAsync()
        {
            return await this._Connection.Reader.ReadLineAsync();
        }
        public async Task SendDataAsync(string data)
        {
            if (!_blockSend)
                await this._Connection.SendDataAsync(data);
        }
        public void SendData(string data)
        {
            if (!_blockSend)
                this._Connection.SendData(data);
        }
        public void Close()
        {
            if (!this._closed)
                this._Connection.Close();
            this._closed = true;
        }
        public void Dispose()
        {
            if (!this._disposed)
                this._Connection.Dispose();
            this._disposed = true;
        }
    }
}
