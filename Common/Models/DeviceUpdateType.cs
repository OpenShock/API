namespace OpenShock.Common.Models;

public enum DeviceUpdateType
{
    Created, // Whenever a new device is created
    Updated, // Whenever name or something else directly related to the device is updated
    ShockerUpdated, // Whenever a shocker is updated, name or limits for a person
    Deleted // Whenever a device is deleted
    
}