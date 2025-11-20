using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GenHTTP.Api.Routing;
using GenHTTP.Engine.Shared.Types;

using Unhinged.GenHttp.Experimental.Server;

namespace Unhinged.GenHttp.Experimental.Types;

public sealed class Request : IRequest
{
    private readonly ResponseBuilder _responseBuilder;

    private bool _freshResponse = true;

    private IServer? _Server;

    private IClientConnection? _Client;
    private IClientConnection? _LocalCLient;

    private FlexibleRequestMethod? _Method;
    private RoutingTarget? _Target;

    private readonly RequestProperties _Properties = new();

    private readonly Query _Query = new();

    private readonly CookieCollection _Cookies = new();

    private readonly ForwardingCollection _Forwardings = new();

    private readonly Headers _Headers = new();

    #region Get-/Setters

    public IRequestProperties Properties => _Properties;

    public IServer Server => _Server ?? throw new InvalidOperationException("Request is not initialized yet");

    public IEndPoint EndPoint => throw new InvalidOperationException("EndPoint is not available as it is managed by Unhinged");

    public IClientConnection Client => _Client ?? throw new InvalidOperationException("Request is not initialized yet");

    public IClientConnection LocalClient => _LocalCLient ?? throw new InvalidOperationException("Request is not initialized yet");

    public HttpProtocol ProtocolType { get; private set; }

    public FlexibleRequestMethod Method => _Method ?? throw new InvalidOperationException("Request is not initialized yet");

    public RoutingTarget Target => _Target?? throw new InvalidOperationException("Request is not initialized yet");

    public string? UserAgent => this["User-Agent"];

    public string? Referer => this["Referer"];

    public string? Host => this["Host"];

    public string? this[string additionalHeader] => Headers.GetValueOrDefault(additionalHeader);

    public IRequestQuery Query => _Query;

    public ICookieCollection Cookies => _Cookies;

    public IForwardingCollection Forwardings => _Forwardings;

    public IHeaderCollection Headers => _Headers;

    // TODO: Wrap the request content received by the client
    // For now there is never content, Unhinged doesn't yet support requests with body
    public Stream Content => Stream.Null;

    public FlexibleContentType? ContentType
    {
        get
        {
            if (Headers.TryGetValue("Content-Type", out var contentType))
            {
                return FlexibleContentType.Parse(contentType);
            }

            // TODO: Unhinged does not support requests with body yet
            return null;
        }
    }

    private Connection? Connection { get; set; }

    #endregion

    #region Initialization

    public Request(ResponseBuilder responseBuilder)
    {
        _responseBuilder = responseBuilder;
    }

    #endregion

    #region Functionality

    public IResponseBuilder Respond()
    {
        if (!_freshResponse)
        {
            _responseBuilder.Reset();
        }
        else
        {
            _freshResponse = false;
        }

        return _responseBuilder;
    }

    public UpgradeInfo Upgrade() => throw new NotSupportedException("Web sockets are not supported by Unhinged");

    public void Configure(ImplicitServer server, Connection connection)
    {
        _Server = server;

        Connection = connection;

        // todo: Unhinged only supports Http11
        ProtocolType = HttpProtocol.Http11;

        _Method = FlexibleRequestMethod.Get(connection.H1HeaderData.HttpMethod);
        _Target = new RoutingTarget(WebPath.FromString(connection.H1HeaderData.Route));

        _Headers.SetConnection(connection);
        _Query.SetConnection(connection);

        if (connection.H1HeaderData.Headers.TryGetValue("forwarded", out var entry))
        {
            _Forwardings.Add(entry);
        }
        else
        {
            _Forwardings.TryAddLegacy(Headers);
        }

        _LocalCLient = new ClientConnection(connection);

        // todo: potential client certificate is not exposed by unhinged
        // Unhinged does not support Tls
        _Client = _Forwardings.DetermineClient(null) ?? LocalClient;
    }

    private CookieCollection FetchCookies(Connection connection)
    {
        // TODO: Get cookies from the connection
        var cookies = new CookieCollection();

        if (connection.H1HeaderData.Headers.TryGetValue("Cookie", out var header))
        {
            cookies.Add(header);
        }

        return cookies;
    }

    internal void Reset()
    {
        _Headers.SetConnection(null);
        _Query.SetConnection(null);

        _Server = null;
        _Client = null;
        _LocalCLient = null;
        _Method = null;
    }

    #endregion

    #region Lifecycle

    public void Dispose()
    {
        // nop
    }

    #endregion

}
