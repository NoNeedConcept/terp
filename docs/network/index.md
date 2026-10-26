---
title: Network
description: Find a free local TCP port with PortFinder.
---

# Network

`Terp.Network` (package `Terp`) contains one helper, `PortFinder`.

```csharp
using Terp.Network;
```

## Find a free port

`PortFinder.FindFreeLocalPort()` binds a TCP socket to port 0 on all IPv4 interfaces, reads the port the operating system assigned, closes the socket and returns the port number.

```csharp
var port = PortFinder.FindFreeLocalPort();
var prefix = $"http://localhost:{port}/";
```

This is useful in tests that start a local server, as `src/Terp.Example` does with an `HttpListener`.

::: warning The port is free only for now
The socket is closed before the method returns. Another process can take the port before you bind it. Bind soon, and retry with a new port if binding fails.
:::

The method checks IPv4 only (`AddressFamily.InterNetwork`).
