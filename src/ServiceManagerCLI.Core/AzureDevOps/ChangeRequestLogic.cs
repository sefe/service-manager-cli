using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.TeamFoundation.Build.WebApi;
using Newtonsoft.Json;
using RestSharp;
using ServiceManagerCLI.Config.Dtos;
using ServiceManagerCLI.Core.Arguments;
using ServiceManagerCLI.Core.ServiceManager;

namespace ServiceManagerCLI.Core.AzureDevOps
{
    public class ChangeRequestLogic
    {
        private readonly AzureDevOpsSettings _adoSettings;
        private readonly IAzureDevOpsTokenHandler _tokenHandler;
        private readonly IVssConnectionFactory _vssConnectionFactory;

        public ChangeRequestLogic(AzureDevOpsSettings adoSettings, IAzureDevOpsTokenHandler tokenHandler, IVssConnectionFactory vssConnectionFactory)
        {
            _adoSettings = adoSettings;
            _tokenHandler = tokenHandler;
            _vssConnectionFactory = vssConnectionFactory;
        }

        public void CreateChangeRequest(CreateCrOptions arguments)
        {
            CreateNewChangeRequest(arguments);
        }

        public object CompleteActivity(SetActivityOptions opts, string uriSection, string prettyPrint)
        {
            Console.WriteLine("Setting Activity " + opts.Activity + $" To {prettyPrint}.");

            var uri = opts.ServiceManagerApi +
                      "/api/changes/activity" + uriSection +
                      "?id=" + opts.ChangeNo + "&activityName=" + opts.Activity;

            var options = new RestClientOptions(uri)
            {
                UseDefaultCredentials = true,
                MaxTimeout = -1
            };

            var client = new RestClient(options);
            var request = new RestRequest(uri, Method.Put) { Timeout = -1 };
            var response = client.Execute(request);
            Console.WriteLine(response.Content);
            if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine($"{opts.ChangeNo} activity {opts.Activity} set to {prettyPrint} completed.");
            }
            else
            {
                Console.WriteLine($"Activity status update failed: {response.StatusCode}{response.ErrorMessage}{response.Content}");
                return -1;
            }

            return 0;
        }

        private void CreateNewChangeRequest(CreateCrOptions arguments)
        {
            Console.WriteLine($"CreateNewChangeRequest - arguments: {JsonConvert.SerializeObject(arguments)}");

            var crInputs = JsonConvert.DeserializeObject<CreateChangeRequestInput>(File.ReadAllText(arguments.CrParamsFile));
            var commSettings = GetCommSettings(arguments.CommParamsFile);
            var buildLogic = new BuildLogic(arguments.CollectionUri, crInputs.TeamProjectName, _adoSettings, _tokenHandler, _vssConnectionFactory);
            var workItemLogic = new WorkItemLogic(arguments.CollectionUri, crInputs.TeamProjectName, _adoSettings, _tokenHandler, _vssConnectionFactory);
            var releaseLogic = new ReleaseLogic(arguments.CollectionUri, crInputs.TeamProjectName, _adoSettings, _tokenHandler);
            var changeDescriptionGenerator = new ChangeDescriptionGenerator();

            var build = GetBuildForRelease(releaseLogic, buildLogic, arguments);
            Console.WriteLine($"Got build, BuildNumber={build.BuildNumber}, BuildId={build.Id}");

            ValidateBranchUsedForBuild(arguments, build, crInputs);

            var buildLinkedWorkItemReferences = buildLogic.GetBuildLinkedWorkItems(build);
            var workItems = workItemLogic.GetWorkItemsLinkedToBuild(buildLinkedWorkItemReferences, build, arguments);
            var changeDescriptions = changeDescriptionGenerator.GenerateChangeDescription(workItems);

            var sprintChangeRequest = CreateChangeRequest(crInputs, arguments, changeDescriptions);

            var response = CallServiceManagerToCreateChangeRequest(arguments, sprintChangeRequest);

            if (response.ResponseReason == "OK")
            {
                ProcessChangeRequestCreatedSuccessfully(
                    response.CrNumber,
                    changeDescriptions,
                    arguments,
                    workItems,
                    releaseLogic,
                    workItemLogic,
                    sprintChangeRequest,
                    commSettings);
            }
            else
            {
                throw new ArgumentException($"CR creation failed. {response.ResponseReason}.");
            }
        }

        private Build GetBuildForRelease(ReleaseLogic releaseLogic, BuildLogic buildLogic, CreateCrOptions arguments)
        {
            var buildFromReleaseId = GetBuildFromReleaseId(releaseLogic, buildLogic, arguments);

            if (buildFromReleaseId != null)
            {
                return buildFromReleaseId;
            }
            
            var buildArgument = arguments.BuildNumber;

            if (buildArgument.Contains(";"))
            {
                throw new Exception($"BuildNumber in CLI arguments is [{arguments.BuildNumber}], which contains a semi-colon. Only single builds are supported");
            }

            var buildFromBuildNumberInArguments = buildLogic.GetBuildForBuildNumber(arguments.BuildNumber);

            if (buildFromBuildNumberInArguments == null)
            {
                throw new Exception($"Unable to get build definition for build number {arguments.BuildNumber} specified in CLI tool arguments");
            }

            return buildFromBuildNumberInArguments;
        }

