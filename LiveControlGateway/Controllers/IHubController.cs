using OpenShock.Serialization.Gateway;
using Semver;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// 
/// </summary>
public interface IHubController : IAsyncDisposable
{
    /// <summary>
    /// The hub ID, unique across all hubs
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
    /// Trigger EStop for device (cannot be undone remotely)
    /// </summary>
    /// <returns></returns>
    public ValueTask<bool> EmergencyStop();
    
    /// <summary>
    /// Start an OTA install
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public ValueTask OtaInstall(SemVersion version);
    
    /// <summary>
    /// Reboot the device
    /// </summary>
    /// <returns></returns>
    public ValueTask<bool> Reboot();

    /// <summary>
    /// Disconnect the old connection in favor of the new one
    /// </summary>
    /// <returns></returns>
    public Task DisconnectOld();
}