using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public class V2UserSharesListItem : GenericIni
{
    public required IEnumerable<UserShareInfo> Shares { get; set; }
}