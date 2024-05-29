using System;
using ServiceManagerCLI.Core.Arguments;
using ServiceManagerCLI.Core.ServiceManager;
using ImpactQuestionResponses = ServiceManagerCLI.Core.ServiceManager.ImpactQuestionResponses;
using RiskQuestionResponses = ServiceManagerCLI.Core.ServiceManager.RiskQuestionResponses;

namespace ServiceManagerCLI.Core
{
    public class ChangeRequestModel : SprintChangeRequestModel
    {
        public ChangeRequestModel(CreateChangeRequestInput inputs)
        {
            Customer = inputs.Customer;
            Approvers = inputs.Approvers;
            ImpactResponses = new ImpactQuestionResponses
            {
                Criticality = inputs.ImpactQuestionResponses.Criticality,
                OutageOrRestrictedFunctionality = inputs.ImpactQuestionResponses.OutageOrRestrictedFunctionality,
                ServiceImpactedOnFailure = inputs.ImpactQuestionResponses.ServiceImpactedOnFailure,
            };
            Priority = inputs.Priority;
            RiskResponses = new RiskQuestionResponses()
            {
                Question1 = inputs.RiskQuestionResponses.Question1,
                Question2 = inputs.RiskQuestionResponses.Question2,
                Question3 = inputs.RiskQuestionResponses.Question3,
                Question4 = inputs.RiskQuestionResponses.Question4,
                Question5 = inputs.RiskQuestionResponses.Question5,
                Question6 = inputs.RiskQuestionResponses.Question6
            };

            // dates should be in GMT, SM will shift to local
            var parser = new Chronic.Parser();
            var scheduledStartDate = parser.Parse(inputs.ScheduledStartDate);
            if (scheduledStartDate == null)
            {
                throw new ArgumentException("Unable to parse the Scheduled Start Time, please use a known format from Chronic, https://github.com/robertwilczynski/nChronic");
            }
            if (scheduledStartDate.Start != null) 
                ScheduledStartDate = ((DateTime) scheduledStartDate.Start).ToUniversalTime();

            var scheduledEndDate = parser.Parse(inputs.ScheduledEndDate);
            if (scheduledEndDate == null)
            {
                throw new ArgumentException("Unable to parse the Scheduled End Time, please use a known format from Chronic, https://github.com/robertwilczynski/nChronic");
            }
            if (scheduledEndDate.Start != null)
                ScheduledEndDate = ((DateTime)scheduledEndDate.Start).ToUniversalTime();

            InitialActivityToComplete = inputs.InitialActivityToComplete;
        }
    }
}
