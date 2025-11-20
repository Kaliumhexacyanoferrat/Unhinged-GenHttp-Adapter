using System.Net;
using System.Security.Cryptography.X509Certificates;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;

namespace Unhinged.GenHttp.Experimental.Types;

public sealed class ClientConnection : IClientConnection
{

    #region Get-/Setters

    public IPAddress IPAddress => throw new InvalidOperationException("Remote client IP address is not known");

    public ClientProtocol? Protocol { get; }

    public string? Host { get; } = "U";

    public X509Certificate? Certificate => null;

    private Connection Connection { get; }

    #endregion

    #region Initialization

    public ClientConnection(Connection connection)
    {
        Connection = connection;

        // todo: wired does not expose this information
        Protocol = ClientProtocol.Http;
    }

    #endregion

}
