namespace OpenShock.Common.Models;

public sealed record LegacyDataResponse<T>(T Data, string Message = "");