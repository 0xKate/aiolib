using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace aiolib
{
    public class aioEvents
    {
        // Register Event Publishers
        // Only allow publicly subscribing

        // -- OnTcpAccept --
        internal GenericEvent<TcpClient> TcpAcceptEvent = new();
        public event EventHandler<GenericEventArgs<TcpClient>> OnTcpAccept
        {
            add { TcpAcceptEvent.OnEvent += value; }
            remove { TcpAcceptEvent.OnEvent -= value; }
        }

        // -- OnUnauthorizedConnection --
        internal GenericEvent<TcpClient> UnauthorizedConnectionEvent = new();
        public event EventHandler<GenericEventArgs<TcpClient>> OnUnauthorizedConnection
        {
            add { UnauthorizedConnectionEvent.OnEvent += value; }
            remove { UnauthorizedConnectionEvent.OnEvent -= value; }
        }

        // OnSSLReady
        internal ConnectionEvent SSLReadyEvent = new();
        public event EventHandler<ConnectionEventArgs> OnSSLReady
        {
            add { SSLReadyEvent.OnEvent += value; }
            remove { SSLReadyEvent.OnEvent -= value; }
        }

        // OnSSLFail
        internal ConnectionEvent SSLFailEvent = new();
        public event EventHandler<ConnectionEventArgs> OnSSLFail
        {
            add { SSLFailEvent.OnEvent += value; }
            remove { SSLFailEvent.OnEvent -= value; }
        }

        // OnSSLFail
        internal ConnectionEvent SSLBeginEvent = new();
        public event EventHandler<ConnectionEventArgs> OnSSLBegin
        {
            add { SSLBeginEvent.OnEvent += value; }
            remove { SSLBeginEvent.OnEvent -= value; }
        }

        // OnRecieve
        internal ConnectionEvent ReceiveEvent = new();
        public event EventHandler<ConnectionEventArgs> OnReceive
        {
            add { ReceiveEvent.OnEvent += value; }
            remove { ReceiveEvent.OnEvent -= value; }
        }

        // OnAwaitRecieve
        internal ConnectionEvent AwaitReceiveEvent = new();
        public event EventHandler<ConnectionEventArgs> OnAwaitRecieve
        {
            add { AwaitReceiveEvent.OnEvent += value; }
            remove { AwaitReceiveEvent.OnEvent -= value; }
        }

        // OnSend
        internal ConnectionEvent SendEvent = new();
        public event EventHandler<ConnectionEventArgs> OnSend
        {
            add { SendEvent.OnEvent += value; }
            remove { SendEvent.OnEvent -= value; }
        }

        // OnHandshakeBegin
        internal ConnectionEvent HandshakeBeginEvent = new();
        public event EventHandler<ConnectionEventArgs> OnHandshakeBegin
        {
            add { HandshakeBeginEvent.OnEvent += value; }
            remove { HandshakeBeginEvent.OnEvent -= value; }
        }

        // OnHandshakeWait
        internal ConnectionEvent HandshakeWaitEvent = new();
        public event EventHandler<ConnectionEventArgs> OnHandshakeWait
        {
            add { HandshakeWaitEvent.OnEvent += value; }
            remove { HandshakeWaitEvent.OnEvent -= value; }
        }

        // OnHandshakeReceived
        internal ConnectionEvent HandshakeReceiveEvent = new();
        public event EventHandler<ConnectionEventArgs> OnHandshakeReceived
        {
            add { HandshakeReceiveEvent.OnEvent += value; }
            remove { HandshakeReceiveEvent.OnEvent -= value; }
        }

        // OnHandshakeComplete
        internal ConnectionEvent HandshakeCompleteEvent = new();
        public event EventHandler<ConnectionEventArgs> OnHandshakeComplete
        {
            add { HandshakeCompleteEvent.OnEvent += value; }
            remove { HandshakeCompleteEvent.OnEvent -= value; }
        }

        // OnHandshakeFailed
        internal ConnectionEvent HandshakeFailedEvent = new();
        public event EventHandler<ConnectionEventArgs> OnHandshakeFailed
        {
            add { HandshakeFailedEvent.OnEvent += value; }
            remove { HandshakeFailedEvent.OnEvent -= value; }
        }

        // OnConnectionClosed
        internal ConnectionEvent ConnectionClosedEvent = new();
        public event EventHandler<ConnectionEventArgs> OnConnectionClosed
        {
            add { ConnectionClosedEvent.OnEvent += value; }
            remove { ConnectionClosedEvent.OnEvent -= value; }
        }

        // OnConnectionReady
        internal ConnectionEvent ConnectionReadyEvent = new();
        public event EventHandler<ConnectionEventArgs> OnConnectionReady
        {
            add { ConnectionReadyEvent.OnEvent += value; }
            remove { ConnectionReadyEvent.OnEvent -= value; }
        }

        // OnListenReady
        internal ServerEvent AcceptLoopReadyEvent = new();
        public event EventHandler<ServerEventArgs> OnListenReady
        {
            add { AcceptLoopReadyEvent.OnEvent += value; }
            remove { AcceptLoopReadyEvent.OnEvent -= value; }
        }

        // OnListenEnd
        internal ServerEvent AcceptLoopEndEvent = new();
        public event EventHandler<ServerEventArgs> OnListenEnd
        {
            add { AcceptLoopEndEvent.OnEvent += value; }
            remove { AcceptLoopEndEvent.OnEvent -= value; }
        }

        // OnAwaitAccept
        internal ServerEvent AwaitAcceptEvent = new();
        public event EventHandler<ServerEventArgs> OnAwaitAccept
        {
            add { AwaitAcceptEvent.OnEvent += value; }
            remove { AwaitAcceptEvent.OnEvent -= value; }
        }

        // OnVerboseException
        internal ServerEvent VerboseExceptionEvent = new();
        public event EventHandler<ServerEventArgs> OnVerboseException
        {
            add { VerboseExceptionEvent.OnEvent += value; }
            remove { VerboseExceptionEvent.OnEvent -= value; }
        }

        //OnException
        internal ServerEvent ExceptionEvent = new();
        public event EventHandler<ServerEventArgs> OnException
        {
            add { ExceptionEvent.OnEvent += value; }
            remove { ExceptionEvent.OnEvent -= value; }
        }

        // OnVerboseConnectionException
        internal ConnectionEvent VerboseConnectionExceptionEvent = new();
        public event EventHandler<ConnectionEventArgs> OnVerboseConnectionException
        {
            add { VerboseConnectionExceptionEvent.OnEvent += value; }
            remove { VerboseConnectionExceptionEvent.OnEvent -= value; }
        }

        // OnConnectionException
        internal ConnectionEvent ConnectionExceptionEvent = new();
        public event EventHandler<ConnectionEventArgs> OnConnectionException
        {
            add { ConnectionExceptionEvent.OnEvent += value; }
            remove { ConnectionExceptionEvent.OnEvent -= value; }
        }

    }
}
