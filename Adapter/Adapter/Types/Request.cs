using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GenHTTP.Api.Routing;
using GenHTTP.Engine.Shared.Types;
using Unhinged;

namespace Adapter.Types;

public sealed class Request : IRequest
{
    private RequestProperties? _Properties;

    private Query? _Query;

    private ICookieCollection? _Cookies;

    private readonly ForwardingCollection _Forwardings = new();

    private Headers? _Headers;

    #region Get-/Setters

    public IRequestProperties Properties
    {
        get { return _Properties ??= new RequestProperties(); }
    }

    public IServer Server { get; }

    public IEndPoint EndPoint => throw new InvalidOperationException("EndPoint is not available as it is managed by Unhinged");

    public IClientConnection Client { get; }

    public IClientConnection LocalClient { get; }

    public HttpProtocol ProtocolType { get; }

    public FlexibleRequestMethod Method { get; }

    public RoutingTarget Target { get; }

    public string? UserAgent => this["User-Agent"];

    public string? Referer => this["Referer"];

    public string? Host => this["Host"];

    public string? this[string additionalHeader] => Headers.GetValueOrDefault(additionalHeader);

    public IRequestQuery Query
    {
        get { return _Query ??= new Query(Connection); }
    }

    public ICookieCollection Cookies
    {
        get { return _Cookies ??= FetchCookies(Connection); }
    }

    public IForwardingCollection Forwardings => _Forwardings;

    public IHeaderCollection Headers
    {
        get { return _Headers ??= new Headers(Connection); }
    }

    // TODO: Wrap the request content received by the client
    // For now there is never content, Unhinged doesn't yet support requests with body
    public Stream Content => Stream.Null;

    public FlexibleContentType? ContentType
    {
        get
        {
            /*if (InnerRequest.Headers.TryGetValue("Content-Type", out var contentType))
            {
                return FlexibleContentType.Parse(contentType);
            }*/
            
            // TODO: Unhinged does not support requests with body yet
            return null;
        }
    }

    //private IExpressRequest InnerRequest { get; }
    private Connection Connection { get; }

    #endregion

    #region Initialization

    public Request(IServer server, Connection connection)
    {
        Server = server;
        //InnerRequest = request;
        Connection = connection;
        
        

        // todo: no API provided by wired
        ProtocolType = HttpProtocol.Http11;
        
        Method = FlexibleRequestMethod.Get(connection.H1HeaderData.HttpMethod);
        Target = new RoutingTarget(WebPath.FromString(connection.H1HeaderData.Route));

        // TODO: Forwarding not supported
        /*if (request.Headers.TryGetValue("forwarded", out var entry))
        {
            _Forwardings.Add(entry);
        }
        else
        {
            _Forwardings.TryAddLegacy(Headers);
        }*/

        LocalClient = new ClientConnection(connection);

        // todo: potential client certificate is not exposed by wired
        Client = _Forwardings.DetermineClient(null) ?? LocalClient;
    }

    private CookieCollection FetchCookies(Connection connection)
    {
        // TODO: Get cookies from the connection
        var cookies = new CookieCollection();

        /*if (request.Headers.TryGetValue("Cookie", out var header))
        {
            cookies.Add(header);
        }*/

        return cookies;
    }

    #endregion

    #region Functionality

    public IResponseBuilder Respond() => new ResponseBuilder().Status(ResponseStatus.Ok);
    
    public UpgradeInfo Upgrade() => throw new NotSupportedException("Web sockets are not supported by Unhinged");

    #endregion

    #region Lifecycle

    private bool _Disposed;

    public void Dispose()
    {
        if (!_Disposed)
        {
            _Properties?.Dispose();

            _Disposed = true;
        }
    }

    #endregion

}