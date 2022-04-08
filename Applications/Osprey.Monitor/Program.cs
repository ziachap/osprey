using System;
using System.Linq;
using System.Threading;
using Fclp;
using Osprey.Http;
using Osprey.Utilities;

namespace Osprey.Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var osprey = new OspreyBuilder("osprey.monitor", "production").Build();
            
            using (osprey.Run())
            {
                while (true)
                {
                    //Console.Clear();
                    Console.WriteLine("========== OSPREY MONITOR ==========");
                    Console.WriteLine("");
                    PrintDiscovered(osprey);

                    Thread.Sleep(4000);
                }
            }
        }

        private static void PrintDiscovered(IOsprey osprey)
        {
            var active = osprey.LocateAll()
                .GroupBy(x => x.Environment)
                .OrderBy(x => x.Key)
                .ToList();
            Console.WriteLine($"-- Active [{active.Count}] --");
            foreach (var group in active)
            {
                Console.WriteLine($"{group.Key}");
                foreach (var node in group)
                {
                    Console.WriteLine($"  {node.Id} | {node.Name} | {node.Ip}");
                    foreach (var service in node.Services)
                    {
                        switch (service.Type)
                        {
                            case "http":
                                Console.WriteLine($"    HTTP [{service.Name}] ({service.Address})");
                                break;
                            case "zmq":
                                Console.WriteLine($"    ZMQ [{service.Name}] ({service.Address})");
                                break;
                        }
                    }
                }
            }
            
        }
    }
}
