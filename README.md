# Osprey

Osprey is a decentralized service discovery library that seeks to solve challenges around building flexible distributed systems with ephemeral services:

- Can be run with zero configuration.
- Dynamically discovers new services added to and removed from the network.
- No need for a centralized store of available services.
- Allows for distributed systems to be built using built-in load balancing.
- Supports SignalR
- Simple and lightweight

Here is a node called `osprey.server` on environment `production` setting itself up:
```
using (var osprey = new OspreyBuilder("my.server", "production").Build())
{
    osprey.Start();

    while (true)
    {
        Thread.Sleep(1000);
    }
}
```

## Osprey.Dashboard
Osprey Dashboard provides a convenient way to view what services are running in the network and provides alerts when the system is not in a desirable state.

## Osprey.Bridge
Osprey can be extended over the internet or a WAN using the `Osprey.Bridge` application which acts as a proxy, relaying osprey's UDP messages from one network to another using HTTP.

## Osprey.Portal
Osprey based services can be accessed through a web browser using the `Osprey.Portal` server application.
