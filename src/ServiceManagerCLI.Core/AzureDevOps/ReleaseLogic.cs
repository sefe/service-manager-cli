﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Newtonsoft.Json;
using RestSharp;
using ServiceManagerCLI.Config.Dtos;

namespace ServiceManagerCLI.Core.AzureDevOps
{
    public class ReleaseLogic
    {
        private readonly string _teamProjectName;
        private readonly string _collectionUri;
        private readonly IAzureDevOpsTokenHandler _tokenHandler;
        private readonly AzureDevOpsSettings _adoSettings;

        public ReleaseLogic(
            string azureDevOpsServer, 
            string teamProjectName, 
            AzureDevOpsSettings adoSettings, 
            IAzureDevOpsTokenHandler tokenHandler)
        {
            _collectionUri = azureDevOpsServer;
            _teamProjectName = teamProjectName;
            _tokenHandler = tokenHandler;
            _adoSettings = adoSettings;
        }

        public void UpdateReleaseVariables(string releaseId, Dictionary<string, string> variableNamesAndValues)
        {
            var releaseClient = GetReleaseClient(releaseId);
            var release = GetRelease(releaseClient);
            UpdateReleaseObjectVariables(release, variableNamesAndValues);
            PublishUpdatedRelease(release, releaseClient);
        }

        private void PublishUpdatedRelease(Release release, RestClient releaseClient)
        {
            Console.WriteLine("Publishing updated release back to Azure DevOps");

            var uri = releaseClient.Options.BaseUrl;
            var updatedReleaseJson = JsonConvert.SerializeObject(release);
            var putRequest = new RestRequest(uri, Method.Put) { Timeout = -1 };
            putRequest.AddJsonBody(updatedReleaseJson);
            var putResponse = releaseClient.Execute(putRequest);

            if (!putResponse.IsSuccessful)
            {
                throw new Exception(putResponse.ErrorMessage);
            }

            Console.WriteLine("Release published back to Azure DevOps successfully");
        }

        private void UpdateReleaseObjectVariables(Release release, Dictionary<string, string> variableNamesAndValues)
        {
            foreach (var variableName in variableNamesAndValues.Keys)
            {
                UpsertReleaseVariableValue(release, variableName, variableNamesAndValues[variableName]);
            }
        }

        private RestClient GetReleaseClient(string releaseId)
        {
            var clientUri = AzureDevOpsApiUriBuilder.GetUriForReleaseId(_collectionUri, _teamProjectName, releaseId);
            var accessToken = _tokenHandler.GetToken();
            var options = RestClientOptionsBuilder.GetRestClientOptions(_adoSettings, accessToken, clientUri);
            return new RestClient(options);
        }

        public Release GetRelease(string releaseId)
        {
            var releaseClient = GetReleaseClient(releaseId);
            return GetRelease(releaseClient);
        }

        public string GetBuildIdFromRelease(string releaseId)
        {
            var release = GetRelease(releaseId);

            const string buildUriName = "buildUri";

            var buildArtifact = release.Artifacts.FirstOrDefault(x => x.Type == "Build");

            if (buildArtifact == null)
            {
                return string.Empty;
            }

            if (!buildArtifact.DefinitionReference.ContainsKey(buildUriName))
            {
                return string.Empty;
            }

            var buildUri = buildArtifact.DefinitionReference[buildUriName];

            var buildId = buildUri.Id.Split('/').Last();

            return buildId;
        }

        private Release GetRelease(RestClient releaseClient)
        {
            var uri = releaseClient.Options.BaseUrl;
            var getRequest = new RestRequest(uri) { Timeout = -1 };

            var getResponse = releaseClient.Execute(getRequest);

            if (!getResponse.IsSuccessful)
            {
                throw new Exception(getResponse.ErrorMessage);
            }

            var release = JsonConvert.DeserializeObject<Release>(getResponse.Content);
            return release;
        }

        private void UpsertReleaseVariableValue(Release release, string variableName, string variableValue)
        {
            if (!release.Variables.ContainsKey(variableName))
            {
                var newVariableValue = new ConfigurationVariableValue
                {
                    Value = variableValue,
                    AllowOverride = true
                };

                release.Variables.Add(variableName, newVariableValue);
                Console.WriteLine($"Adding variable to release, VariableName={variableName}, VariableValue={variableValue}");
            }
            else
            {
                var variableToUpdate = release.Variables[variableName];
                variableToUpdate.Value = variableValue;
                Console.WriteLine($"Updating variable in release, VariableName={variableName}, VariableValue={variableValue}");
            }
        }
    }
}
