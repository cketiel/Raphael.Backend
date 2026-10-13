namespace Raphael.Shared.Interfaces
{
    /// <summary>
    /// A tenant's user tried to change or delete a row of another company, or of the common
    /// catalog. Answered 403 by the API (RE-049).
    /// </summary>
    public sealed class TenantAccessException : Exception
    {
        public TenantAccessException()
            : base("This record belongs to another company or to the common catalog.")
        {
        }
    }
}
