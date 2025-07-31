using Bogus;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.SeedE2E.Fakers;

public static class FakeSafetySettings
{
    public static Faker<T> ApplySafetySettingsRules<T>(this Faker<T> faker) where T : SafetySettings
    {
        return faker
            .RuleFor(m => m.AllowShock, f => f.Random.Bool(0.7f))
            .RuleFor(m => m.AllowVibrate, f => f.Random.Bool(0.7f))
            .RuleFor(m => m.AllowSound, f => f.Random.Bool(0.7f))
            .RuleFor(m => m.AllowLiveControl, f => f.Random.Bool(0.8f))
            .RuleFor(m => m.MaxIntensity, f => f.Random.Byte(10, 100))
            .RuleFor(m => m.MaxDuration, f => f.Random.UShort(5000, 30000))
            .RuleFor(m => m.IsPaused, f => f.Random.Bool(0.1f));
    }
}
