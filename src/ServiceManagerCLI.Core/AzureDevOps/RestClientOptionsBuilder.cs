using System;
using RestSharp;
using RestSharp.Authenticators.OAuth2;
using ServiceManagerCLI.Config.Dtos;

namespace Trading.ServiceManagerCLI.Core.AzureDevOps
{
    public class RestClientOptionsBuilder
    {
        public static RestClientOptions GetRestClientOptions(AzureDevOpsSettings adoSettings, string token, string clientUri)
        {
            if (clientUri.ToLowerInvariant().Contains(adoSettings.CollectionUrlCloudIndicator.ToLowerInvariant()))
            {
                Console.WriteLine($"Creating RestClientOptions using OAuth2Authorization for Uri={clientUri}");
                return new RestClientOptions(clientUri)
                {
                    Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(token, "Bearer"),
                    MaxTimeout = -1
                };
            }

            Console.WriteLine($"Creating RestClientOptions using default credentials for Uri={clientUri}");
            return new RestClientOptions(clientUri)
            {
                UseDefaultCredentials = true,
                MaxTimeout = -1
            };
        }

    }
}
