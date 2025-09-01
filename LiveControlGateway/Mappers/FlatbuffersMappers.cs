using OpenShock.Common.Models;
using OpenShock.Serialization.Types;

namespace OpenShock.LiveControlGateway.Mappers;

public static class FlatbuffersMappers
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
}
