﻿using CommandLine;

namespace ServiceManagerCLI.Core.Arguments
{
    public class OptionsBase
    {
        [Option('s', "servicemangerapi", Required = true, HelpText = "The endpoint for the Service Manager API to use for interacting with Service Manager")]
        public string ServiceManagerApi { get; set; }

        [Option('c', "collectionuri", Required = true, HelpText = "The Azure DevOps Collection URI to use when interacting with Azure DevOps Server")]
        public string CollectionUri { get; set; }

    }
    
    [Verb("createcr", HelpText = "Add file contents to the index.")]
    public class CreateCrOptions : OptionsBase
    {
        [Option('b', "buildnumber", Required = true, HelpText = "Azure DevOps Build Number to attach to the Change Request.")]
        public string BuildNumber { get; set; }

        [Option('r', "releasename", Required = true, HelpText = "Azure DevOps Release Name to attach to the Change Request.")]
        public string ReleaseName { get; set; }

        [Option('u', "requestedby", Required = true, HelpText = "The display name of the identity that triggered (started) the deployment currently in progress.")]
        public string ReleaseDeploymentRequestedFor { get; set; }

        [Option('p', "crparamsfile", Required = true, HelpText = "The JSON File to use when constructing the CR, contains all required data to create a CR.")]
        public string CrParamsFile { get; set; }

        [Option('i', "releaseid", Required = false, Default = "", HelpText = "Azure DevOps Release ID, used for setting variable values.")]
        public string ReleaseId { get; set; }

        [Option('m', "commparamsfile", Required = false, Default = "", HelpText = "JSON File containing details required for sending out release comms.")]
        public string CommParamsFile { get; set; }

        [Option('w', "workItemLinking", Required = false, Default = "WorkItem", HelpText = "Flag to determine how linked work items should be found. Valid values are 'All' and 'WorkItem'.")]
        public string WorkItemLinking { get; set; }

        [Option('x', "existingCr", Required = false, Default = "", HelpText = "Existing CR number")]
        public string ExistingCr { get; set; }

        public bool IncludeAllLinkedWorkItems => WorkItemLinking == "All";
    }

    public class SetActivityOptions : OptionsBase
    {
        [Option('a', "activitytitle", Required = false, HelpText = "Activity in Change Request to update.")]
        public string Activity { get; set; }

        [Option('r', "changeno", Required = false, HelpText = "Change Request to Update, in format 'CR123456'.")]
        public string ChangeNo { get; set; }
    }

    [Verb("activitysuccess", HelpText = "Record changes to the repository.")]
    public class ActivitySuccessOptions : SetActivityOptions
    {

    }

    [Verb("activityfailed", HelpText = "Clone a repository into a new directory.")]
    public class ActivityFailedOptions : SetActivityOptions
    {
        
    }

    [Verb("setreleasevariable", HelpText = "Set variable value within a release pipeline.")]
    public class SetReleaseVariableOptions
    {
        [Option('c', "collectionuri", Required = true, HelpText = "The Azure DevOps Collection URI to use when interacting with Azure DevOps Server")]
        public string CollectionUri { get; set; }

        [Option('i', "releaseid", Required = true, HelpText = "Azure DevOps Release ID.")]
        public string ReleaseId { get; set; }

        [Option('t', "teamprojectname", Required = false, HelpText = "Team Project Name, e.g. 'Data and Analytics'")]
        public string TeamProjectName { get; set; }

        [Option('n', "variablename", Required = false, HelpText = "Variable Name to be updated'")]
        public string VariableName { get; set; }

        [Option('v', "variablevalue", Required = false, HelpText = "Variable value'")]
        public string VariableValue { get; set; }
    }
}
