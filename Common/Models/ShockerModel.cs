using NpgsqlTypes;

namespace ShockLink.Common.Models;

public enum ShockerModel
{
    Small = 0,
    [PgName("petTrainer")] PetTrainer = 1
}