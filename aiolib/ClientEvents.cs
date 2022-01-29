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
