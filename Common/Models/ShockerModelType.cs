using NpgsqlTypes;

namespace OpenShock.Common.Models;

public enum ShockerModelType
{
    [PgName("caiXianlin")] CaiXianlin = 0,
    [PgName("petTrainer")] PetTrainer = 1, // Misspelled, should be "petrainer",
    [PgName("petrainer998DR")] Petrainer998DR = 2,
}