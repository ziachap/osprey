using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Osprey.Http;
using Osprey.ZeroMQ;

namespace Osprey.Demo.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========== OSPREY SERVER ==========");
            
            using (Osprey.Join("osprey.server", "acceptance"))
            using (var zmq = new ZeroMQServer("zmq1"))
            {
                var topics = new HashSet<string>();
                zmq.OnSubscribe += topic =>
                {
                    if (topics.Contains(topic)) return;
                
                    Task.Run(() =>
                    {
                        var rnd = new Random();
                        while (true)
                        {
                            try
                            {
                                var id = Osprey.Network.Node.Info.Id;
                                var data = new TestData()
                                {
                                    Data1 = id + " | " + topic + " = " + rnd.Next(1, 999)
                                };
                
                                zmq.Publish(topic, data);
                
                                Thread.Sleep(500);
                            }
                            catch (Exception ex)
                            {
                
                            }
                        }
                    });
                
                    topics.Add(topic);
                };

                //Task.Run(() =>
                //{
                //    while (true)
                //    {
                //        var data = new TestData()
                //        {
                //            Data1 = "_234_"
                //        };
                //
                //        zmq.Publish("A", data);
                //
                //        Thread.Sleep(50);
                //    }
                //});

                /*
                StartSendingHttp();

                Task.Run(() =>
                {
                    while (true)
                    {
                        tcp.Broadcast(Guid.NewGuid().ToString());
                        Thread.Sleep(100);
                    }
                });
                */
                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private static void StartSendingHttp()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        var task = Osprey.Network.Locate("osprey.client")
                            .Http()
                            .Send(new HttpMessage<string, string>
                            {
                                Endpoint = "test",
                                Payload = "apples",
                                RequestType = HttpVerb.GET
                            });

                        var response = await task;
                        
                        Console.WriteLine("RESPONSE: " + response);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    Thread.Sleep(4000);
                }
            }, TaskCreationOptions.LongRunning);
        }
    }

    internal class TestData
    {
        public string Data1 { get; set; }
    }
}
