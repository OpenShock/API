using NpgsqlTypes;

namespace OpenShock.Common.Models;

public enum ShockerModelType
{
    [PgName("caiXianlin")] CaiXianlin = 0,
    [PgName("petTrainer")] PetTrainer = 1
}