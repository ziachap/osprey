using System;
using System.Threading;

namespace Osprey.Demo.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========== OSPREY SERVER ==========");
            
            using (var osprey = new OspreyBuilder("osprey.server", "acceptance").Build())
            {
                osprey.Start();

                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
