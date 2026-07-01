using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Serialization.Gateway;
using OpenShock.Serialization.Types;

namespace OpenShock.LiveControlGateway.Mappers;

public static class FbsMapper
{
    public static Serialization.Types.ShockerModelType ToFbsModelType(Common.OpenShockDb.ShockerModelType type)
    {
        return type switch
        {
            Common.OpenShockDb.ShockerModelType.CaiXianlin => Serialization.Types.ShockerModelType.CaiXianlin,
            Common.OpenShockDb.ShockerModelType.PetTrainer => Serialization.Types.ShockerModelType.Petrainer,
            Common.OpenShockDb.ShockerModelType.Petrainer998DR => Serialization.Types.ShockerModelType.Petrainer998DR,
            Common.OpenShockDb.ShockerModelType.WellturnT330 => Serialization.Types.ShockerModelType.WellturnT330,
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

    public static ShockerCommand ToFbsShockerCommand(ShockerControlCommand control)
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
