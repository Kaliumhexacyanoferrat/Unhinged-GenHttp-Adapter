using System.Collections;
using System.Diagnostics.Contracts;
using GenHTTP.Api.Protocol;
using Unhinged;

namespace Adapter.Types;

public sealed class Query : IRequestQuery
{

    #region Get-/Setters

    public int Count => QueryParameters?.Count ?? 0;

    public bool ContainsKey(string key) => QueryParameters?.ContainsKey(key) ?? false;

    public bool TryGetValue(string key, out string value)
    {
        if (QueryParameters?.TryGetValue(key, out var stringValue) ?? false)
        {
            value = stringValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public string this[string key]
    {
        get
        {
            if (QueryParameters?.TryGetValue(key, out var stringValue) ?? false)
            {
                return stringValue;
            }

            return string.Empty;
        }
    }

    public IEnumerable<string> Keys => QueryParameters?.Keys ?? Enumerable.Empty<string>();

    public IEnumerable<string> Values
    {
        get
        {
            if (QueryParameters != null)
            {
                foreach (var entry in QueryParameters)
                {
                    yield return entry.Value;
                }
            }
        }
    }
    
    private Connection Connection { get; }
    
    private PooledDictionary<string, string>? QueryParameters { get; } = null!;

    #endregion

    #region Initialization

    public Query(Connection connection)
    {
        Connection = connection;
        QueryParameters = connection.H1HeaderData.QueryParameters;
    }

    #endregion

    #region Functionality

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        if (QueryParameters != null)
        {
            foreach (var entry in QueryParameters)
            {
                yield return new(entry.Key, entry.Value);
            }
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