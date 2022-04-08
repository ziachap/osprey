# Osprey

Osprey is a .NET library for messaging and streaming across distributed systems intended for use on local area networks. It is designed to require zero configuration and be fully decentralised, meaning a large number of distributed services can interact with eachother and handle failures without intervention. For example, suppose a client application is receiving data through a SignalR feed from a service. If the server fails, the client can automatically locate an equivalent service and re-establish the SignalR feed.

Features:
* Decentralized service discovery
* Automatic load balancing
* SignalR support
* Simple and lightweight

Here is a node called `osprey.server` on environment `production` setting itself up:
```
using (var osprey = new OspreyBuilder("osprey.server", "production").Build())
{
    osprey.Start();

    while (true)
    {
        Thread.Sleep(1000);
    }
}
```

Osprey can be extended to the internet or WAN using the `Osprey.Bridge` application which acts as a proxy, relaying osprey's UDP messages from one network to another using HTTP.
