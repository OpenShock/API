namespace OpenShock.ServicesCommon.Hubs;

public interface IShareLinkHub
{
    Task Welcome(ShareLinkHub.AuthType authType);
    Task Updated();
}