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
