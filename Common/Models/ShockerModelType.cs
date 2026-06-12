using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Models;

[PgEnum]
public enum ShockerModelType
{
    [PgName("caiXianlin")] CaiXianlin = 0,
    [PgName("petrainer")] PetTrainer = 1,
    [PgName("petrainer998DR")] Petrainer998DR = 2,
    [PgName("wellturnT330")] WellturnT330 = 3,
}