namespace Raphael.Shared.Interfaces
{
    /// <summary>
    /// A row that belongs to a client company (tenant), or to Raphael itself when null (RE-049).
    /// </summary>
    /// <remarks>
    /// The context filters these by the caller's tenant and stamps new ones with it: the client
    /// never chooses whose a row is.
    /// </remarks>
    public interface IOwnedByProvider
    {
        int? OwnerProviderId { get; set; }
    }
}
