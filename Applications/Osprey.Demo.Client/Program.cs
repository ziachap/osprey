using System;
using System.Threading;
using Osprey.Builder;

namespace Osprey.Demo.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========== OSPREY CLIENT ==========");
            using (var osprey = new OspreyBuilder("osprey.client", "acceptance").Build())
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
