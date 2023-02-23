namespace ShockLink.API.Utils;

public static class ApplicationLogging
{
    public static ILoggerFactory LoggerFactory { get; set; } = null!;
    public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
    public static ILogger<T> CreateLoggerGen<T>() => LoggerFactory.CreateLogger<T>();
    public static ILogger CreateLogger(Type type) => LoggerFactory.CreateLogger(type);
    public static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
}