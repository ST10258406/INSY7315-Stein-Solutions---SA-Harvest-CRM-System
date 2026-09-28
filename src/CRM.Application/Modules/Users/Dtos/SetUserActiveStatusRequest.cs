namespace CRM.Application.Modules.Users.Dtos;

public class SetUserActiveStatusRequest
{
    // Nullable on purpose: a plain bool would bind a missing field (e.g. a body of {})
    // to false and silently deactivate the target. Null is rejected by the validator.
    public bool? IsActive { get; set; }
}
