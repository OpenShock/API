using OpenShock.Common.Models;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class PermissionUtilsTests
{
    [Test]
    public async Task NullPerms_AlwaysAllowed()
    {
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Shock, false, null)).IsTrue();
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Vibrate, false, null)).IsTrue();
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Sound, false, null)).IsTrue();
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Stop, false, null)).IsTrue();
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Shock, true, null)).IsTrue();
    }

    [Test]
    public async Task Shock_Allowed_WhenShockPermTrue()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = true, Vibrate = false, Sound = false, Live = false,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Shock, false, perms)).IsTrue();
    }

    [Test]
    public async Task Shock_Denied_WhenShockPermFalse()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = false, Vibrate = true, Sound = true, Live = true,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Shock, false, perms)).IsFalse();
    }

    [Test]
    public async Task Vibrate_Allowed_WhenVibratePermTrue()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = false, Vibrate = true, Sound = false, Live = false,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Vibrate, false, perms)).IsTrue();
    }

    [Test]
    public async Task Vibrate_Denied_WhenVibratePermFalse()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = true, Vibrate = false, Sound = true, Live = true,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Vibrate, false, perms)).IsFalse();
    }

    [Test]
    public async Task Sound_Allowed_WhenSoundPermTrue()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = false, Vibrate = false, Sound = true, Live = false,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Sound, false, perms)).IsTrue();
    }

    [Test]
    public async Task Sound_Denied_WhenSoundPermFalse()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = true, Vibrate = true, Sound = false, Live = true,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Sound, false, perms)).IsFalse();
    }

    [Test]
    public async Task Stop_Allowed_WhenAnyPermTrue()
    {
        var shockOnly = new SharePermsAndLimits
        {
            Shock = true, Vibrate = false, Sound = false, Live = false,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Stop, false, shockOnly)).IsTrue();

        var vibrateOnly = new SharePermsAndLimits
        {
            Shock = false, Vibrate = true, Sound = false, Live = false,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Stop, false, vibrateOnly)).IsTrue();

        var soundOnly = new SharePermsAndLimits
        {
            Shock = false, Vibrate = false, Sound = true, Live = false,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Stop, false, soundOnly)).IsTrue();
    }

    [Test]
    public async Task Stop_Denied_WhenAllPermsFalse()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = false, Vibrate = false, Sound = false, Live = true,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Stop, false, perms)).IsFalse();
    }

    [Test]
    public async Task Live_Denied_WhenLivePermFalse()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = true, Vibrate = true, Sound = true, Live = false,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Shock, true, perms)).IsFalse();
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Vibrate, true, perms)).IsFalse();
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Sound, true, perms)).IsFalse();
    }

    [Test]
    public async Task Live_Allowed_WhenLivePermTrue()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = true, Vibrate = true, Sound = true, Live = true,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Shock, true, perms)).IsTrue();
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Vibrate, true, perms)).IsTrue();
        await Assert.That(PermissionUtils.IsAllowed(ControlType.Sound, true, perms)).IsTrue();
    }

    [Test]
    public async Task UnknownControlType_ReturnsFalse()
    {
        var perms = new SharePermsAndLimits
        {
            Shock = true, Vibrate = true, Sound = true, Live = true,
            Duration = null, Intensity = null
        };
        await Assert.That(PermissionUtils.IsAllowed((ControlType)999, false, perms)).IsFalse();
    }
}
