namespace OpenShock.API.Hubs;

public interface IShareLinkHub
{
    Task Welcome(ShareLinkHub.AuthType authType);
    Task Updated();
}