        private Build GetBuildFromReleaseId(ReleaseLogic releaseLogic, BuildLogic buildLogic, CreateCrOptions arguments)
        {
            try
            {
                Console.WriteLine($"Trying to get Build Id from Release Id {arguments.ReleaseId}");
                var buildId = releaseLogic.GetBuildIdFromRelease(arguments.ReleaseId);

                if (!string.IsNullOrEmpty(buildId))
                {
                    Console.WriteLine($"Build Id = {buildId} for Release Id {arguments.ReleaseId}. Trying to get build definition.");
                    var build = buildLogic.GetBuildForId(buildId);
                    return build;
                }

                Console.WriteLine($"Failed to get Build Id from Release Id {arguments.ReleaseId}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get build from release ID {arguments.ReleaseId}. Exception = {ex}");
                return null;
            }

        }

        private CommSettings GetCommSettings(string commParamsFile)
        {
            CommSettings commSettings = null;

            if (!string.IsNullOrEmpty(commParamsFile))
            {
                if (File.Exists(commParamsFile))
                {
                    var commParamsFileContents = File.ReadAllText(commParamsFile);
                    commSettings = JsonConvert.DeserializeObject<CommSettings>(commParamsFileContents);
                    Console.WriteLine($"Comm inputs json file read successfully: {commParamsFileContents}");
                }
                else
                {
                    Console.WriteLine($"Comm inputs json file does not exist - {commParamsFile}");
                }
                
            }
            else
            {
                Console.WriteLine("No Comm inputs json file specified");
            }

            return commSettings;
        }

        private void ProcessChangeRequestCreatedSuccessfully(
            string crNumber, 
            List<string> crChangesList, 
            CreateCrOptions arguments,
            List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> workItems,
            ReleaseLogic releaseLogic,
            WorkItemLogic workItemLogic,
            ChangeRequestModel changeRequest,
            CommSettings commSettings
            )
        {
            Console.WriteLine($"CR raised: {crNumber}");

            AddCrNumberTagToPbis(workItems, crNumber, arguments, workItemLogic);

            if (!string.IsNullOrEmpty(arguments.ReleaseId))
            {
                SetVariablesInReleaseId(
                    releaseLogic, 
                    arguments.ReleaseId,
                    crChangesList, 
                    crNumber, 
                    changeRequest.ScheduledStartDate, 
                    changeRequest.ScheduledEndDate,
                    commSettings);
            }
            else
            {
                Console.WriteLine("Release ID not specified so no variables will be saved in the release");
            }
        }

        private void AddCrNumberTagToPbis(
            List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> workItems, 
            string crNumber, 
            CreateCrOptions arguments,
            WorkItemLogic workItemLogic)
        {
            var tagPrefix = arguments.ServiceManagerApi.ToLower().Contains("dv") ? "AutoDv" : "Auto";
            var newTag = tagPrefix + crNumber.Replace("\"", "");

            Console.WriteLine($"Adding CR tag '{newTag}' to work items: {string.Join(",", workItems.Select(x => x.Id).ToList())}");
            
            foreach (var workItem in workItems)
            {
                workItemLogic.AddTagToWorkItem(workItem, newTag);
            }
        }

        private ServiceManagerResponse CallServiceManagerToCreateChangeRequest(CreateCrOptions arguments, ChangeRequestModel sprintChangeRequest)
        {
            Console.WriteLine($"Submitting CR: {JsonConvert.SerializeObject(sprintChangeRequest, Formatting.Indented)}");
            var httpClient = new ServiceManagerHttpClient(arguments.ServiceManagerApi + "/api/changes/new");
            return httpClient.PostNewChangeRequest(sprintChangeRequest).GetAwaiter().GetResult();
        }

        private ChangeRequestModel CreateChangeRequest(CreateChangeRequestInput crInputs, CreateCrOptions arguments, List<string> changeDescriptions)
        {
            var crDescription = string.Join(Environment.NewLine, changeDescriptions);

            return new ChangeRequestModel(crInputs)
            {
                BackoutPlan = crInputs.ImplementationPlan.Rollback,
                ImplementationPlan = $"Pre Production: {Environment.NewLine}" +
                                     $"{crInputs.ImplementationPlan.PreProduction}{Environment.NewLine}" +
                                     $"=================================={Environment.NewLine}" +
                                     $"Production:{Environment.NewLine}{crInputs.ImplementationPlan.Production}",
                Title = crInputs.Title,
                Area = crInputs.Area,
                Reason = crInputs.Reason,
                TestPlan = crInputs.TestPlan,
                SupportGroup = crInputs.SupportGroup,
                Description =
                    $"The following enhancements will be delivered by this CR:{Environment.NewLine}{crDescription}",
                TemplateName = crInputs.TemplateName,
                CreatedBy = GetUserIdFromDisplayName(arguments.ReleaseDeploymentRequestedFor),
                Notes = crInputs.Notes
            };
        } 

