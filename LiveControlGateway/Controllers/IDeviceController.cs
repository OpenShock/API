using OpenShock.Common.Redis.PubSub;
using OpenShock.Serialization.Gateway;
using Semver;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// 
/// </summary>
public interface IDeviceController : IAsyncDisposable
{
    /// <summary>
    /// The devices ID, unique across all devices
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Control shockers
    /// </summary>
    /// <param name="controlCommands"></param>
    /// <returns></returns>
    public ValueTask Control(List<ShockerCommand> controlCommands);

    /// <summary>
    /// Turn the captive portal on or off
    /// </summary>
    /// <param name="enable"></param>
    /// <returns></returns>
    public ValueTask CaptivePortal(bool enable);
    
    /// <summary>
    /// Start an OTA install
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public ValueTask OtaInstall(SemVersion version);
}