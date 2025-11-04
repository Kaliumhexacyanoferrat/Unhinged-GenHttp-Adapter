[![NuGet](https://img.shields.io/nuget/v/Unhinged.GenHttp.Experimental.svg)](https://www.nuget.org/packages/Unhinged.GenHttp.Experimental/)

# Unhinged-GenHttp-Adapter

Note: This project is still under development and some basic features are not available yet.

Undergoing task: Support requests with content (either known Content-Length or Transfer-Encoding: chunked)

# What is Unhinged?

Unhinged is an experimental, ultra-high-performance HTTP/1.1 server built from the ground up to bypass every layer of overhead in the .NET networking stack. Unlike all existing C# web frameworks — including Kestrel, ASP.NET, and other custom servers — Unhinged does not use System.Net.Sockets or SocketAsyncEventArgs. Instead, it interfaces directly with Linux system calls like epoll_wait, accept4, and eventfd through P/Invoke, giving it complete control over connection multiplexing, scheduling, and I/O readiness. This design eliminates the managed socket layer, thread pool dispatch, and kernel transition overhead typical of conventional .NET servers. Running under Native AOT with pinned, cache-aligned buffers and zero GC allocations, Unhinged achieves deterministic performance at massive concurrency levels — targeting tens of millions of requests per second in TechEmpower-style benchmarks. It’s not just a web server — it’s a proof that .NET can operate at the same level as hand-optimized C or Rust servers when freed from its abstractions.

# 🔌 Unhinged + GenHTTP Integration

A dedicated GenHTTP adapter allows Unhinged to serve as the socket engine for the GenHTTP framework.
In this setup, Unhinged handles all low-level network operations — including connection acceptance, epoll-based readiness events, and zero-copy I/O — while GenHTTP operates purely at the application layer, managing routing, middleware, and content generation.

This architecture cleanly separates network I/O performance from HTTP framework logic: Unhinged delivers the raw throughput and event-driven efficiency, while GenHTTP provides the developer-friendly, feature-rich API surface.
The result is a hybrid model that combines C-level socket performance with high-level .NET ergonomics, bridging the gap between bare-metal speed and modern web development.

# ⚙️ Intelligent Resource Control

Unhinged isn’t just fast — it’s aware of your hardware limits.
Unlike traditional servers that rely on external middleware or reverse proxies for traffic control, Unhinged includes rate limiting and server throttling mechanisms baked directly into its engine.

These controls operate at the epoll event loop level, allowing precise regulation of connection intake, read/write frequency, and CPU utilization.
By enforcing backpressure before requests ever reach user code, Unhinged can maintain stable latency, prevent resource saturation, and deliver fine-grained control over how system resources are consumed — ensuring consistent performance even under extreme load.

### 🧩 Why This Isn’t Possible in ASP.NET Core

In traditional .NET servers like ASP.NET Core or Kestrel, the network layer is abstracted behind the managed System.Net.Sockets stack and the ThreadPool scheduler.
Because of this, all incoming connections and I/O events are processed after the runtime has already accepted sockets and queued work items — meaning the application never has a chance to intercept or shape system-level behavior.

As a result, features such as hardware-aware throttling, connection-level rate limiting, or adaptive CPU backpressure cannot be implemented precisely.
Any rate limiting must occur after requests are parsed and dispatched, wasting resources on sockets, buffers, and worker threads that have already been allocated.

Unhinged, by contrast, runs below the managed runtime boundary, owning the entire socket lifecycle. It can pause accept4 calls, suppress read events in epoll, or delay writes in response to CPU or memory pressure — capabilities that simply don’t exist when the networking layer is buried inside the .NET runtime.

# 🚀 Quickstart
```cs
internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = UnhingedEngine
            .CreateBuilder()
            .SetNWorkersSolver(() => Environment.ProcessorCount / 2) // Nº of worker threads
            .SetBacklog(16384)
            .SetMaxEventsPerWake(512)
            .SetMaxNumberConnectionsPerWorker(512)
            .SetPort(8080)
            .SetSlabSizes(16 * 1024, 16 * 1024) // Pinned unmanaged memory slabs size per connection
            .Map(CreateLayoutBuilder());        // Register your GenHttp LayoutBuilder
        
        var engine = builder.Build();
        engine.Run();
    }

    private static LayoutBuilder CreateLayoutBuilder() =>
        Layout
            .Create()
            .AddService<BenchmarkService>("bench") // Adding a webservice

            // High performance endpoints
            .Add("/json",                          
                 Content.From(Resource.FromString(
                    JsonSerializer.Serialize(new JsonMessage{ message = "Hello, World!" }))))
            .Add("/plaintext", 
                 Content.From(Resource.FromString("Hello, World!"))); // Direct resource for blazing performance
}

public class BenchmarkService
{
    [ResourceMethod]
    public async Task<IResponse> Get(IRequest request)
    {
        return request
            .Respond()
            .Content(new JsonContent(new JsonMessage
            {
                message = "Hello, World!"
            }, JsonSerializerOptions.Default))
            .Build();
    }
}

public class JsonMessage
{
    public string message { get; set; }
}
```