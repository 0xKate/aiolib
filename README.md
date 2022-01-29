# aiolib

aiolib is an event-driven asyncronous i/o library written in c# for .NET Core 6.0

See the samples for in-depth examples on how to utilize the library for now.

# Basic Usage

## Server
```csharp
  // Create an instance of the StreamServer  
  aioStreamServer server = new aioStreamServer(port, ipAddress);
  // Hook into the OnReceiveEvent and echo back what we received. (Simple echo server in 1 line)
  server.ClientEventsPublisher.receiveEvent.OnReceive += (sender, receiveArgs) => receiveArgs.Client.SendData(receiveArgs.Payload);
  // Run this without await to start the tasks and continue to the next line. Run with await in an asyncronous enviroment.
  server.Run();
```

## Client
```csharp
    // Create an instance of the StreamClient
    aioStreamClient client = new aioStreamClient(port, hostname);
    // Subscribe to the Connection Ready event and send a hello message
    client.ConnReadyEvent.OnEvent += (sender, eventArgs) => eventArgs.Conn.SendData($"Hello from {client.ServerConnection.LocalEndPoint}");
    // Subscribe to the receive event and print whatever we receive
    client.RecvEvent.OnEvent += (sender, eventArgs) => Console.WriteLine($"Received data {eventArgs.Message} from server {client.ServerConnection.RemoteEndPoint}");
    // Run this without await to start the tasks and continue to the next line. Run with await in an asyncronous enviroment.
    client.Run();
``

![image](https://user-images.githubusercontent.com/94711905/151676330-e1c4262e-8059-49b0-b5fb-7354a9a979e7.png)
