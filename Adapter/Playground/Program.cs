using Adapter;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Functional.Provider;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.Layouting.Provider;
using Unhinged;

namespace Playground;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = UnhingedEngine
            .CreateBuilder()
            .SetNWorkersSolver(() => Environment.ProcessorCount / 2)
            .SetBacklog(16384)
            .SetMaxEventsPerWake(512)
            .SetMaxNumberConnectionsPerWorker(512)
            .SetPort(8080)
            .SetSlabSizes(16 * 1024, 16 * 1024)
            .Map(CreateLayoutBuilder());
        
        var engine = builder.Build();
        
        engine.Run();
    }

    private static LayoutBuilder CreateLayoutBuilder() =>
        Layout
            .Create()
            .Add("/plaintext", Content.From(Resource.FromString("Hello World!")))
            .Add("/api", CreateApi());
    
    private static InlineBuilder CreateApi() => 
        Inline
            .Create() 
            .Get("plaintext", () => "Hello, World!")
            .Get("json", () => "Hello, World!");
}