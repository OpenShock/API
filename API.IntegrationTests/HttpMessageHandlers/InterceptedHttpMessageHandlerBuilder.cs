using Microsoft.Extensions.Http;

namespace OpenShock.API.IntegrationTests.HttpMessageHandlers;

sealed class InterceptedHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
{
    public override string? Name { get; set; }
    public override HttpMessageHandler PrimaryHandler { get; set; }
    public override IList<DelegatingHandler> AdditionalHandlers => [];


    public override HttpMessageHandler Build()
    {
        return new InterceptedHttpMessageHandler();
    }
}
