using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GitCredentialManager;

namespace Atlassian.Bitbucket.Cloud
{
    public class BitbucketRestApi : IBitbucketRestApi
    {
        private readonly ICommandContext _context;
        private readonly Uri _apiUri = CloudConstants.BitbucketApiUri;

        public BitbucketRestApi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public async Task<RestApiResult<IUserInfo>> GetUserInformationAsync(string userName, string password, bool isBearerToken)
        {
            var requestUri = new Uri(_apiUri, "2.0/user");
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                if (isBearerToken)
                {
                    request.AddBearerAuthenticationHeader(password);
                }
                else
                {
                    request.AddBasicAuthenticationHeader(userName, password);
                }

                _context.Trace.WriteLine($"HTTP: GET {requestUri}");
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int) response.StatusCode} [{response.StatusCode}]");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.Always };
                        var obj = await response.Content.ReadFromJsonAsync<UserInfo>(jsonOptions);

                        return new RestApiResult<IUserInfo>(response.StatusCode, obj);
                    }

                    return new RestApiResult<IUserInfo>(response.StatusCode);
                }
            }
        }

        public Task<bool> IsOAuthInstalledAsync()
        {
            return Task.FromResult(true);
        }

        public Task<List<AuthenticationMethod>> GetAuthenticationMethodsAsync()
        {
            // For Bitbucket Cloud there is no REST API to determine login methods
            // instead this is determined later in the process by attempting
            // authenticated REST API requests and checking the response.
            return Task.FromResult(new List<AuthenticationMethod>());
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ??= _context.HttpClientFactory.CreateClient();

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
