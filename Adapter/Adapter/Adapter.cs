using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Adapter.Protocol;
using Adapter.Server;
using Adapter.Types;
using Adapter.Utils;
using GenHTTP.Api.Content;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using Unhinged;

namespace Adapter;

public static class Adapter
{
    public static UnhingedEngine.UnhingedBuilder Map(
        this UnhingedEngine.UnhingedBuilder builder, 
        IHandlerBuilder handlerBuilder, 
        IServerCompanion? companion = null)
    {
        builder.InjectRequestHandler(RequestHandler(handlerBuilder.Build(), companion));
        return builder;
    }

    private static async ValueTask GenHttpStaticHandler(Connection connection, IHandler handler, IServerCompanion? companion)
    {
        var server = new ImplicitServer(handler, companion);

        try
        {
            using var request = new Request(server, connection);
            
            using var response = await handler.HandleAsync(request);

            if (response != null)
            {
                MapResponse(response, connection);

                server.Companion?.OnRequestHandled(request, response);
            }
        }
        catch (Exception e)
        {
            // todo: cannot tell the IP of the client in unhinged
            server.Companion?.OnServerError(ServerErrorScope.ServerConnection, null, e);
            throw;
        }
    }

    private static void MapResponse(IResponse response, Connection connection)
    {
        connection.WriteBuffer.WriteUnmanaged(HttpStatusLines.Lines[response.Status.RawStatus]);
        connection.WriteBuffer.WriteUnmanaged(ServerHeaderName);
        
        foreach (var header in response.Headers)
        {
            connection.WriteBuffer.WriteHeaderUnmanaged(header);
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
            connection.WriteBuffer.WriteHeaderUnmanaged("Content-Length", response.ContentLength.ToString());
        }
        
        connection.WriteBuffer.WriteUnmanaged(DateHelper.HeaderBytes);
        
        connection.WriteBuffer.WriteUnmanaged("Hello, World!"u8);
    }

    private static void AdvanceTo(Request request, string registeredPath)
    {
        var parts = registeredPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var _ in parts)
        {
            request.Target.Advance();
        }
    }
    
    [Pure]
    private static Action<Connection> RequestHandler(IHandler handler, IServerCompanion? companion) =>
        conn => _ = GenHttpStaticHandler(conn, handler, companion);
    
    
    private static ReadOnlySpan<byte> ServerHeaderName => "Server: Urn\n"u8;
    private static ReadOnlySpan<byte> ContentTypeHeader => "Content-Type: "u8;
    private static ReadOnlySpan<byte> ContentLengthHeader => "Content-Length: "u8;
    private static ReadOnlySpan<byte> ContentEncodingHeader => "Content-Encoding: "u8;
    private static ReadOnlySpan<byte> TransferEncodingHeader  => "Transfer-Encoding: "u8;
    private static ReadOnlySpan<byte> TransferEncodingChunkedHeader  => "Transfer-Encoding: chunked\r\n"u8;
    private static ReadOnlySpan<byte> LastModifiedHeader => "Last-Modified: "u8;
    private static ReadOnlySpan<byte> ExpiresHeader => "Expires: "u8;
    private static ReadOnlySpan<byte> ConnectionHeader => "Connection: "u8;
    private static ReadOnlySpan<byte> DateHeader => "Date: "u8;
}