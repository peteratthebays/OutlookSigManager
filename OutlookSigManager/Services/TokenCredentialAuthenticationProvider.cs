using Azure.Core;
using Microsoft.Graph;

namespace OutlookSigManager.Services;

/// <summary>
/// Bridges Azure.Identity TokenCredential with Graph SDK v4 IAuthenticationProvider
/// </summary>
public class TokenCredentialAuthenticationProvider : IAuthenticationProvider
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;

    public TokenCredentialAuthenticationProvider(TokenCredential credential, string[] scopes)
    {
        _credential = credential;
        _scopes = scopes;
    }

    public async Task AuthenticateRequestAsync(System.Net.Http.HttpRequestMessage request)
    {
        var context = new TokenRequestContext(_scopes);
        var token = await _credential.GetTokenAsync(context, CancellationToken.None);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
    }
}
