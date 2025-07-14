namespace OpenShock.Common.Hubs;

public interface IPublicShareHub
{
    Task Welcome(PublicShareHub.AuthType authType);
    Task Updated();
}