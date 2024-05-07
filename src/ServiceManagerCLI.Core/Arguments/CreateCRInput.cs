namespace Trading.ServiceManagerCLI.Core.Arguments
{
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

    public class CreateChangeRequestInput
    {
        public string Title { get; set; }
        public string Reason { get; set; }
        public string Area { get; set; }
        public string Priority { get; set; }
        public string Notes { get; set; }
        public string SupportGroup { get; set; }
        public string ScheduledStartDate { get; set; }
        public string ScheduledEndDate { get; set; }
        public InstallInstructions ImplementationPlan { get; set; }
        public string TestPlan { get; set; }
        public string Customer { get; set; }
        public string[] Approvers { get; set; }
        public ImpactQuestionResponses ImpactQuestionResponses { get; set; }
        public RiskQuestionResponses RiskQuestionResponses { get; set; }
        public string TemplateName { get; set; }
        public string InitialActivityToComplete { get; set; }
        public BranchingStrategies BranchingStrategy { get; set; }
        public string TeamProjectName { get; set; }
    }
}
