namespace CRM.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid GetCurrentUserId();
    IList<string> GetCurrentUserRoles();
}
