using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.Utils.Migration;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ComplexMigrationAttribute<TMigration, TDbContext> : Attribute where TMigration : ComplexMigrationBase<TDbContext> where TDbContext : DbContext
{
}
