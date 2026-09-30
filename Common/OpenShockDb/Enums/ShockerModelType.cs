using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "shocker_model_type")]
public enum ShockerModelType
{
    [PgName("cai_xianlin")] CaiXianlin = 0,
    [PgName("petrainer")] PetTrainer = 1,
    [PgName("petrainer_998dr")] Petrainer998DR = 2,
    [PgName("wellturn_t330")] WellturnT330 = 3
}