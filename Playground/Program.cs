using System.Text.Json;
using GenHTTP.Api.Content.IO;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Functional.Provider;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.IO.Strings;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.Layouting.Provider;
using GenHTTP.Modules.Webservices;
using Unhinged;
using Unhinged.GenHttp.Experimental;

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

            .Add("/json", Content.From(
                Resource.FromString(JsonSerializer.Serialize(new JsonMessage{ message = "Hello, World!" }))
                    .Type(new FlexibleContentType("application/json"))))

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
    public IResponse Get(IRequest request)
    {
        return request
            .Respond()
            .Type(new FlexibleContentType("application/json"))
            .Content(new CustomContent(new JsonMessage{ message = "Hello, World!" }))
            .Build();
    }
}

public class CustomContent(object data) : IResponseContent
{
    public ValueTask<ulong?> CalculateChecksumAsync()
    {
        throw new NotImplementedException();
    }

    public async ValueTask WriteAsync(Stream target, uint bufferSize)
    {
        await JsonSerializer.SerializeAsync(target, data, data.GetType(), JsonSerializerOptions.Default);
    }

    public ulong? Length { get; } = 27;
}

public class JsonMessage
{
    public string message { get; set; }
}

public sealed class JsonResourceBuilder() : IResourceBuilder<JsonResourceBuilder>
{
    private string? _Content, _Name;

    private FlexibleContentType? _ContentType;

    private DateTime? _Modified;

    #region Functionality

    public JsonResourceBuilder Content(string content)
    {
        _Content = content;
        return this;
    }

    public JsonResourceBuilder Name(string name)
    {
        _Name = name;
        return this;
    }

    public JsonResourceBuilder Type(FlexibleContentType contentType)
    {
        _ContentType = contentType;
        return this;
    }

    public JsonResourceBuilder Modified(DateTime modified)
    {
        _Modified = modified;
        return this;
    }

    public IResource Build()
    {
        var content = _Content ?? throw new BuilderMissingPropertyException("content");

        return new StringResource(content, _Name, _ContentType, _Modified);
    }

    #endregion

}
