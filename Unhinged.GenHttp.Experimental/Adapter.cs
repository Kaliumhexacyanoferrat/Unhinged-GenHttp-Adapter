using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using GenHTTP.Api.Content;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using Microsoft.Extensions.ObjectPool;
using Unhinged.GenHttp.Experimental.Protocol;
using Unhinged.GenHttp.Experimental.Server;
using Unhinged.GenHttp.Experimental.Types;
using Unhinged.GenHttp.Experimental.Utils;

namespace Unhinged.GenHttp.Experimental;

public static class Adapter
{
    private const int BufferSize = 8192;

    private static readonly DefaultObjectPool<ClientContext> ContextPool = new(new ClientContextPolicy(), 65536);

    private static readonly ConcurrentDictionary<string, ImplicitServer> ServerCache = [];

    public static UnhingedEngine.UnhingedBuilder Map(
        this UnhingedEngine.UnhingedBuilder builder,
        IHandlerBuilder handlerBuilder,
        IServerCompanion? companion = null)
    {
        builder.InjectRequestHandler(RequestHandler(handlerBuilder.Build(), companion));
        return builder;
    }

    private static async ValueTask GenHttpAsyncStaticHandler(Connection connection, IHandler handler, IServerCompanion? companion)
    {
        var server = ServerCache.GetOrAdd("/", _ => new ImplicitServer(handler, companion));

        var context = ContextPool.Get();

        try
        {
            context.Request.Configure(server, connection);

            var response = await handler.HandleAsync(context.Request);

            if (response != null)
            {
                await MapResponse(response, connection);

                server.Companion?.OnRequestHandled(context.Request, response);
            }
        }
        catch (Exception e)
        {
            // todo: cannot tell the IP of the client in unhinged
            server.Companion?.OnServerError(ServerErrorScope.ServerConnection, null, e);
            throw;
        }
        finally
        {
            ContextPool.Return(context);
        }
    }

    private static ValueTask MapResponse(IResponse response, Connection connection)
    {
        connection.WriteBuffer.WriteUnmanaged(HttpStatusLines.Lines[response.Status.RawStatus]);
        connection.WriteBuffer.WriteUnmanaged(ServerHeaderName);

        foreach (var header in response.Headers)
        {
            connection.WriteBuffer.WriteHeaderUnmanaged(header);
        }

        if (response.ContentType is not null)
        {
            connection.WriteBuffer.WriteHeaderUnmanaged(ContentTypeHeader, response.ContentType.RawType);
        }

        if (response.Modified != null)
        {
            connection.WriteBuffer.WriteHeaderUnmanaged("Last-Modified", response.Modified.Value.ToUniversalTime().ToString("r"));
        }

        if (response.Expires != null)
        {
            connection.WriteBuffer.WriteHeaderUnmanaged("Expires", response.Expires.Value.ToUniversalTime().ToString("r"));
        }

        if (response.HasCookies)
        {
            foreach (var cookie in response.Cookies)
            {
                connection.WriteBuffer.WriteHeaderUnmanaged("Set-Cookie", $"{cookie.Key}={cookie.Value.Value}");
            }
        }

        if (response.ContentLength is not null)
        {
            connection.WriteBuffer.WriteUnmanaged(ContentLengthHeader);

            var buffer = connection.WriteBuffer.GetSpan(16); // 16 is enough for any int in UTF-8

            if (!Utf8Formatter.TryFormat((ulong)response.ContentLength, buffer, out var written))
                throw new InvalidOperationException("Failed to format int");

            connection.WriteBuffer.Advance(written);
            connection.WriteBuffer.Write("\r\n"u8);
        }
        else
        {
            connection.WriteBuffer.WriteUnmanaged(TransferEncodingChunkedHeader);
        }

        connection.WriteBuffer.WriteUnmanaged(DateHelper.HeaderBytes);

        // TODO: Improve performance here, cache or something, can't be creating and disposing streams every request?
        // TODO: Maybe consider using [ThreadStatic]

        if (response.ContentLength is null)
        {
            return WriteContentChunked(response, connection);
        }

        return response.Content!.WriteAsync(connection.WriteBuffer.AsUnhingedStream(), BufferSize);
    }

    private static async ValueTask WriteContentChunked(IResponse response, Connection connection)
    {
        await using var stream = new ChunkedStream(connection.WriteBuffer.AsUnhingedStream());

        await response.Content!.WriteAsync(stream, BufferSize);

        stream.Finish();
    }

    [Pure]
    private static Func<Connection, ValueTask> RequestHandler(IHandler handler, IServerCompanion? companion) =>
        conn => GenHttpAsyncStaticHandler(conn, handler, companion);

    private static ReadOnlySpan<byte> ServerHeaderName => "Server: U\r\n"u8;
    private static ReadOnlySpan<byte> ContentTypeHeader => "Content-Type"u8;
    private static ReadOnlySpan<byte> ContentLengthHeader => "Content-Length: "u8;
    private static ReadOnlySpan<byte> ContentEncodingHeader => "Content-Encoding: "u8;
    private static ReadOnlySpan<byte> TransferEncodingHeader  => "Transfer-Encoding: "u8;
    private static ReadOnlySpan<byte> TransferEncodingChunkedHeader  => "Transfer-Encoding: chunked\r\n"u8;
    private static ReadOnlySpan<byte> LastModifiedHeader => "Last-Modified: "u8;
    private static ReadOnlySpan<byte> ExpiresHeader => "Expires: "u8;
    private static ReadOnlySpan<byte> ConnectionHeader => "Connection: "u8;
    private static ReadOnlySpan<byte> DateHeader => "Date: "u8;

    // Might be useful further on
    //
    /*private static void AdvanceTo(Request request, string registeredPath)
    {
        var parts = registeredPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var _ in parts)
            request.Target.Advance();
    }*/
}
