using System;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Osprey.Http;
using Osprey.ZeroMQ;

namespace Osprey.Demo.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========== OSPREY CLIENT ==========");
            using (Osprey.Join("osprey.client", "acceptance"))
            using (new HttpServer<DefaultStartup<DefaultNancyBootstrapper>>("http"))
            using (var client = new ZeroMQClient("osprey.server", "zmq1"))
            {

                client.OnDisconnected += () => Console.WriteLine("CLIENT IS DISCONNECTED");

                client.OnConnected += () => Console.WriteLine("CLIENT IS CONNECTED");

                client.OnConnected += () =>
                {
                    client.Subscribe("C");
                };

                client.On<TestData>("C", x =>
                {
                    Interlocked.Increment(ref _count);
                    Console.WriteLine(x.Data1);
                });

                client.Connect();

                Task.Run(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        var count = _count;
                        Interlocked.Exchange(ref _count, 0);

                        Console.WriteLine($"Received {count} updates");
                    }
                });

                Console.ReadKey();

                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private static int _count = 0;
    }

    internal class TestData
    {
        public string Data1 { get; set; }
    }
}
