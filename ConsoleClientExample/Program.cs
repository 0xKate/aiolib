using System;
using System.Net;
using System.Threading.Tasks;
using aiolib;

namespace StreamClientAsync
{
    class ConsoleApp
    {
        static void Main(string[] args)
        {
            try
            {
                int port = 50000;
                IPAddress ipAddress = IPAddress.Parse("10.0.0.10");
                bool running = true;

                aioStreamClient client = new aioStreamClient(port, ipAddress);
                client.Run();

                while (running)
                {
                    Console.Write("Enter Message: ");
                    var userInput = Console.ReadLine();

                    if (userInput == "Exit")
                    {
                        running = false;
                        break;
                    }
                    else
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        Task SendDataTask = client.SendDataAsync(userInput);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                }
                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
