namespace OpenShock.Cron.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CronJobAttribute : Attribute
{
    public CronJobAttribute(string shcedule, string? jobName = null)
    {
        Schedule = shcedule;
        JobName = jobName;
    }

    public string Schedule { get; }
    public string? JobName { get; }
}
