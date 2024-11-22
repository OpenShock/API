using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.Utils.Migration;

public abstract class ComplexMigrationBase<T> where T : DbContext
{
    public virtual void BeforeUp(T context, ILogger logger) { }
    public virtual void AfterUp(T context, ILogger logger) { }
    public virtual void BeforeDown(T context, ILogger logger) { }
    public virtual void AfterDown(T context, ILogger logger) { }
}
