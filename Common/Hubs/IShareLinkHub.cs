namespace OpenShock.Common.Hubs;

public interface IShareLinkHub
{
    Task Welcome(ShareLinkHub.AuthType authType);
    Task Updated();
}