using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Trading.ServiceManagerCLI.Core.ServiceManager
{
    public class ServiceManagerHttpClient
    {
        private readonly Uri _serviceManagerUri;
        private readonly CredentialCache _credentialCache;

        public ServiceManagerHttpClient(string serviceManagerUri)
        {
            _serviceManagerUri = new Uri(serviceManagerUri);
            _credentialCache = new CredentialCache
            {
                {_serviceManagerUri, "NTLM", CredentialCache.DefaultNetworkCredentials}
            };
        }

        public async Task<ServiceManagerResponse> PostNewChangeRequest(SprintChangeRequestModel changeRequest)
        {
            string json = JsonConvert.SerializeObject(changeRequest);

            var clientHandler = new HttpClientHandler {Credentials = _credentialCache};

            using (var client = new HttpClient(clientHandler))
            {
                var response = await client.PostAsync(
                    _serviceManagerUri,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                StringBuilder crNumber = new StringBuilder();
                using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                {
                    while (!reader.EndOfStream)
                    {
                        crNumber.Append(reader.ReadLine());
                    }
                }

                return new ServiceManagerResponse()
                {
                    CrNumber = crNumber.ToString(),
                    ResponseReason = response.ReasonPhrase
                };
            }
        }
    }
}