        private void SetVariablesInReleaseId(
            ReleaseLogic releaseLogic, 
            string releaseId, 
            List<string> changeChangesList, 
            string crNumber,
            DateTime startDateTime,
            DateTime endDateTime,
            CommSettings commSettings)
        {
            var startDateTimeLocal = startDateTime.ToLocalTime();
            var endDateTimeLocal = endDateTime.ToLocalTime();

            var variables = new Dictionary<string, string>
            {
                { "CR_ID", crNumber.Replace("\"", string.Empty) },
                { "ActualStartTime", startDateTimeLocal.ToString("yyyy-MM-d HH:mm:ss") },
                { "ActualEndTime", endDateTimeLocal.ToString("yyyy-MM-d HH:mm:ss")}
            };

            AddCommSettingVariables(variables, commSettings, changeChangesList);

            releaseLogic.UpdateReleaseVariables(releaseId, variables);
        }

        private void AddCommSettingVariables(
            Dictionary<string, string> variables, 
            CommSettings commSettings, 
            List<string> changesList)
        {
            if (commSettings is null)
            {
                var changeDescriptionSimple = string.Join(Environment.NewLine, changesList);
                variables.Add("ChangeDescription", changeDescriptionSimple);
                return;
            }

            var changeDescriptionHtml = this.ConvertChangeDescriptionToHtmlBulletList(changesList);

            variables.Add("Comms_ApplicationName", commSettings.ApplicationName);
            variables.Add("Comms_ApplicationAlias", commSettings.ApplicationAlias);
            variables.Add("Comms_ChangeImpact", commSettings.ChangeImpact);
            variables.Add("Comms_PointOfContact", commSettings.PointOfContact);
            variables.Add("Comms_UserActions", commSettings.UserActions);


            if (!string.IsNullOrEmpty(commSettings.ChangeDescriptionStart))
            {
                changeDescriptionHtml = $"<p>{commSettings.ChangeDescriptionStart}</p>{changeDescriptionHtml}";
            }

            if (!string.IsNullOrEmpty(commSettings.ChangeDescriptionEnd))
            {
                changeDescriptionHtml = $"{changeDescriptionHtml}<p>{commSettings.ChangeDescriptionEnd}</p>";
            }

            variables.Add("ChangeDescription", changeDescriptionHtml);
        }

        private string ConvertChangeDescriptionToHtmlBulletList(List<string> changes)
        {
            var sb = new StringBuilder();
            sb.Append("<ul>");

            foreach (var change in changes)
            {
                sb.Append($"<li>{change}</li>");
            }

            sb.Append("</ul>");

            return sb.ToString();
        }

        private static string GetUserIdFromDisplayName(string displayName)
        {
            DirectoryEntry dirEntry = new DirectoryEntry();
            DirectorySearcher dirSearcher = new DirectorySearcher(dirEntry)
            {
                SearchScope = SearchScope.Subtree,
                Filter = string.Format("(&(objectClass=user)(|(cn={0})(sn={0}*)(givenName={0})(DisplayName={0}*)(sAMAccountName={0}*)))",
                    displayName)
            };
            var searchResults = dirSearcher.FindAll();

            foreach (SearchResult sr in searchResults)
            {
                var de = sr.GetDirectoryEntry();
                string user = de.Properties["SAMAccountName"][0].ToString();

                if (de.NativeGuid == null)
                {
                    throw new ArgumentException("Failed to find a valid user GUID for the AD Account of requester!");
                }

                int flags = (int)de.Properties["userAccountControl"].Value;

                var enabled = !Convert.ToBoolean(flags & 0x0002);
                if (enabled)
                    return user;
            }
            throw new ArgumentException("Failed to locate a valid user account for requested user!");
        }

        private void ValidateBranchUsedForBuild(CreateCrOptions crArguments, Build build, CreateChangeRequestInput crInputs)
        {
            Dictionary<BranchingStrategies, List<string>> branches =

            new Dictionary<BranchingStrategies, List<string>>
            {
                {BranchingStrategies.GitHubFlow, new List<string> {"project", "master", "feature", "bug", "release", "main"}},
                {BranchingStrategies.GitFlow, new List<string> {"release", "master", "develop", "hotfix", "main"}}
            };

            if (!build.SourceBranch.ContainsAny(branches[crInputs.BranchingStrategy].ToArray()))
            {
                if (!crArguments.ServiceManagerApi.ToLower().Contains("dv"))
                    throw new ArgumentException(
                        $"Cannot raise a CR for Build {crArguments.BuildNumber} as this is not a valid branch for release!\nYou have specified {crInputs.BranchingStrategy} as your branching strategy, which includes the following releasable branches: {string.Join("|", branches[crInputs.BranchingStrategy])}");

                Console.WriteLine($"If this was a production deploy, the build isn't a valid branch for release and so would fail here.");
            }

            if (build.KeepForever != true)
            {
                throw new ArgumentException($"Cannot raise a CR for Build {crArguments.BuildNumber} as this is not a pinned build. Pin the build and re-run the CR Creator");
            }
        }
    }
}
