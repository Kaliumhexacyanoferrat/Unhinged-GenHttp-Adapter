using System.Collections;
using GenHTTP.Api.Protocol;
using GenHTTP.Engine.Shared.Types;
using Unhinged;

namespace Adapter.Types;

public sealed class Headers : IHeaderCollection
{
    #region Get-/Setters

    public int Count => HeadersInternal.Count;

    public bool ContainsKey(string key) => HeadersInternal.ContainsKey(key);

    public bool TryGetValue(string key, out string value)
    {
        if (HeadersInternal.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public string this[string key] => ContainsKey(key) ? HeadersInternal[key] : string.Empty;

    public IEnumerable<string> Keys => HeadersInternal.Keys;

    public IEnumerable<string> Values
    {
        get
        {
            foreach (var entry in HeadersInternal)
            {
                yield return entry.Value;
            }
        }
    }

    private Connection Connection { get; }

    private Unhinged.PooledDictionary<string, string> HeadersInternal { get; set; }

    #endregion

    #region Initialization

    public Headers(Connection connection)
    {
        Connection = connection;

        HeadersInternal = connection.H1HeaderData.Headers;
    }

    #endregion

    #region Functionality

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        foreach (var entry in HeadersInternal)
        {
            yield return new(entry.Key, entry.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Lifecycle

    public void Dispose()
    {

    }

    #endregion

}
