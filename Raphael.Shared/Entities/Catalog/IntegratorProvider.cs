namespace Raphael.Shared.Entities.Catalog
{
    /// <summary>
    /// A clinic (integrator) works with a Provider from the catalog: it has "contracted" it.
    /// </summary>
    /// <remarks>
    /// One relation seen from both sides (CATALOG_MODEL.md §7, B): the Providers a clinic can send
    /// trips to, and, from the Provider's side, the clinics it works for.
    ///
    /// <para>
    /// It points at the catalog row, not at a <see cref="Provider"/> account, because a clinic may
    /// contract a Provider that does not operate in Raphael yet. Such a Provider cannot be given
    /// trips until it has an account: nobody would see them.
    /// </para>
    ///
    /// <para>
    /// Removing a Provider from the list deletes the row; trips already given to it keep their
    /// <c>ProviderId</c>.
    /// </para>
    /// </remarks>
    public class IntegratorProvider
    {
        public int Id { get; set; }

        public int IntegratorId { get; set; }
        public Integrator Integrator { get; set; } = null!;

        public int CatalogProviderId { get; set; }
        public CatalogProvider CatalogProvider { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>The person who contracted it: a clinic's admin.</summary>
        public int? CreatedByUserId { get; set; }

        /// <summary>The company, when a Provider creates the relation from its side (Desktop, later).</summary>
        public int? CreatedByProviderId { get; set; }
    }
}
