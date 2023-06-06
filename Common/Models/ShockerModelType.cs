using NpgsqlTypes;

namespace ShockLink.Common.Models;

public enum ShockerModelType
{
    Small = 0,
    [PgName("petTrainer")] PetTrainer = 1
}