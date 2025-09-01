using OpenShock.Common.Models;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Serialization.Gateway;
using OpenShock.Serialization.Types;

namespace OpenShock.LiveControlGateway.Mappers;

public static class FbsMapper
{
    public static Serialization.Types.ShockerModelType ToFbsModelType(Common.Models.ShockerModelType type)
    {
        return type switch
        {
            Common.Models.ShockerModelType.CaiXianlin => Serialization.Types.ShockerModelType.CaiXianlin,
            Common.Models.ShockerModelType.PetTrainer => Serialization.Types.ShockerModelType.Petrainer,
            Common.Models.ShockerModelType.Petrainer998DR => Serialization.Types.ShockerModelType.Petrainer998DR,
            _ => throw new NotImplementedException(),
        };
    }

    public static ShockerCommandType ToFbsCommandType(ControlType type)
    {
        return type switch
        {
            ControlType.Stop => ShockerCommandType.Stop,
            ControlType.Shock => ShockerCommandType.Shock,
            ControlType.Vibrate => ShockerCommandType.Vibrate,
            ControlType.Sound => ShockerCommandType.Sound,
            _ => throw new NotImplementedException(),
        };
    }

    public static ShockerCommand ToFbsShockerCommand(DeviceControlPayload.ShockerControlInfo control)
    {
        return new ShockerCommand
        {
            Model = ToFbsModelType(control.Model),
            Id = control.RfId,
            Type = ToFbsCommandType(control.Type),
            Intensity = control.Intensity,
            Duration = control.Duration
        };
    }
}
