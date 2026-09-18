namespace Raphael.Api.Versioning
{
    /// <summary>
    /// The two contract versions this API publishes, as opposed to the version of the build that
    /// happens to be serving them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// They move for different reasons and on different clocks, and conflating them is how an
    /// integrator ends up being told to change their code because a caching bug was fixed on a
    /// Tuesday.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <b>Build version</b> — <c>v1.4.0</c>, the tag. Changes with every deployment. An
    ///     integrator never needs to know it and nothing of theirs breaks when it moves.
    ///   </item>
    ///   <item>
    ///     <b><see cref="Document"/></b> — the OpenAPI document, <c>v1</c>. Moves only when the
    ///     shape of the API stops being backwards compatible, which has not happened yet.
    ///   </item>
    ///   <item>
    ///     <b><see cref="IntegrationSpec"/></b> — the API-key contract that
    ///     <c>_meta/INTEGRATION_API_SPEC.md</c> describes and that external integrators code
    ///     against. Bumped in that document and here, in the same commit, or the number the API
    ///     reports is not the document anybody read.
    ///   </item>
    /// </list>
    /// </remarks>
    public static class ApiContract
    {
        /// <summary>Name of the OpenAPI document, used in its route and in its title.</summary>
        public const string Document = "v1";

        /// <summary>
        /// Version of <c>_meta/INTEGRATION_API_SPEC.md</c> that this build implements.
        /// </summary>
        public const string IntegrationSpec = "1.0.0";
    }
}
