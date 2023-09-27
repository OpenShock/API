using OpenShock.Common.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace OpenShock.ServicesCommon.ExceptionHandle;

public class ErrorResponse : BaseResponse<object>
{
    public required ErrorObj Error { get; set; }

    public class ErrorObj
    {
        public required Guid CorrelationId { get; set; }
    }
}