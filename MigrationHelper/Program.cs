using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenShock.Common.OpenShockDb;

HostBuilder builder = new();
builder.ConfigureServices(collection =>
{
    collection.AddDbContext<OpenShockContext>();
});

var host = builder.Build();
host.RunAsync();