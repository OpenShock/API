using ShockLink.API.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ShockLink.API.ExceptionHandle;

public class ErrorResponse : BaseResponse<object>
{
    public required ErrorObj Error { get; set; }

    public class ErrorObj
    {
        public required Guid CorrelationId { get; set; }
    }
}