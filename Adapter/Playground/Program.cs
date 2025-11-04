using System;
using System.Text.Json;
using Adapter;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Conversion.Serializers.Json;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Functional.Provider;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.Layouting.Provider;
using GenHTTP.Modules.Webservices;
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
            .AddService<BenchmarkService>("bench")
            .Add("/plaintext", Content.From(Resource.FromString("Hello, World!")))
            .Add("/json", Content.From(Resource.FromString(JsonSerializer.Serialize(new JsonMessage{ message = "Hello, World!" }))))
            .Add("/api", CreateApi());
    
    private static InlineBuilder CreateApi() => 
        Inline
            .Create()
            .Get("plaintext", () => "Hello, World!")
            .Get("json", () => new JsonMessage{ message = "Hello, World!" });
}

public class BenchmarkService
{
    [ResourceMethod]
    public async Task<IResponse> Get(IRequest request)
    {
        await Task.Delay(100);
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