using aiolib;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS8618 // The IDE is being really weird about this entire .cs file, it works, and also did not display errors in older versions of .NET
namespace aiolib
{
    // Not quite sure how to inherit this and overwrite the constructor with additional properties.
    public abstract class BaseEvent
    {
        public class Args : EventArgs
        {
            public RemoteClient Client { get; set; }
            public Args(RemoteClient client)
            {
                Client = client;
            }
        }
        public event EventHandler<Args> OnEvent;
        public void Raise(RemoteClient client)
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
                public RemoteClient Client { get; set; }

                public ConnectArgs(RemoteClient client)
                {
                    Client = client;
                }

            }

            public event EventHandler<ConnectArgs> OnConnect;
            public void Raise(RemoteClient client)
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
                public RemoteClient Client { get; set; }

                public DisconnectArgs(RemoteClient client)
                {
                    Client = client;
                }

            }
            public event EventHandler<DisconnectArgs> OnDisconnect;
            public void Raise(RemoteClient client)
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
                public RemoteClient Client { get; set; }
                public string Payload { get; set; }

                public ReceiveEventArgs(RemoteClient client, string payload)
                {
                    Client = client;
                    Payload = payload;
                }

            }

            public event EventHandler<ReceiveEventArgs> OnReceive = delegate { };

            public void Raise(RemoteClient client, string payload)
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
                public RemoteClient Client { get; set; }
                public Exception Error { get; set; }

                public ExceptionEventArgs(RemoteClient client, Exception exception)
                {
                    Client = client;
                    Error = exception;
                }

            }

            public event EventHandler<ExceptionEventArgs> OnException = delegate { };

            public void Raise(RemoteClient client, Exception exception)
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
