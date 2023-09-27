using NpgsqlTypes;

namespace OpenShock.Common.Models;

public enum ShockerModelType
{
    Small = 0,
    [PgName("petTrainer")] PetTrainer = 1
}