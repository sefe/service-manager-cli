using System;
using Newtonsoft.Json;

namespace ServiceManagerCLI.Core.ServiceManager
{
    public class SprintChangeRequestModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
        public string Area { get; set; }
        public string Priority { get; set; }
        public string Notes { get; set; }
        public string SupportGroup { get; set; }
        public DateTime ScheduledStartDate { get; set; }
        public DateTime ScheduledEndDate { get; set; }
        public string ImplementationPlan { get; set; }
        public string TestPlan { get; set; }
        public string BackoutPlan { get; set; }
        public string Customer { get; set; }
        public string[] Approvers { get; set; }

        [JsonProperty("ImpactQuestionResponses")]
        public ImpactQuestionResponses ImpactResponses { get; set; }

        [JsonProperty("RiskQuestionResponses")]
        public RiskQuestionResponses RiskResponses { get; set; }

        public string TemplateName { get; set; }

        public string InitialActivityToComplete { get; set; }

        public string CreatedBy { get; set; }
    }

    public class ImpactQuestionResponses
    {
        public bool OutageOrRestrictedFunctionality { get; set; }
        public bool ServiceImpactedOnFailure { get; set; }
        public string Criticality { get; set; }
    }

    public class RiskQuestionResponses
    {
        public bool Question1 { get; set; }
        public bool Question2 { get; set; }
        public bool Question3 { get; set; }
        public bool Question4 { get; set; }
        public bool Question5 { get; set; }
        public bool Question6 { get; set; }
    }
}
