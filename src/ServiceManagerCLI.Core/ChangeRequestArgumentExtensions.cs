using System.Linq;

namespace Trading.ServiceManagerCLI.Core
{
    public static class ChangeRequestArgumentExtensions
    {
        public static bool ContainsAny(this string haystack, params string[] needles)
        {
            return needles.Any(haystack.Contains);
        }
    }
}
