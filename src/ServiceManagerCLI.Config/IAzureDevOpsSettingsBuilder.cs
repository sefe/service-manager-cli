using ServiceManagerCLI.Config.Dtos;

namespace ServiceManagerCLI.Config
{
    public interface IAzureDevOpsSettingsBuilder
    {
        AzureDevOpsSettings GetSettings();
    }
}
