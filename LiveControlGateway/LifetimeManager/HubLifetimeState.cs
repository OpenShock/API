namespace OpenShock.LiveControlGateway.LifetimeManager;

/// <summary>
/// State of a hub lifetime
/// </summary>
public enum HubLifetimeState
{
    /// <summary>
    /// Normal operation
    /// </summary>
    Idle,
    
    /// <summary>
    /// Initial state
    /// </summary>
    SettingUp,
    
    /// <summary>
    /// Swapping to a new hub controller 
    /// </summary>
    Swapping,
    
    /// <summary>
    /// Hub controller is disconnecting, shutting down the lifetime
    /// </summary>
    Removing
}