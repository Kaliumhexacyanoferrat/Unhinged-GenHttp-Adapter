using System.Collections;
using GenHTTP.Api.Protocol;

namespace Unhinged.GenHttp.Experimental.Types;

public sealed class Headers : IHeaderCollection
{

    #region Get-/Setters

    public int Count => HeadersInternal?.Count ?? 0;

    public bool ContainsKey(string key) => HeadersInternal?.ContainsKey(key) ?? false;

    public bool TryGetValue(string key, out string value)
    {
        if (HeadersInternal?.TryGetValue(key, out var found) ?? false)
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public string this[string key] => ContainsKey(key) ? HeadersInternal?[key] ?? string.Empty : string.Empty;

    public IEnumerable<string> Keys => HeadersInternal?.Keys ?? Enumerable.Empty<string>();

    public IEnumerable<string> Values
    {
        get
        {
            if (HeadersInternal != null)
            {
                foreach (var entry in HeadersInternal)
                {
                    yield return entry.Value;
                }
            }
        }
    }

    private Connection? Connection { get; set; }

    private PooledDictionary<string, string>? HeadersInternal { get; set; }

    #endregion

    #region Functionality

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        if (HeadersInternal != null)
        {
            foreach (var entry in HeadersInternal)
            {
                yield return new(entry.Key, entry.Value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void SetConnection(Connection? connection)
    {
        Connection = connection;
        HeadersInternal = connection?.H1HeaderData.Headers;
    }

    #endregion

}
