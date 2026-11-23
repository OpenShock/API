using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Tests.OpenShockDb;

public sealed class OpenShockContextModelTests
{
    private sealed record CapturedLog(LogLevel Level, EventId EventId, string Message);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<CapturedLog> _logs = [];

        public IReadOnlyList<CapturedLog> Logs => _logs;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_logs);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly List<CapturedLog> _logs;

            public CapturingLogger(List<CapturedLog> logs)
            {
                _logs = logs;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;

                lock (_logs)
                {
                    _logs.Add(new CapturedLog(logLevel, eventId, formatter(state, exception)));
                }
            }
        }
    }

    [Test]
    public async Task ModelInitialization_DoesNotLogCollectionWithoutComparerWarning()
    {
        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddProvider(loggerProvider);
        });

        var optionsBuilder = new DbContextOptionsBuilder<OpenShockContext>();
        OpenShockContext.ConfigureOptionsBuilder(
            optionsBuilder,
            "Host=localhost;Database=openshock;Username=openshock;Password=openshock",
            debug: false);
        optionsBuilder
            .EnableServiceProviderCaching(false)
            .UseLoggerFactory(loggerFactory);

        await using var context = new OpenShockContext(optionsBuilder.Options);

        var adminUsersView = context.Model.FindEntityType(typeof(AdminUsersView));
        var rolesProperty = adminUsersView?.FindProperty(nameof(AdminUsersView.Roles));

        await Assert.That(rolesProperty).IsNotNull();
        await Assert.That(rolesProperty!.GetValueConverter()).IsNull();
        await Assert.That(rolesProperty.GetColumnType()).IsEqualTo("role_type[]");
        await Assert.That(loggerProvider.Logs.Any(log => log.EventId.Id == CoreEventId.ContextInitialized.Id)).IsTrue();

        var collectionWithoutComparerWarnings = loggerProvider.Logs
            .Where(log => log.EventId.Id == CoreEventId.CollectionWithoutComparer.Id)
            .ToArray();

        await Assert.That(collectionWithoutComparerWarnings).IsEmpty();
    }
}
