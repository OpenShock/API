using Hangfire.Common;
using OpenShock.Cron.Attributes;
using System.Reflection;

namespace OpenShock.Cron.Utils;

public static class CronJobCollector
{
    public sealed record CronJob(string Name, string Schedule, Job Job);
    public static List<CronJob> GetAllCronJobs()
    {
        List<CronJob> jobs = [];

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass) continue;

                var attribute = type.GetCustomAttribute<CronJobAttribute>();
                if (attribute is null) continue;

                var execFunc = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance);
                if (execFunc is null) throw new Exception($"Failed to find \"Execute()\" method of {type.FullName}");

                jobs.Add(new CronJob(attribute.JobName ?? type.Name, attribute.Schedule, new Job(execFunc)));
            }
        }

        var duplicates = jobs.GroupBy(job => job.Name).Where(group => group.Count() > 1).Select(group => group.First().Name).ToArray();
        if (duplicates.Length > 0)
        {
            throw new Exception($"Found the following CronJobs with duplicate names: {string.Join(", ", duplicates)}");
        }

        return jobs;
    }
}
