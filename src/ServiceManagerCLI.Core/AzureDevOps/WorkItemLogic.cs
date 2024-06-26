﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using RestSharp;
using ServiceManagerCLI.Config.Dtos;
using ServiceManagerCLI.Core.Arguments;

namespace ServiceManagerCLI.Core.AzureDevOps
{
    public class WorkItemLogic
    {
        private readonly WorkItemTrackingHttpClient _workItemTrackingHttpClient;
        private readonly string _teamProjectName;
        private readonly string _collectionUri;
        private readonly AzureDevOpsSettings _adoSettings;
        private readonly IAzureDevOpsTokenHandler _tokenHandler;

        public WorkItemLogic(
            string azureDevOpsServer, 
            string teamProjectName, 
            AzureDevOpsSettings adoSettings, 
            IAzureDevOpsTokenHandler tokenHandler, 
            IVssConnectionFactory vssConnectionFactory)
        {
            _collectionUri = azureDevOpsServer;
            _teamProjectName = teamProjectName;
            _adoSettings = adoSettings;
            _tokenHandler = tokenHandler;

            var vssConnection = vssConnectionFactory.CreateVssConnection(_collectionUri);
            _workItemTrackingHttpClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();
        }

        public async Task<WorkItem> GetWorkItemAsync(int workItemId)
        {
            return await _workItemTrackingHttpClient.GetWorkItemAsync(_teamProjectName, workItemId, null, null, WorkItemExpand.All);
        }

        public List<WorkItem> GetWorkItemsLinkedToBuild(List<ResourceRef> workItemReferencesFromBuild, Build build, CreateCrOptions arguments)
        {
            // If you start with a build and get linked work items, you often get additional work items that are not required
            // This takes the list of linked work items from the build, and then checks the work item to see if it is linked to the build
            // and it will then only return the subset of the work items that are linked to the build.
            var workItems = new List<WorkItem>();

            var buildUri = build.Uri;

            foreach (var workItemReference in workItemReferencesFromBuild)
            {
                var workItemId = int.Parse(workItemReference.Id);

                var workItem = GetWorkItemAsync(workItemId).GetAwaiter().GetResult();

                if (ShouldWorkItemBeIncluded(workItem, buildUri.ToString(), arguments))
                {
                    workItems.Add(workItem);
                }
            }

            var workItemsForConsole = string.Join(", ", workItems.Select(x => x.Id));
            Console.WriteLine($"Found {workItems.Count} work items that are linked to BuildNumber={build.BuildNumber}, BuildId={build.Id}: {workItemsForConsole}");

            return workItems;
        }

        private bool ShouldWorkItemBeIncluded(WorkItem workItem, string buildUri, CreateCrOptions arguments)
        {
            if (arguments.IncludeAllLinkedWorkItems)
            {
                return true;
            }

            var workItemRelations = workItem.Relations;

            foreach (var relation in workItemRelations)
            {
                if (relation.Url == buildUri)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddTagToWorkItem(WorkItem workItem, string newTag)
        {
            if (workItem.Id == null)
            {
                Console.WriteLine($"Cannot add tag to Work Item, as the ID is null");
                return;
            }

            var workItemTags = GetWorkItemTags(workItem);
            workItemTags = AddAutoCrTagsToTags(workItemTags, newTag);

            var client = GetWorkItemClientForWorkItemId(workItem.Id.Value.ToString());

            PublishUpdatedWorkItem(workItem, client, workItemTags, newTag);
        }

        private void PublishUpdatedWorkItem(WorkItem workItem, RestClient client, string workItemTags, string newTag)
        {
            var uri = client.Options.BaseUrl;

            var request = new RestRequest(uri, Method.Patch) { Timeout = -1 };
            request.AddHeader("Content-Type", "application/json-patch+json");

            string param = "[\r\n  {\r\n    \"op\": \"add\",\r\n    \"path\": \"/fields/System.Tags\",\r\n    \"value\": \"" + workItemTags + "\"\r\n  }\r\n]";
            request.AddParameter("application/json-patch+json", param, ParameterType.RequestBody);

            var clientResponse = client.Execute(request);

            Console.WriteLine(!clientResponse.IsSuccessful
                ? $"Failed to add tag '{newTag}' to work item {workItem.Id}. Error = {clientResponse.ErrorMessage}"
                : $"Tag '{newTag}' successfully added to work item {workItem.Id}");
        }

        private RestClient GetWorkItemClientForWorkItemId(string workItemId)
        {
            var token = _tokenHandler.GetToken();
            var clientUri = AzureDevOpsApiUriBuilder.GetUriForWorkItemId(_collectionUri, _adoSettings, _teamProjectName, workItemId);
            var options = RestClientOptionsBuilder.GetRestClientOptions(_adoSettings, token, clientUri);
            return new RestClient(options);
        }

        private string GetWorkItemTags(WorkItem workItem)
        {
            if (workItem.Fields.TryGetValue("System.Tags", out var tags))
            {
                return tags.ToString();
            }

            return string.Empty;
        }

        private string AddAutoCrTagsToTags(string workItemTags, string newTag)
        {
            if (workItemTags == null)
            {
                workItemTags = newTag;
            }
            else
            {
                workItemTags = workItemTags + ";" + newTag;
            }

            return workItemTags;
        }
    }
}
