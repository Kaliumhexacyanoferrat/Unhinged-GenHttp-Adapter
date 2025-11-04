# Unhinged-GenHttp-Adapter

Note: This project is still under development and some basic features are not available yet.

Undergoing task: Support requests with content (either known Content-Length or Transfer-Encoding: chunked)

## What is Unhinged?

Unhinged is an experimental, ultra-high-performance HTTP/1.1 server built from the ground up to bypass every layer of overhead in the .NET networking stack. Unlike all existing C# web frameworks — including Kestrel, ASP.NET, and other custom servers — Unhinged does not use System.Net.Sockets or SocketAsyncEventArgs. Instead, it interfaces directly with Linux system calls like epoll_wait, accept4, and eventfd through P/Invoke, giving it complete control over connection multiplexing, scheduling, and I/O readiness. This design eliminates the managed socket layer, thread pool dispatch, and kernel transition overhead typical of conventional .NET servers. Running under Native AOT with pinned, cache-aligned buffers and zero GC allocations, Unhinged achieves deterministic performance at massive concurrency levels — targeting tens of millions of requests per second in TechEmpower-style benchmarks. It’s not just a web server — it’s a proof that .NET can operate at the same level as hand-optimized C or Rust servers when freed from its abstractions.

## 🔌 Unhinged + GenHTTP Integration

A dedicated GenHTTP adapter allows Unhinged to serve as the socket engine for the GenHTTP framework.
In this setup, Unhinged handles all low-level network operations — including connection acceptance, epoll-based readiness events, and zero-copy I/O — while GenHTTP operates purely at the application layer, managing routing, middleware, and content generation.

This architecture cleanly separates network I/O performance from HTTP framework logic: Unhinged delivers the raw throughput and event-driven efficiency, while GenHTTP provides the developer-friendly, feature-rich API surface.
The result is a hybrid model that combines C-level socket performance with high-level .NET ergonomics, bridging the gap between bare-metal speed and modern web development.